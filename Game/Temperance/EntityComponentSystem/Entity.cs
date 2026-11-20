using System.Collections.Generic;
using Godot;

namespace Game.Temperance.EntityComponentSystem;

/// <summary>
/// Extension of Node3D so we can store logical things like the entity's network ID,
/// the components that it has, the visual position of the entity compared to the global position,
/// etc.
/// </summary>
public partial class Entity : Node3D
{
    public ushort NetworkId { get; set; }
    
    public Dictionary<string, Component> Components = [];
    
    /// <summary>
    /// The visuals of an entity should lag behind the actual position/etc of the entity
    /// to avoid rubberbanding. Use this when you need to place the visuals for an entity.
    /// Anything else should use the normal GlobalTransform3D.
    /// </summary>
    public Transform3D VisualGlobalTransform3D;
    
}

/// <summary>
/// A struct for dealing with a Node and Component tuple so that you can reference a entity and it's component
/// by having something like Entity<MovementComponent> to maintain a reference to the entity and the component at the same time
/// </summary>
public readonly struct Entity<T>(Entity owner, T comp) where T : Component?
{
    public readonly Entity Owner = owner;
    public readonly T Comp = comp;
        
    public static implicit operator Entity<T>((Entity ParentNode, T Component) tuple)
    {
        return new Entity<T>(tuple.ParentNode, tuple.Component);
    }

    public static implicit operator Entity<T?>(Entity owner)
    {
        return new Entity<T?>(owner, null);
    }

    public static implicit operator Entity(Entity<T> ent)
    {
        return ent.Owner;
    }

    public static implicit operator T(Entity<T> ent)
    {
        return ent.Comp;
    }

    public readonly void Deconstruct(out Entity parentNode, out T comp)
    {
        parentNode = Owner;
        comp = Comp;
    }
}