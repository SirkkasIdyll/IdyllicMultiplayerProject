using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Godot;
using Godot.Collections;
using Game.Temperance.Signals;

namespace Game.Temperance.EntityComponentSystem;

/// <summary>
/// ECS - Entity, Component, (Entity)System architecture
///
/// The EntitySystemManager initializes all <see cref="EntitySystem"/>,
/// attaches them as children the SceneTree root so that they are present in the Godot scene tree,
/// then injects any <see cref="EntitySystem"/> dependencies they have with each other
/// </summary>
public partial class EntitySystemManager
{
    public static EntitySystemManager Instance { get; } = new();
    private Node? _rootScene;

    // Used to inject dependencies based on name of NodeSystem
    private readonly Dictionary<string, EntitySystem> _entitySystemDictionary = [];

    /// <summary>
    /// To initialize all <see cref="EntitySystem"/> we look for the classes with the [GlobalClass] attribute
    /// and if their name ends in "System"
    ///
    /// We initialize the <see cref="EntitySystem"/> and add it as a child of the SceneTree root so that it may
    /// subscribe to user-created signals and be a part of the Godot Scenetree for things like Input/Godot signals
    /// </summary>
    public void InitializeNodeSystems(Node rootScene)
    {
        _rootScene = rootScene;
        var globalClassList = ProjectSettings.GetGlobalClassList();
        foreach (var dict in globalClassList)
        {
            var resource = ResourceLoader.Load( (StringName) dict["path"], "Script");
            if (resource is not Script script)
                continue;

            if (script.IsAbstract())
                continue;

            if (!script.GetGlobalName().ToString().EndsWith("System", true, null))
                continue;
            
            // We add the NodeSystem as a child so that it can subscribe to signals
            var entitySystem = (EntitySystem) GD.Load<CSharpScript>(script.ResourcePath).New();
            _entitySystemDictionary.Add(entitySystem.Name, entitySystem);
            rootScene.AddChild(entitySystem);
        }
        
        InjectDependencies();
    }

    /// <summary>
    /// After all systems are initialized, system dependencies can be injected without worry of order of initialization
    /// </summary>
    private void InjectDependencies()
    {
        foreach (var entitySystem in _entitySystemDictionary.Values)
        {
            var fields = entitySystem.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(t => t.GetCustomAttribute<InjectedDependency>(false) != null);
            foreach (var field in fields)
            {
                if (_entitySystemDictionary.TryGetValue(field.FieldType.Name, out var dependency))
                    field.SetValue(entitySystem, dependency);

                if (field.FieldType.Name == ComponentManager.Instance.GetType().Name)
                    field.SetValue(entitySystem, ComponentManager.Instance);
                
                if (field.FieldType.Name == EntityManager.Instance.GetType().Name)
                    field.SetValue(entitySystem, EntityManager.Instance);

                if (field.FieldType.Name == GetType().Name)
                    field.SetValue(entitySystem, this);
                
                if (field.FieldType.Name == SignalBus.Instance.GetType().Name)
                    field.SetValue(entitySystem, SignalBus.Instance);
            }
        }
    }
    
    /// <summary>
    /// Every system should be a child of the root scene,
    /// retrieve it so that its public functions can be accessed
    /// </summary>
    /// <param name="entitySystem"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool TryGetEntitySystem<T>([NotNullWhen(true)] out T? entitySystem) where T : EntitySystem
    {
        entitySystem = _rootScene?.GetNodeOrNull<T>($"{typeof(T).Name}");
        return entitySystem != null;
    }
}

/// <summary>
/// <see cref="EntitySystemManager"/> will run the InjectDepencies() function on each NodeSystem
/// Each <see cref="EntitySystem"/> will go through its PRIVATE field instances with this attribute
/// and assign the appropriate system to it
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class InjectedDependency : Attribute { }

public class DependenciesInjectedSignal : UserSignalArgs { }