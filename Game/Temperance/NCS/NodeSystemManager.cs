using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Godot;
using Godot.Collections;
using Game.Temperance.Signals;

namespace Game.Temperance.NCS;

/// <summary>
/// NCS - Node, Component, (Node)System architecture
///
/// The NodeSystemManager initializes all <see cref="NodeSystem"/>,
/// attaches them as children the SceneTree root so that they are present in the Godot scene tree,
/// then injects any <see cref="NodeSystem"/> dependencies they have with each other
/// </summary>
public partial class NodeSystemManager
{
    public static NodeSystemManager Instance { get; } = new();
    private Node? _rootScene;

    // Used to inject dependencies based on name of NodeSystem
    private readonly Dictionary<string, NodeSystem> _nodeSystemDictionary = [];

    /// <summary>
    /// To initialize all <see cref="NodeSystem"/> we look for the classes with the [GlobalClass] attribute
    /// and if their name ends in "System"
    ///
    /// We initialize the <see cref="NodeSystem"/> and add it as a child of the SceneTree root so that it may
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
            var nodeSystem = (NodeSystem) GD.Load<CSharpScript>(script.ResourcePath).New();
            _nodeSystemDictionary.Add(nodeSystem.Name, nodeSystem);
            rootScene.AddChild(nodeSystem);
        }
        
        InjectDependencies();
    }

    /// <summary>
    /// After all systems are initialized, system dependencies can be injected without worry of order of initialization
    /// </summary>
    private void InjectDependencies()
    {
        foreach (var nodeSystem in _nodeSystemDictionary.Values)
        {
            var fields = nodeSystem.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(t => t.GetCustomAttribute<InjectedDependency>(false) != null);
            foreach (var field in fields)
            {
                if (_nodeSystemDictionary.TryGetValue(field.FieldType.Name, out var dependency))
                    field.SetValue(nodeSystem, dependency);

                if (field.FieldType.Name == ComponentManager.Instance.GetType().Name)
                    field.SetValue(nodeSystem, ComponentManager.Instance);
                
                if (field.FieldType.Name == NodeManager.Instance.GetType().Name)
                    field.SetValue(nodeSystem, NodeManager.Instance);

                if (field.FieldType.Name == GetType().Name)
                    field.SetValue(nodeSystem, this);
                
                if (field.FieldType.Name == SignalBus.Instance.GetType().Name)
                    field.SetValue(nodeSystem, SignalBus.Instance);
            }
        }
    }
    
    /// <summary>
    /// Every system should be a child of the root scene,
    /// retrieve it so that its public functions can be accessed
    /// </summary>
    /// <param name="nodeSystem"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool TryGetNodeSystem<T>([NotNullWhen(true)] out T? nodeSystem) where T : NodeSystem
    {
        nodeSystem = _rootScene?.GetNodeOrNull<T>($"{typeof(T).Name}");
        return nodeSystem != null;
    }
}

/// <summary>
/// <see cref="NodeSystemManager"/> will run the InjectDepencies() function on each NodeSystem
/// Each <see cref="NodeSystem"/> will go through its PRIVATE field instances with this attribute
/// and assign the appropriate system to it
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class InjectedDependency : Attribute { }