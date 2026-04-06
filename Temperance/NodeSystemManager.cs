using System;
using System.Linq;
using System.Reflection;
using Godot;
using Godot.Collections;

namespace IdyllicMultiplayerProject.Temperance;

/// <summary>
/// NCS - Node, Component, (Node)System architecture
///
/// The NodeSystemManager initializes all <see cref="NodeSystem"/>,
/// attaches them as children of itself so that they are present in the Godot scene tree,
/// then injects any <see cref="NodeSystem"/> dependencies they have with each other
/// </summary>
[GlobalClass]
public partial class NodeSystemManager : Node3D
{
    // Used to inject dependencies based on name of NodeSystem
    private readonly Dictionary<string, NodeSystem> _nodeSystemDictionary = [];

    public override void _EnterTree()
    {
        base._EnterTree();
        InitializeNodeSystems();
        InjectDependencies();
    }

    /// <summary>
    /// To initialize all <see cref="NodeSystem"/> we look for the classes with the [GlobalClass] attribute
    /// and if their name ends in "System"
    ///
    /// We initialize the <see cref="NodeSystem"/> and add it as a child of the NodeSystemManager so that it may
    /// subscribe to user-created signals and be a part of the Godot Scenetree for things like Input/Godot signals
    /// </summary>
    public void InitializeNodeSystems()
    {
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
            AddChild(nodeSystem);
        }
    }

    /// <summary>
    /// After all systems are initialized, system dependencies can be injected without worry of order of initialization
    /// </summary>
    public void InjectDependencies()
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

                if (field.FieldType.Name == GetType().Name)
                    field.SetValue(nodeSystem, this);
                
                if (field.FieldType.Name == SignalBus.Instance.GetType().Name)
                    field.SetValue(nodeSystem, SignalBus.Instance);
            }
        }
    }
}

/// <summary>
/// <see cref="NodeSystemManager"/> will run the InjectDepencies() function on each NodeSystem
/// Each <see cref="NodeSystem"/> will go through its PRIVATE field instances with this attribute
/// and assign the appropriate system to it
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class InjectedDependency : Attribute { }