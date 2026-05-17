using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Godot;
using Google.Protobuf.Collections;
using IdyllicMultiplayerProject.Shared.Systems.Metadata;
using IdyllicMultiplayerProject.Temperance.Network;
using IdyllicMultiplayerProject.Temperance.Signals;
using static GdUnit4.Assertions;
using static Godot.SceneReplicationConfig;

namespace IdyllicMultiplayerProject.Temperance.NCS;

/// <summary>
/// NCS - Node, Component, (Node)System architecture
///
/// The ComponentManager is responsible for getting, adding, or removing components
/// or finding nodes that have specific components
/// </summary>
public class ComponentManager
{
    public static ComponentManager Instance { get; } = new();
    private readonly SignalBus _signalBus = SignalBus.Instance;
    
    private readonly Dictionary<string, Component> _componentDictionary = [];
    public readonly Dictionary<string, List<Node>> NodeDictionary = [];

    private ComponentManager()
    {
        GetAllComponents();
    }

    /// <summary>
    /// Gets all <see cref="Component"/> by looking for classes with the [GlobalClass] attribute
    /// and if their name ends in "Component"
    ///
    /// Populates the internal component and node dictionaries to be used in other functions
    /// </summary>
    private void GetAllComponents()
    {
        var globalClassList = ProjectSettings.GetGlobalClassList();
        foreach (var dict in globalClassList)
        {
            var resource = ResourceLoader.Load( (StringName) dict["path"], "Script");
            if (resource is not Script script)
                continue;

            if (script.IsAbstract())
                continue;

            if (!script.GetGlobalName().ToString().EndsWith("Component", true, null))
                continue;
            
            var component = (Component) GD.Load<CSharpScript>(script.ResourcePath).New();
            _componentDictionary.Add(component.GetType().Name, component);
        }
    }

    /// <summary>
    /// Don't use this, it's just because I can't get the generic types directly from a protobuf message
    /// for syncing components on spawn
    /// </summary>
    private bool HasComponent(Node node, string componentName)
    {
        var comp = node.GetNodeOrNull(componentName);
        return comp != null;
    }
    
    /// <summary>
    /// Don't use this, it's just because I can't get the generic types directly from a protobuf message
    /// for syncing components on spawn
    /// </summary>
    public void RemoveComponent(Node node, string componentName)
    {
        var component = node.GetNodeOrNull(componentName);
        if (component == null)
            return;
        
        node.RemoveChild(component);
        _signalBus.EmitComponentRemovedSignal((node, (Component)component));
        component.QueueFree();
    }

    /// <summary>
    /// Don't use this, it's just because I can't get the generic types directly from a protobuf message
    /// for syncing components on spawn
    /// </summary>
    private bool TryAddComponent(Node node, string componentName)
    {
        _componentDictionary.TryGetValue(componentName, out var component);

        if (component == null)
            return false;
        
        var dupe = component.Duplicate();
        node.AddChild(dupe);
        _signalBus.EmitComponentAddedSignal((node, (Component)dupe));
        
        return true;
    }

    /// <summary>
    /// Prevent memory leaks by purging resources, should be signaled when closing
    /// </summary>
    public void PurgeDictionary()
    {
        foreach (var node in _componentDictionary.Values)
            node.Free();
    }

    /// <summary>
    /// This is really only something useful for clients
    /// When a client late-joins, they need to be aware of all the correct components something has
    /// </summary>
    /// <param name="node3D"></param>
    /// <param name="components"></param>
    public void SyncComponentsOnSpawn(Node3D node3D, RepeatedField<string> components)
    {
        // We shouldn't need to declare this, but we're doing it to make a point
        if (Networking.IsServer())
            return;

        // MetadataComponent contains our list of initial components
        if (!TryGetComponent<MetadataComponent>(node3D, out var metadataComponent))
            return;
        
        List<string> initialComponentsList = new List<string>(metadataComponent.ComponentDictionary.Keys);
        
        // Compare the list of components that the server's version has to what we spawned by default
        // There are three scenarios to account for
        foreach (var componentName in components)
        {
            // 1. We already have the component, so we don't need to add it or remove it from the node
            if (HasComponent(node3D, componentName))
                initialComponentsList.Remove(componentName);
            
            // 2. We don't have the component, so we add it to the node
            TryAddComponent(node3D, componentName);
        }
        
        // 3. The component wasn't communicated, meaning it was removed, so we remove it from the node
        foreach (var componentName in initialComponentsList)
            RemoveComponent(node3D, componentName);
    }
    
