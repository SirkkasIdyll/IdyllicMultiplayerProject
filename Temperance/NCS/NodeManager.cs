using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;
using IdyllicMultiplayerProject.Temperance.Network;

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
    private Node? _rootScene;
    public readonly Dictionary<string, string> NodeScenePathDictionary = []; // second value is the scene_file_path for spawning
    public readonly Dictionary<Guid, NodeUpdateInfo> NetGuidDictionary = [];

    private NodeManager()
    {
        GetAllNodePrototypes();
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
    /// </summary>
    public bool TrySpawnNode(string nodeName, [NotNullWhen(true)] out Node3D? node3D)
    {
        return TrySpawnNode(nodeName, new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(1, 1, 1), out node3D);
    }

    /// <summary>
    /// Tries to spawn a node at global position, default rotation, and scale then add it as a child of the root scene
    /// </summary>
    public bool TrySpawnNode(string nodeName, Vector3 globalPosition, [NotNullWhen(true)] out Node3D? node3D)
    {
        return TrySpawnNode(nodeName, globalPosition, new Vector3(0, 0, 0), new Vector3(1, 1, 1), out node3D);
    }

    /// <summary>
    /// Tries to spawn a node at global position, global rotation, and default scale then add it as a child of the root scene
    /// </summary>
    public bool TrySpawnNode(string nodeName, Vector3 globalPosition, Vector3 globalRotation,
        [NotNullWhen(true)] out Node3D? node3D)
    {
        return TrySpawnNode(nodeName, globalPosition, globalRotation, new Vector3(1, 1, 1), out node3D);
    }

    /// <summary>
    /// Tries to spawn a node at global position, global rotation, and global scale then add it as a child of the root scene
    /// </summary>
    public bool TrySpawnNode(string nodeName, Vector3 globalPosition, Vector3 globalRotation, Vector3 globalScale,
        [NotNullWhen(true)] out Node3D? node3D)
    {
        node3D = null;
        if (!NodeScenePathDictionary.TryGetValue(nodeName, out var sceneFilePath))
            return false;

        node3D = GD.Load<PackedScene>(sceneFilePath).Instantiate<Node3D>();
        _rootScene?.AddChild(node3D);
        
        node3D.SetGlobalPosition(globalPosition);
        node3D.SetGlobalRotation(globalRotation);
        node3D.GlobalScale(globalScale);
        
        if (Networking.IsServer())
            NetGuidDictionary.Add(Guid.CreateVersion7(), new NodeUpdateInfo(node3D));
        
        return true;
    }
    
    /// <summary>
    /// One of the rare instances we use a Godot dictionary,
    /// because I absolutely HATE having to use Godot's <see cref="Variant"/>.
    ///
    /// Just have to assume the correct arguments are being passed.
    /// </summary>
    // public Node3D SpawnNode(Godot.Collections.Dictionary dictionary)
    // {
    //     var nodeName = dictionary["name"];
    //     var node = GD.Load<PackedScene>(NodeDictionary[(string)nodeName]).Instantiate<Node3D>();
    //
    //     if (dictionary.TryGetValue("spawnPosition", out var spawnPosition))
    //         node.GlobalPosition = (Vector3)spawnPosition;
    //     
    //     return node;
    // }
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