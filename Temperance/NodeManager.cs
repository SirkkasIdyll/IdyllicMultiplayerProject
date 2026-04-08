using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace IdyllicMultiplayerProject.Temperance;

/// <summary>
/// NCS - Node, Component, (Node)System architecture
///
/// The NodeManager is responsible for getting, removing, or spawning specific Nodes
/// </summary>
public class NodeManager
{
    public static NodeManager Instance { get; } = new();
    private Node? _rootScene;
    public readonly MultiplayerSpawner MainSpawner = new();
    public readonly Dictionary<string, string> _nodeDictionary = []; // second value is the scene_file_path for spawning

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
        var directories = new List<string> { "res://Resources/Prototypes" };

        foreach (var directory in directories)
        {
            var files = ResourceLoader.ListDirectory(directory);
            foreach (var file in files)
            {
                var fileName = file;
            
                // We only care about the last part of the file name
                if (file.LastIndexOf('/') != -1)
                    fileName = file.Substring(0, file.LastIndexOf('/'));
            
                // No length means it's a directory
                if (fileName.Length == 0)
                    continue;
            
                // Don't care for Base nodes that are just used to created inherited scenes,
                // they're not intended to be spawned
                if (fileName.StartsWith("Base", true, null))
                    continue;

                // If it's not a .tscn then I don't know what it is
                if (!fileName.EndsWith(".tscn"))
                    continue;

                var nodeName = fileName.Substring(0, file.LastIndexOf('.'));
                var sceneFilePath = directory + "/" + file;
                _nodeDictionary.TryAdd(nodeName, sceneFilePath);
            }   
        }
    }
    
    public void InitializeNodeSpawner(Node rootScene)
    {
        _rootScene = rootScene;
        _rootScene.AddChild(MainSpawner);
        MainSpawner.SpawnPath = _rootScene.GetPath();
        foreach (var (nodeName, sceneFilePath) in _nodeDictionary)
        {
            MainSpawner.AddSpawnableScene(sceneFilePath);
        }
    }

    public bool TrySpawnNode(string nodeName, [NotNullWhen(true)] out Node? spawnedNode)
    {
        spawnedNode = null;
        
        if (spawnedNode == null)
            return false;

        return true;
    }
}