    /// <summary>
    /// Checks if GetNodeOrNull returns the type of T
    /// </summary>
    public bool HasComponent<T>(Node node) where T : Component
    {
        var comp = node.GetNodeOrNull<T>($"{typeof(T).Name}");
        return comp != null;
    }
    
    public bool TryAddComponent<T>(Node node) where T : Component
    {
        _componentDictionary.TryGetValue(typeof(T).Name, out var component);

        if (component == null)
            return false;
        
        var dupe = component.Duplicate();
        node.AddChild(dupe);
        _signalBus.EmitComponentAddedSignal((node, (Component)dupe));
        
        return true;
    }

    /// <summary>
    /// Checks if GetNodeOrNull returns the type of T and returns the Node
    /// </summary>
    public bool TryGetComponent<T>(Node node, [NotNullWhen(true)] out T? component) where T : Component
    {
        component = node.GetNodeOrNull<T>($"{typeof(T).Name}");
        return component != null;
    }

    /// <summary>
    /// Removes the component if it exists and is a child of the node
    /// QueueFrees the component as well
    /// </summary>
    public void RemoveComponent<T>(Node node) where T : Component
    {
        var component = node.GetNodeOrNull<T>($"{typeof(T).Name}");
        if (component == null)
            return;
        
        _signalBus.EmitComponentRemovedSignal((node, component));
        node.RemoveChild(component);
        component.QueueFree();
    }

    /// <summary>
    /// Gets all nodes that have the Component T so that certain checks can be applied to them
    /// or so that things that need to happen each frame can be applied to each component instance
    /// </summary>
    public void GetNodesWithComponent<T>(out List<Node> nodes) where T : Component
    {
        nodes = [];
        if (NodeDictionary.TryGetValue(typeof(T).Name, out var nodeList))
            nodes = nodeList;
    }
}

/// <summary>
/// The PeerSynchronized attribute will add a MultiplayerSynchronizer
/// </summary>
// [AttributeUsage(AttributeTargets.Class)]
// public class PeerSynchronized : Attribute
// {
//     /// <summary>
//     /// Used when a ReplicationConfig property has their ReplicationMode set to REPLICATION_MODE_ON_CHANGE
//     /// </summary>
//     public float DeltaInterval = 0.0f;
//     
//     /// <summary>
//     /// When true, all multiplayer peers get updates sent out.
//     /// When false, visibility should be controlled by adding a visibility filter
//     /// </summary>
//     public bool PublicVisibility = true;
//     
//     /// <summary>
//     /// Used when a ReplicationConfig property has their ReplicationMode set to REPLICATION_MODE_ALWAYS
//     /// </summary>
//     public float ReplicationInterval = 0.0f;
//     
//     /// <summary>
//     /// Chooses if visibility filters are updated automatically during process frames,
//     /// physics frames, or entirely manually
//     /// </summary>
//     public VisibilityUpdateModeEnum VisibilityUpdateMode = VisibilityUpdateModeEnum.Idle;
// }

[AttributeUsage(AttributeTargets.Class)]
public class Synchronized : Attribute
{
    
}

[AttributeUsage(AttributeTargets.Field)]
public class SynchronizedField : Attribute
{
    public bool OnChange = false;
}

/// <summary>
/// A struct for dealing with a Node and Component tuple so that you can reference a node and it's component
/// by having something like Node<MovementComponent> to maintain a reference to the node and the component at the same time
/// </summary>
public readonly struct Node<T> where T : Component?
{
    public readonly Node Owner;
    public readonly T Comp;

    private Node(Node owner, T comp)
    {
        Debug.Assert(comp?.Owner == owner);
        AssertBool(comp?.Owner == owner).IsTrue();
        
        Owner = owner;
        Comp = comp;
    } 
        
    public static implicit operator Node<T>((Node ParentNode, T Component) tuple)
    {
        return new Node<T>(tuple.ParentNode, tuple.Component);
    }

    public static implicit operator Node<T?>(Node owner)
    {
        return new Node<T?>(owner, null);
    }

    public static implicit operator Node(Node<T> ent)
    {
        return ent.Owner;
    }

    public static implicit operator T(Node<T> ent)
    {
        return ent.Comp;
    }

    public readonly void Deconstruct(out Node parentNode, out T comp)
    {
        parentNode = Owner;
        comp = Comp;
    }
}