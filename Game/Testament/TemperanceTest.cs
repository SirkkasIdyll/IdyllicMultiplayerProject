using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Game.Shared.Systems.Movement;
using Game.Temperance.EntityComponentSystem;
using Game.Temperance.Network.Client;
using Game.Temperance.Network.Server;

namespace Game.Testament;

using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite][RequireGodotRuntime]
public class TemperanceTest
{
    private readonly Node _rootScene = new();
    private readonly ComponentManager _componentManager = ComponentManager.Instance;
    private readonly EntityManager _entityManager = EntityManager.Instance;
    private readonly EntitySystemManager _entitySystemManager = EntitySystemManager.Instance;

    [Before]
    public void Before()
    {
        _rootScene.TreeEntered += () =>
        {
            _entitySystemManager.InitializeNodeSystems(_rootScene);
            _rootScene.AddChild(_entityManager);
            
            ENetClient.Instance.Free();
            ENetServer.Instance.Free();
        };
        AddNode(_rootScene);
    }
    
    /// <summary>
    /// Tests the basic functionality of the ComponentManager
    /// </summary>
    [TestCase]
    public async Task ComponentManagerTest()
    {
        var testEntity = AddNode(new Entity());
        
        // Adds component
        AssertBool(_componentManager.TryAddComponent<MovementComponent>(testEntity)).AppendFailureMessage(
            "Failed to add component to entity.").IsTrue();
        
        // Gets component
        AssertBool(_componentManager.TryGetComponent<MovementComponent>(testEntity, out var movementComponent)).AppendFailureMessage(
            "Failed to retrieve component from entity after adding it.").IsTrue();
        AssertObject(movementComponent).IsNotNull();
        
        // Gets component again
        _componentManager.GetEntitiesWithComponent<MovementComponent>(out var list);
        AssertBool(list.Contains(testEntity)).AppendFailureMessage(
            "Failed to retrieve entity with a specific component type.").IsTrue();
        
        // Removes component and waits some time for QueueFree() to process
        _componentManager.RemoveComponent<MovementComponent>(testEntity);
        await Task.Delay(100);
        
        // Has component
        AssertBool(_componentManager.HasComponent<MovementComponent>(testEntity)).AppendFailureMessage(
            "Failed to remove component from a entity.").IsFalse();
        AssertBool(list.Contains(testEntity)).AppendFailureMessage(
            "Failed to remove entity from list of entities that have a specific component type.").IsFalse();
    }

    /// <summary>
    /// Tests that there are no duplicate names being used for entities 
    /// </summary>
    [TestCase]
    public void DuplicateEntityPrototypeTest()
    {
        Dictionary<string, string> entityDictionary = []; // second value is the scene_file_path for spawning
        
        var prototypePaths = _entityManager.RecursiveListDirectory("res://Resources/Prototypes");
            
        foreach (var prototypePath in prototypePaths)
        {
            // If it's not a .tscn then I don't know what it is
            if (!prototypePath.EndsWith(".tscn"))
                continue;

            var entityNameWithExtension = prototypePath.Remove(0, prototypePath.LastIndexOf('/') + 1);
            var entityName = entityNameWithExtension.Substring(0, entityNameWithExtension.LastIndexOf('.'));
            GD.Print("Node name: " + entityName);
            
            AssertBool(entityDictionary.TryAdd(entityName, prototypePath)).AppendFailureMessage("Duplicate entity " +
                "prototype with entity name: \"" + entityName + "\". Resolve the error by renaming the entity.").IsTrue();
        }
    }

    [TestCase]
    public void EntityManagerTest()
    {
        AssertBool(_entityManager.TrySpawnEntity("TheWorld", null, out _)).AppendFailureMessage(
            "Spawned entity despite not being assigned as the server.").IsFalse();
    }

    /// <summary>
    /// Tests the basic functionality of the EntitySystemManager
    /// </summary>
    [TestCase]
    public void EntitySystemManagerTest()
    {
        _entitySystemManager.TryGetEntitySystem<MovementSystem>(out var movementSystem);
        AssertObject(movementSystem).IsNotNull();
    }
}