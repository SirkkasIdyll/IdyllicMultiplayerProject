using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ENet;
using Godot;
using Google.Protobuf.Collections;
using Game.Client.Services.GRpc.Spawn;
using Game.Resources.ProtocolBuffers;
using Game.Shared.Systems.Metadata;
using Game.Temperance.Network;
using Game.Temperance.Signals;
using Games.Resources.ProtocolBuffers;

namespace Game.Temperance.NCS;

/// <summary>
/// NCS - Node, Component, (Node)System architecture
///
/// The NodeManager is responsible for getting, removing, or spawning specific Nodes
/// The NodeManager is an actual Node to get access to interact with Godot's MultiplayerSpawner 
/// </summary>
public partial class NodeManager : Node
{
    public static NodeManager Instance { get; } = new();
    private readonly ComponentManager _componentManager = ComponentManager.Instance;
    private readonly SignalBus _signalBus = SignalBus.Instance;

    public readonly Dictionary<string, string> NodeScenePathDictionary = []; // second value is the scene_file_path for spawning
    public readonly Dictionary<Guid, NodeUpdateInfo> NetGuidDictionary = [];
    private readonly Queue<Tuple<Guid, string, RepeatedField<string>>> _deferredQueue = new();

    private NodeManager()
    {
        GetAllNodePrototypes();
    }

    public override void _Ready()
    {
        base._Ready();

        _signalBus.PeerDisconnectedSignal += OnPeerDisconnected;
        _signalBus.RequestSpawnSignal += OnRequestSpawn;
        _signalBus.ServerMessageTimerSignal += OnServerMessageTimer;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        while (_deferredQueue.Count > 0)
        {
            var (netGuid, nodeName, components) = _deferredQueue.Dequeue();
            if (!TrySpawnNode(nodeName, netGuid, out var node3D))
                continue;
            
            _componentManager.SyncComponentsOnSpawn(node3D, components);
        }
    }

    /// <summary>
    /// When disconnecting from the server, despawn everything
    /// </summary>
    private void OnPeerDisconnected(Event netEvent, ref PeerDisconnectedSignal args)
    {
        foreach (var (guid, _) in NetGuidDictionary)
            DespawnNode(guid);
    }

    private void OnServerMessageTimer(ref ServerMessageTimerSignal args)
    {
        if (!Networking.IsServer())
            return;
        
        var signal = new SendNodeStatesSignal();
        foreach (var (netGuid, _) in NetGuidDictionary)
            signal.Message.NodeState.Add(new NodeState
            {
                Sequence = Library.Time,
                NodeNetworkGuid = netGuid.ToString()
            });
        
        _signalBus.EmitSendNodeStatesSignal(ref signal);
        ENetServer.Instance.Broadcast(ENetChannels.SynchronizeNodes, signal.Message);
    }

    /// <summary>
    /// Client has received a message from the server telling it to spawn a node
    /// Defer the spawn to be handled in _process (async thread to main thraed)
    /// </summary>
    private void OnRequestSpawn(ref RequestSpawnSignal args)
    {
        // This should never be called on the server, but sanity check it just to make a point
        if (Networking.IsServer())
            return;
        
        _deferredQueue.Enqueue(Tuple.Create(args.NetGuid, args.ProtoName, args.Components));
    }

    /// <summary>
    /// Grabs all non-base nodes and their scene_file_paths for the _nodeDictionary
    /// so that we can spawn nodes freely by their name. Nodes are grabbed from the
    /// Client/Server/Shared->Nodes directory
    /// </summary>
    private void GetAllNodePrototypes()
    {
        var prototypePaths = RecursiveListDirectory("res://Resources/Prototypes");
        foreach (var prototypePath in prototypePaths)
        {
            var nodeNameWithExtension = prototypePath.Remove(0, prototypePath.LastIndexOf('/') + 1);
            var nodeName = nodeNameWithExtension.Substring(0, nodeNameWithExtension.LastIndexOf('.'));

            if (nodeName.StartsWith("Base", true, null))
                continue;
            
            NodeScenePathDictionary.TryAdd(nodeName, prototypePath);
        }
    }

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

