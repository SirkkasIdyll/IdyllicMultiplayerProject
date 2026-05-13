using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ENet;
using Godot;
using IdyllicMultiplayerProject.Temperance.Network;
using IdyllicMultiplayerProject.Temperance.Signals;

namespace IdyllicMultiplayerProject.Temperance.NCS;

/// <summary>
/// NCS - Node, Component, (Node)System architecture
///
/// The NodeManager is responsible for getting, removing, or spawning specific Nodes
/// The NodeManager is an actual Node to get access to interact with Godot's MultiplayerSpawner 
/// </summary>
public partial class NodeManager : Node
{
    public static NodeManager Instance { get; } = new();
    public readonly Dictionary<string, string> NodeScenePathDictionary = []; // second value is the scene_file_path for spawning
    public readonly Dictionary<Guid, NodeUpdateInfo> NetGuidDictionary = [];
    private readonly SignalBus _signalBus = SignalBus.Instance;
    private readonly Queue<Tuple<Guid, string>> _deferredQueue = new();

    private NodeManager()
    {
        GetAllNodePrototypes();
    }

    public override void _Ready()
    {
        base._Ready();

        _signalBus.PeerDisconnectedSignal += OnPeerDisconnected;
        _signalBus.RequestSpawnNodeSignal += OnRequestSpawnNode;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        while (_deferredQueue.Count > 0)
        {
            var (netGuid, nodeName) = _deferredQueue.Dequeue();
            TrySpawnNode(nodeName, netGuid, out _);
        }
    }

    private void OnPeerDisconnected(Event netEvent)
    {
        ClearNetGuidDictionary();
    }

    /// <summary>
    /// Client has received a message from the server telling it to spawn a node
    /// Defer the spawn to be handled in _process (async thread to main thraed)
    /// </summary>
    private void OnRequestSpawnNode(Guid netGuid, string nodeName)
    {
        _deferredQueue.Enqueue(Tuple.Create(netGuid, nodeName));
    }

    private void ClearNetGuidDictionary()
    {
        foreach (var (guid, nodeUpdateInfo) in NetGuidDictionary)
        {
            var node3D = nodeUpdateInfo.Node;
            node3D.QueueFree();
        }
        
        NetGuidDictionary.Clear();
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
        
        NetGuidDictionary.Add(netGuid.Value, new NodeUpdateInfo(node3D));
        SignalBus.Instance.EmitNodeSpawnedSignal(netGuid.Value);
        
        return true;
    }
}

public struct NodeUpdateInfo
{
    public readonly Node3D Node;
    public uint LastUpdated;

    public NodeUpdateInfo(Node3D node)
    {
        Node = node;
    }
}