using Godot;

namespace IdyllicMultiplayerProject.Temperance;

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
        SetName(GetType().Name);
        SetOwner(GetParent());
        if (ComponentManager.Instance.NodeDictionary.TryGetValue(GetType().Name, out var list))
            list.Add(Owner);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (ComponentManager.Instance.NodeDictionary.TryGetValue(GetType().Name, out var list))
            list.Remove(Owner);
    }
}