    /// <summary>
    /// Tries to spawn a node at default position, rotation, and scale then add it as a child of the root scene
    /// Leave the netGuid null unless you know what it is
    /// </summary>
    public bool TrySpawnNode(string nodeName, Guid? netGuid, [NotNullWhen(true)] out Node3D? node3D)
    {
        return TrySpawnNode(nodeName, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(1, 1, 1), netGuid, out node3D);
    }

    /// <summary>
    /// Tries to spawn a node at global position, default rotation, and scale then add it as a child of the root scene
    /// Leave the netGuid null unless you know what it is
    /// </summary>
    public bool TrySpawnNode(string nodeName, Vector3 globalPosition, Guid? netGuid, [NotNullWhen(true)] out Node3D? node3D)
    {
        return TrySpawnNode(nodeName, globalPosition, new Vector3(0, 0, 0), new Vector3(1, 1, 1), netGuid, out node3D);
    }

    /// <summary>
    /// Tries to spawn a node at global position, global rotation, and default scale then add it as a child of the root scene
    /// Leave the netGuid null unless you know what it is
    /// </summary>
    public bool TrySpawnNode(string nodeName, Vector3 globalPosition, Vector3 globalRotation, Guid? netGuid,
        [NotNullWhen(true)] out Node3D? node3D)
    {
        return TrySpawnNode(nodeName, globalPosition, globalRotation, new Vector3(1, 1, 1), netGuid, out node3D);
    }

    /// <summary>
    /// Tries to spawn a node at global position, global rotation, and global scale then add it as a child of the root scene
    /// Leave the netGuid null unless you know what it is
    /// </summary>
    public bool TrySpawnNode(string nodeName, Vector3 globalPosition, Vector3 globalRotation, Vector3 globalScale, Guid? netGuid,
        [NotNullWhen(true)] out Node3D? node3D)
    {
        node3D = null;
        if (!NodeScenePathDictionary.TryGetValue(nodeName, out var sceneFilePath))
            return false;
        
        // Server can come up with its own guid, clients should have received the guid from the server
        if (Networking.IsServer())
            netGuid ??= Guid.CreateVersion7();

        // If it doesn't have a network guid then it doesn't exist on the server 
        if (netGuid == null)
            return false;

        node3D = GD.Load<PackedScene>(sceneFilePath).Instantiate<Node3D>();
        GetParent().AddChild(node3D);
        
        node3D.SetGlobalPosition(globalPosition);
        node3D.SetGlobalRotation(globalRotation);
        node3D.GlobalScale(globalScale);
        
        _componentManager.TryGetComponent<MetadataComponent>(node3D, out var metadataComponent);
        NetGuidDictionary.Add(netGuid.Value, new NodeUpdateInfo(node3D, metadataComponent));
        var signal = new NodeSpawnedSignal(netGuid.Value);
        _signalBus.EmitNodeSpawnedSignal(netGuid.Value, ref signal);
        
        return true;
    }

    /// <summary>
    /// Removes node from tracked guids and frees the node
    /// </summary>
    /// <param name="netGuid"></param>
    public void DespawnNode(Guid netGuid)
    {
        if (!NetGuidDictionary.TryGetValue(netGuid, out var nodeUpdateInfo))
            return;

        var node =  nodeUpdateInfo.Node;

        var signal = new NodeDespawningSignal(node);
        _signalBus.EmitNodeDespawningSignal(node, ref signal);
        NetGuidDictionary.Remove(netGuid);
        node.QueueFree();
    }
}

public class NodeSpawnedSignal : UserSignalArgs
{
    public Guid NetGuid;
    
    public NodeSpawnedSignal(Guid netGuid)
    {
        NetGuid = netGuid;
    }
}

public class NodeDespawningSignal : UserSignalArgs
{
    public Node3D Node3D;

    public NodeDespawningSignal(Node3D node3D)
    {
        Node3D = node3D;
    }
}

public struct NodeUpdateInfo
{
    public readonly Node3D Node;
    // This exists here because we can't TryGetComponent<> in async threads i.e. gRPC services
    public readonly MetadataComponent? MetadataComponent;
    public uint LastUpdated;

    public NodeUpdateInfo(Node3D node, MetadataComponent? metadataComponent)
    {
        Node = node;
        MetadataComponent = metadataComponent;
    }
}

public class SendNodeStatesSignal : UserSignalArgs
{
    public NodeStates Message = new();
}

public class ReceiveNodeStatesSignal : UserSignalArgs
{
    public NodeStates Message = new();
}