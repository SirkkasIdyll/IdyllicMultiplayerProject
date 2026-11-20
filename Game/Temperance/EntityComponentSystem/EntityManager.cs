using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ENet;
using Godot;
using Game.Temperance.Network;
using Game.Temperance.Signals;

namespace Game.Temperance.EntityComponentSystem;

/// <summary>
/// ECS - Entity, Component, (Entity)System architecture
///
/// The EntityManager is responsible for getting, removing, or spawning specific Entities
/// The EntityManager is an actual Node to get access to interact with Godot's MultiplayerSpawner 
/// </summary>
public partial class EntityManager : Node
{
    public static EntityManager Instance { get; } = new();
    private readonly ComponentManager _componentManager = ComponentManager.Instance;
    private readonly SignalBus _signalBus = SignalBus.Instance;

    public readonly Dictionary<string, string> NodeScenePathDictionary = []; // second value is the scene_file_path for spawning
    public readonly Dictionary<Entity, ushort> EntityNetworkIdDictionary = new();
    public readonly Dictionary<ushort, Entity> NetworkIdEntityDictionary = new();

    private EntityManager()
    {
        GetAllEntityPrototypes();
    }

    public override void _Ready()
    {
        base._Ready();

        _signalBus.PeerDisconnectedSignal += OnPeerDisconnected;
    }

    /// <summary>
    /// When disconnecting from the server, despawn everything
    /// </summary>
    private void OnPeerDisconnected(Event netEvent, ref PeerDisconnectedSignal args)
    {
        // foreach (var (guid, _) in NetGuidDictionary)
        //     DespawnEntity(guid);
    }
    
    /// <summary>
    /// Removes entity from tracked guids and frees the node
    /// </summary>
    /// <param name="netGuid"></param>
    // public void DespawnEntity(Guid netGuid)
    // {
        // if (!NetGuidDictionary.TryGetValue(netGuid, out var entityUpdateInfo))
        //     return;
        //
        // var entity =  nodeUpdateInfo.Node;
        //
        // var signal = new NodeDespawningSignal(entity);
        // _signalBus.EmitNodeDespawningSignal(entity, ref signal);
        // NetGuidDictionary.Remove(netGuid);
        // entity.QueueFree();
    // }

    /// <summary>
    /// Recursively looks through a directory and returns only the res://file_path that end with a given extension
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="extension"></param>
    /// <returns></returns>
    public List<string> RecursiveListDirectory(string directory, string extension = ".tscn")
    {
        var result = new List<string>();
        var listedDirectory = ResourceLoader.ListDirectory(directory);
        foreach (var listedFile in listedDirectory)
        {
            // File is a directory, grab all files within it
            if (listedFile.EndsWith('/'))
                result.AddRange(RecursiveListDirectory(directory + '/' + listedFile, extension));

            if (!listedFile.EndsWith(extension))
                continue;
            
            if (directory.EndsWith('/'))
                result.Add(directory + listedFile);
            else
                result.Add(directory + '/' + listedFile);
        }

        return result;
    }

    public string GetNodeName(Entity entity)
    {
        var fileBaseName = entity.SceneFilePath.GetBaseName();
        var slashIndexPosition = fileBaseName.LastIndexOf('/');
        return fileBaseName.Substring(slashIndexPosition + 1,  fileBaseName.Length - (slashIndexPosition + 1));
    }

    /// <summary>
    /// Tries to spawn a entity at default position, rotation, and scale then add it as a child of the root scene
    /// Leave the entityNetworkId null unless you know what it is
    /// </summary>
    public bool TrySpawnEntity(string entityName, ushort? entityNetworkId, [NotNullWhen(true)] out Entity? entity)
    {
        return TrySpawnEntity(entityName, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(1, 1, 1), entityNetworkId, out entity);
    }

    /// <summary>
    /// Tries to spawn a entity at global position, default rotation, and scale then add it as a child of the root scene
    /// Leave the entityNetworkId null unless you know what it is
    /// </summary>
    public bool TrySpawnEntity(string entityName, Vector3 globalPosition, ushort? entityNetworkId, [NotNullWhen(true)] out Entity? entity)
    {
        return TrySpawnEntity(entityName, globalPosition, new Vector3(0, 0, 0), new Vector3(1, 1, 1), entityNetworkId, out entity);
    }

    /// <summary>
    /// Tries to spawn a entity at global position, global rotation, and default scale then add it as a child of the root scene
    /// Leave the entityNetworkId null unless you know what it is
    /// </summary>
    public bool TrySpawnEntity(string entityName, Vector3 globalPosition, Vector3 globalRotation, ushort? entityNetworkId,
        [NotNullWhen(true)] out Entity? entity)
    {
        return TrySpawnEntity(entityName, globalPosition, globalRotation, new Vector3(1, 1, 1), entityNetworkId, out entity);
    }

    /// <summary>
    /// Tries to spawn a entity at global position, global rotation, and global scale then add it as a child of the root scene
    /// Leave the entityNetworkId null unless you know what it is
    /// </summary>
    public bool TrySpawnEntity(string entityName, Vector3 globalPosition, Vector3 globalRotation, Vector3 globalScale, ushort? entityNetworkId,
        [NotNullWhen(true)] out Entity? entity)
    {
        entity = null;
        // TODO: This will stop working once we add in despawning
        // Server can come up with its own network ID, clients should have received the network ID from the server
        if (Networking.IsServer())
            entityNetworkId ??= (ushort)EntityNetworkIdDictionary.Count;

        // If it doesn't have a network ID then it doesn't exist on the server so it shouldn't exist on the client...
        if (entityNetworkId == null)
            return false;

        // Node with that network ID already exists, no need to spawn it again
        if (NetworkIdEntityDictionary.TryGetValue(entityNetworkId.Value, out entity))
            return true;
        
        if (!NodeScenePathDictionary.TryGetValue(entityName, out var sceneFilePath))
            return false;
        
        entity = GD.Load<PackedScene>(sceneFilePath).Instantiate<Entity>();
        GetParent().AddChild(entity);
        
        entity.SetGlobalPosition(globalPosition);
        entity.SetGlobalRotation(globalRotation);
        entity.GlobalScale(globalScale);

        EntityNetworkIdDictionary.Add(entity, entityNetworkId.Value);
        NetworkIdEntityDictionary.Add(entityNetworkId.Value, entity);

        var signal = new EntitySpawnedSignal(entity);
        _signalBus.EmitEntitySpawnedSignal(entity, ref signal);
        return true;
    }
    
    /// <summary>
    /// Grabs all non-base entities and their scene_file_paths for the _entityDictionary
    /// so that we can spawn entities freely by their name. Entities are grabbed from the
    /// Client/Server/Shared->Entities directory
    /// </summary>
    private void GetAllEntityPrototypes()
    {
        var prototypePaths = RecursiveListDirectory("res://Resources/Prototypes");
        foreach (var prototypePath in prototypePaths)
        {
            var entityNameWithExtension = prototypePath.Remove(0, prototypePath.LastIndexOf('/') + 1);
            var entityName = entityNameWithExtension.Substring(0, entityNameWithExtension.LastIndexOf('.'));

            if (entityName.StartsWith("Base", true, null))
                continue;
            
            NodeScenePathDictionary.TryAdd(entityName, prototypePath);
        }
    }
}

public class EntitySpawnedSignal(Entity entity) : UserSignalArgs
{
    private Entity _entity = entity;
}