using System;
using Godot;

namespace Game.Temperance.NCS;

/// <summary>
/// NCS - Node, Component, (Node)System architecture
///
/// USE THE GLOBALCLASS ATTRIBUTE, DON'T FORGET IT GOD DAMN IT
/// Components represent a particular quality you want to give to a node,
/// and contain the fields needed to implement that kind of behavior when accessed by a <see cref="NodeSystem"/>
/// Only one unique instance of a component can be added to a node
/// </summary>
public abstract partial class Component : Node
{
    /// <summary>
    /// When the Synchronized attribute is added,
    /// the server will constantly communicate the state of SynchronizedFields to clients
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    protected sealed class Synchronized : Attribute { }

    /// <summary>
    /// Will do nothing if the component is not Synchronized
    /// Marks the field to be looked at when sending updates to clients
    /// 
    /// As for how I'm going to accomplish this, I don't know
    /// I think I should entirely use ENet for this since this is something that's going to happen extremely often
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    protected sealed class SynchronizedField : Attribute
    {
        /// <summary>
        /// When set to false, will have updates sent frequently and unreliably
        /// When set to true, will only have updates sent reliably whenever the value changes
        /// </summary>
        public bool OnChange = false;
    }
    
    /// <summary>
    /// Set node name to the class name because Godot[GlobalClass] requires the node name to match the class name
    /// </summary>
    public override void _EnterTree()
    {
        base._EnterTree();
        
        // When adding components in-game, set the name and owner in case we want to save the scene for later
        SetName(GetType().Name);
        SetOwner(GetParent());
    }
}