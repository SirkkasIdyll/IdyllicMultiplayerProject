using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Godot;
using Game.Temperance.Signals;
using static GdUnit4.Assertions;

namespace Game.Temperance.NCS;

/// <summary>
/// NCS - Node, Component, (Node)System architecture
///
/// The ComponentManager is responsible for getting, adding, or removing components
/// or finding nodes that have specific components
/// </summary>
public partial class ComponentManager : Node
{
    public static ComponentManager Instance { get; } = new();
    private readonly SignalBus _signalBus = SignalBus.Instance;
    
    /// <summary>
    /// Nodes in the world and what components they have
    /// First populated when a node spawns
    /// </summary>
    private readonly Dictionary<Node, List<string>> _componentDictionaryByNode = new();

    public override void _Ready()
    {
        base._Ready();

        _signalBus.NodeSpawnedSignal += OnNodeSpawned;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
		
        // Kill the orphans
        PurgeDictionary();
    }

    private void OnNodeSpawned(Node node, ref NodeSpawnedSignal args)
    {
        var children = node.GetChildren();
        foreach (var child in children)
        {
            if (child is not Component component)
                continue;
            
            _componentDictionaryByNode.TryAdd(node, []);
            _componentDictionaryByNode[node].Add(component.GetType().Name);
        }
    }
    
    /// <summary>
    /// Prevent memory leaks by purging resources, should be signaled when closing
    /// </summary>
    public void PurgeDictionary()
    {
        foreach (var componentAndNodes in _componentDictionary.Values)
            componentAndNodes.Component.Free();
    }
    
    /// <summary>
    /// TODO: REWORK THIS
    /// This is really only something useful for clients
    /// When a client late-joins, they need to be aware of all the correct components something has
    /// </summary>
    /// <param name="node3D"></param>
    /// <param name="components"></param>
    // public void SyncComponentsOnSpawn(Node3D node3D, RepeatedField<string> components)
    // {
    //     // We shouldn't need to declare this, but we're doing it to make a point
    //     if (Networking.IsServer())
    //         return;
    //
    //     // MetadataComponent contains our list of initial components
    //     if (!TryGetComponent<MetadataComponent>(node3D, out var metadataComponent))
    //         return;
    //     
    //     List<string> initialComponentsList = new List<string>(metadataComponent.ComponentDictionary.Keys);
    //     
    //     // Compare the list of components that the server's version has to what we spawned by default
    //     // There are three scenarios to account for
    //     foreach (var componentName in components)
    //     {
    //         // 1. We already have the component, so we don't need to add it or remove it from the node
    //         if (HasComponent(node3D, componentName))
    //             initialComponentsList.Remove(componentName);
    //         
    //         // 2. We don't have the component, so we add it to the node
    //         TryAddComponent(node3D, componentName);
    //     }
    //     
    //     // 3. The component wasn't communicated, meaning it was removed, so we remove it from the node
    //     foreach (var componentName in initialComponentsList)
    //         RemoveComponent(node3D, componentName);
    // }
    
    /// <summary>
    /// Checks if GetNodeOrNull returns the type of T
    /// </summary>
    public bool HasComponent<T>(Node node) where T : Component
    {
        _componentDictionaryByNode.TryAdd(node, []);
        if (_componentDictionaryByNode[node].Contains(typeof(T).Name))
            return true;
        
        // The below is the backup if we for some reason haven't recorded it in the dictionary
        // Not sure why it wouldn't be recorded in the dictionary at this point
        // It means the component was added by circumventing TryAddComponent and TrySpawnNode
        // Record it anyways
        var comp = node.GetNodeOrNull<T>($"{typeof(T).Name}");
        if (comp == null)
            return false;
        
        _componentDictionaryByNode[node].Add(typeof(T).Name);
        return true;
    }
    
    /// <summary>
    /// Returns false if the node already has the Component
    /// </summary>
    public bool TryAddComponent<T>(Node node) where T : Component, new()
    {
        if (HasComponent<T>(node))
            return false;

        var component = new T();
        node.AddChild(component);

        _componentDictionary[typeof(T).Name].Nodes.Add(node);
        _componentDictionaryByNode.TryAdd(node, []);
        _componentDictionaryByNode[node].Add(component.GetType().Name);
        
        var signal = new ComponentAddedSignal((node, component));
        _signalBus.EmitComponentAddedSignal((node, component), ref signal);
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
        if (!TryGetComponent<T>(node, out var component))
            return;

        _componentDictionary[typeof(T).Name].Nodes.Remove(node);
        _componentDictionaryByNode.TryAdd(node, []);
        _componentDictionaryByNode[node].Remove(typeof(T).Name);
        
        var signal = new ComponentRemovedSignal((node, component));
        _signalBus.EmitComponentRemovedSignal((node, component), ref signal);
        
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
        if (_componentDictionary.TryGetValue(typeof(T).Name, out var componentAndNodes))
            nodes = componentAndNodes.Nodes;
    }
}

public class ComponentAddedSignal : UserSignalArgs
{
    private Node<Component> Node;

    public ComponentAddedSignal(Node<Component> node)
    {
        Node = node;
    }
}

public class ComponentRemovedSignal : UserSignalArgs
{
    private Node<Component> Node;
    
    public ComponentRemovedSignal(Node<Component> node)
    {
        Node = node;
    }
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