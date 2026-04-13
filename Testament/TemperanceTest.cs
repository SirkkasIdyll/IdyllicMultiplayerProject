using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using IdyllicMultiplayerProject.Shared.Systems.Movement;
using IdyllicMultiplayerProject.Temperance;

namespace IdyllicMultiplayerProject.Testament;

using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite][RequireGodotRuntime]
public class TemperanceTest
{
    private readonly Node _rootScene = new();
    private readonly ComponentManager _componentManager = ComponentManager.Instance;
    private readonly NodeManager _nodeManager = NodeManager.Instance;
    private readonly NodeSystemManager _nodeSystemManager = NodeSystemManager.Instance;

    [Before]
    public void Before()
    {
        AddNode(_rootScene);
        _nodeSystemManager.InitializeNodeSystems(_rootScene);
    }
    
    /// <summary>
    /// Tests the basic functionality of the ComponentManager
    /// </summary>
    [TestCase]
    public async Task ComponentManagerTest()
    {
        var testNode = AddNode(new Node3D());
        
        // Adds component
        AssertBool(_componentManager.TryAddComponent<MovementComponent>(testNode)).IsTrue();
        
        // Gets component
        AssertBool(_componentManager.TryGetComponent<MovementComponent>(testNode, out var movementComponent)).IsTrue();
        AssertObject(movementComponent).IsNotNull();
        
        // Gets component again
        _componentManager.GetNodesWithComponent<MovementComponent>(out var list);
        AssertBool(list.Contains(testNode)).IsTrue();
        
        // Removes component and waits some time for QueueFree() to process
        _componentManager.RemoveComponent<MovementComponent>(testNode);
        await Task.Delay(100);
        
        // Has component
        AssertBool(_componentManager.HasComponent<MovementComponent>(testNode)).IsFalse();
        AssertBool(list.Contains(testNode)).IsFalse();
    }

    /// <summary>
    /// Tests that there are no duplicate names being used for nodes 
    /// </summary>
    [TestCase]
    public void DuplicateNodePrototypeTest()
    {
        Dictionary<string, string> nodeDictionary = []; // second value is the scene_file_path for spawning
        var files = new List<string>();
        files.AddRange(ResourceLoader.ListDirectory("res://Resources/Prototypes"));
        
        foreach (var file in files)
        {
            var fileName = file;
            
            // We only care about the last part of the file name
            if (file.LastIndexOf('/') != -1)
                fileName = file.Substring(0, file.LastIndexOf('/'));
            
            // No length means it's a directory
            if (fileName.Length == 0)
                continue;

            // If it's not a .tscn then I don't know what it is
            if (!fileName.EndsWith(".tscn"))
                continue;

            var nodeName = fileName.Substring(0, file.LastIndexOf('.'));
            AssertBool(nodeDictionary.TryAdd(nodeName, file)).AppendFailureMessage("Duplicate node " +
                "prototype with node name: \"" + nodeName + "\". Resolve the error by renaming the node.").IsTrue();
        }
    }

    [TestCase]
    public void NodeManagerTest()
    {
        
    }

    /// <summary>
    /// Tests the basic functionality of the NodeSystemManager
    /// </summary>
    [TestCase]
    public void NodeSystemManagerTest()
    {
        _nodeSystemManager.TryGetNodeSystem<MovementSystem>(out var movementSystem);
        AssertObject(movementSystem).IsNotNull();
    }
}