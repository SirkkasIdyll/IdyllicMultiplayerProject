using Godot;

namespace Game.Temperance.EntityComponentSystem;

/// <summary>
/// ECS - Entity, Component, (Entity)System architecture
///
/// USE THE GLOBALCLASS ATTRIBUTE
/// GLOBALCLASS ONLY ADDED TO THE GODOT SCENE TREE BY THE <see cref="EntitySystemManager"/>
/// NodeSystems implement specific logic and coordinate the application of signals to entities
/// </summary>
public abstract partial class EntitySystem : Node
{
    /// <summary>
    /// Set entity name to the type that it is for easier retrieval in <see cref="EntitySystemManager"/>
    /// and for injecting dependencies by checking type by name
    /// Otherwise default name will look like @Node3D@1
    /// </summary>
    protected EntitySystem() { SetName(GetType().Name); }
}