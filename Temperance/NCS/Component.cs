using System.Reflection;
using Godot;

namespace IdyllicMultiplayerProject.Temperance.NCS;

/// <summary>
/// NCS - Node, Component, (Node)System architecture
///
/// Components represent a particular quality you want to give to a node,
/// and contain the fields needed to implement that kind of behavior when accessed by a <see cref="NodeSystem"/>
/// Only one unique instance of a component can be added to a node
/// </summary>
public abstract partial class Component : Node3D
{
    /// <summary>
    /// Set node name to the class name because Godot[GlobalClass] requires the node name to match the class name
    /// </summary>
    public override void _EnterTree()
    {
        base._EnterTree();
        // When adding components in-game, set the name and owner in case we want to save the scene for later
        SetName(GetType().Name);
        SetOwner(GetParent());
        
        // Add the parent to the list of nodes with this specific component
        if (ComponentManager.Instance.NodeDictionary.TryGetValue(GetType().Name, out var list))
            list.Add(Owner);
        else
            ComponentManager.Instance.NodeDictionary[GetType().Name] = [Owner];
        
        // If the component is intended to be multiplayer synchronized,
        // add a MultiplayerSynchronizer child and give it all the properties intended to be replicated
        var synchronizedAttribute = GetType().GetCustomAttribute<Synchronized>();
        if (synchronizedAttribute == null)
            return;

        var sceneReplicationConfig = new SceneReplicationConfig();
        var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttribute<SynchronizedField>();
            if (attribute == null)
                continue;

            var nodePath = GetType().Name + ":" + field.Name;
            sceneReplicationConfig.AddProperty(nodePath);
            sceneReplicationConfig.PropertySetSpawn(nodePath, attribute.SynchronizeOnSpawn);
            sceneReplicationConfig.PropertySetReplicationMode(nodePath, attribute.ReplicationMode);
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (ComponentManager.Instance.NodeDictionary.TryGetValue(GetType().Name, out var list))
            list.Remove(Owner);
    }
}