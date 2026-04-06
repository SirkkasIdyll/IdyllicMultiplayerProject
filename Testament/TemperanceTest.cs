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
    private readonly NodeSystemManager _nodeSystemManager = NodeSystemManager.Instance;
    private readonly ComponentManager _componentManager = ComponentManager.Instance;

    [Before]
    public void Before()
    {
        AddNode(_rootScene);
        _nodeSystemManager.InitializeNodeSystems(_rootScene);
    }

    [TestCase]
    public void NodeSystemManagerTest()
    {
        _nodeSystemManager.TryGetNodeSystem<MovementSystem>(out var movementSystem);
        AssertObject(movementSystem).IsNotNull();
    }

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
}