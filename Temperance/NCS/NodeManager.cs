using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace IdyllicMultiplayerProject.Temperance;

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
    public readonly Dictionary<string, string> NodeDictionary = []; // second value is the scene_file_path for spawning

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
            
            NodeDictionary.TryAdd(nodeName, prototypePath);
        }
    }

    /// <summary>
    /// Recursively looks through a directory and returns only the res://file_path that end with a given extension
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="extension"></param>
    /// <returns></returns>
    private List<string> RecursiveListDirectory(string directory, string extension = ".tscn")
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