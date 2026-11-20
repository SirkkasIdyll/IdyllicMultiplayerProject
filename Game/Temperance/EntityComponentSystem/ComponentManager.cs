using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;
using Game.Temperance.Signals;

namespace Game.Temperance.EntityComponentSystem;

/// <summary>
/// ECS - Entity, Component, (Entity)System architecture
///
/// The ComponentManager is responsible for getting, adding, or removing components
/// or finding entities that have specific components
/// </summary>
public partial class ComponentManager : Node
{
    public static ComponentManager Instance { get; } = new();
    private readonly SignalBus _signalBus = SignalBus.Instance;
    
    public override void _Ready()
    {
        base._Ready();

        _signalBus.EntitySpawnedSignal += OnEntitySpawned;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
		
        // Kill the orphans
        PurgeDictionary();
    }

    private void OnEntitySpawned(Entity entity, ref EntitySpawnedSignal args)
    {
        var children = entity.GetChildren();
        foreach (var child in children)
        {
            if (child is not Component component)
                continue;
            
            EntitiesWithComponentDictionary[component.GetType().Name].Entities.Add(entity);
            entity.Components.TryAdd(component.GetType().Name, component);
        }

        var signal = new ComponentsRegisteredSignal(entity);
        _signalBus.EmitComponentsRegisteredSignal(entity, ref signal);
    }
    
    /// <summary>
    /// Prevent memory leaks by purging resources, should be signaled when closing
    /// </summary>
    public void PurgeDictionary()
    {
        foreach (var (component, _) in EntitiesWithComponentDictionary.Values)
            component.Free();
    }
    
    /// <summary>
    /// Checks if GetNodeOrNull returns the type of T
    /// </summary>
    public bool HasComponent<T>(Entity entity) where T : Component
    {
        if (entity.Components.ContainsKey(typeof(T).Name))
            return true;
        
        // The below is the backup if we for some reason haven't recorded it in the dictionary
        // Not sure why it wouldn't be recorded in the dictionary at this point
        // It means the component was added by circumventing TryAddComponent and TrySpawnNode
        // Record it anyways
        var comp = entity.GetNodeOrNull<T>($"{typeof(T).Name}");
        if (comp == null)
            return false;
        
        entity.Components.Add(typeof(T).Name, comp);
        return true;
    }
    
    /// <summary>
    /// Overload for network response that only has the component ID
    /// </summary>
    public bool HasComponent(Entity entity, ushort componentId)
    {
        // Invalid component ID check
        if (!ComponentIdToComponentNameDictionary.TryGetValue(componentId, out var componentName))
            return false;
        
        if (entity.Components.ContainsKey(componentName))
            return true;
        
        // The below is the backup if we for some reason haven't recorded it in the dictionary
        // Not sure why it wouldn't be recorded in the dictionary at this point
        // It means the component was added by circumventing TryAddComponent and TrySpawnNode
        // Record it anyways
        if (entity.GetNodeOrNull($"{componentName}") is not Component comp)
            return false;
        
        entity.Components.Add(componentName, comp);
        return true;
    }
    
    /// <summary>
    /// Returns false if the entity already has the Component
    /// </summary>
    public bool TryAddComponent<T>(Entity entity) where T : Component, new()
    {
        if (HasComponent<T>(entity))
            return false;

        var component = new T();
        entity.AddChild(component);

        EntitiesWithComponentDictionary[typeof(T).Name].Entities.Add(entity);
        entity.Components.Add(component.GetType().Name, component);
        
        var signal = new ComponentAddedSignal((entity, component));
        _signalBus.EmitComponentAddedSignal((entity, component), ref signal);
        return true;
    }

    /// <summary>
    /// Overload for network response that only has the component ID
    /// </summary>
    public bool TryAddComponent(Entity entity, ushort componentId)
    {
        if (HasComponent(entity, componentId))
            return false;

        var componentName = ComponentIdToComponentNameDictionary[componentId];
        
        var component = (Component)EntitiesWithComponentDictionary[componentName].Component.Duplicate();
        entity.AddChild(component);

        EntitiesWithComponentDictionary[componentName].Entities.Add(entity);
        entity.Components.Add(component.GetType().Name, component);
        
        var signal = new ComponentAddedSignal((entity, component));
        _signalBus.EmitComponentAddedSignal((entity, component), ref signal);
        return true;
    }

    /// <summary>
    /// Checks if GetNodeOrNull returns the type of T and returns the Node
    /// </summary>
    public bool TryGetComponent<T>(Entity entity, [NotNullWhen(true)] out T? component) where T : Component
    {
        component = null;

        var componentName = $"{typeof(T).Name}";
        if (!entity.Components.TryGetValue(componentName, out var comp))
            return false;
        
        component = (T)comp;
        return true;

    }

    /// <summary>
    /// Overload for network response that only has the component ID
    /// </summary>
    public bool TryGetComponent(Entity entity, ushort componentId, [NotNullWhen(true)] out Component? component)
    {
        component = null;

        if (!ComponentIdToComponentNameDictionary.TryGetValue(componentId, out var componentName))
            return false;
        
        return entity.Components.TryGetValue(componentName, out component);
    }

    /// <summary>
    /// Removes the component if it exists and is a child of the node
    /// QueueFrees the component as well
    /// </summary>
    public void RemoveComponent<T>(Entity entity) where T : Component
    {
        if (!TryGetComponent<T>(entity, out var component))
            return;

        EntitiesWithComponentDictionary[typeof(T).Name].Entities.Remove(entity);
        entity.Components.Remove(typeof(T).Name);
        
        var signal = new ComponentRemovedSignal((entity, component));
        _signalBus.EmitComponentRemovedSignal((entity, component), ref signal);
        
        entity.RemoveChild(component);
        component.QueueFree();
    }

    /// <summary>
    /// Overload for network response that only has the component ID
    /// </summary>
    public void RemoveComponent(Entity entity, ushort componentId)
    {
        if (!TryGetComponent(entity, componentId, out var component))
            return;
        
        var componentName = ComponentIdToComponentNameDictionary[componentId];
        EntitiesWithComponentDictionary[componentName].Entities.Remove(entity);
        entity.Components.Remove(componentName);

        var signal = new ComponentRemovedSignal((entity, component));
        _signalBus.EmitComponentRemovedSignal((entity, component), ref signal);
        
        entity.RemoveChild(component);
        component.QueueFree();
    }

    /// <summary>
    /// Gets all entities that have the Component T so that certain checks can be applied to them
    /// or so that things that need to happen each frame can be applied to each component instance
    /// </summary>
    public void GetEntitiesWithComponent<T>(out List<Entity> entities) where T : Component
    {
        entities = [];
        if (EntitiesWithComponentDictionary.TryGetValue(typeof(T).Name, out var componentAndEntities))
            entities = componentAndEntities.Entities;
    }
}

public class ComponentAddedSignal(Entity<Component> entity) : UserSignalArgs
{
    private Entity<Component> _entity = entity;
}

public class ComponentsRegisteredSignal(Entity entity) : UserSignalArgs
{
    private Entity _entity = entity;
}

public class ComponentRemovedSignal(Entity<Component> entity) : UserSignalArgs
{
    private Entity<Component> _entity = entity;
}