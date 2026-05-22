using System.Collections.Generic;
using Godot;
using IdyllicMultiplayerProject.Temperance.NCS;

namespace IdyllicMultiplayerProject.Shared.Systems.Metadata;

/// <summary>
/// Serves as a dedicated place to get certain metadata that's useful elsewhere
/// </summary>
[GlobalClass]
public partial class MetadataComponent : Component
{
    public string PrototypeName = null!;
    public readonly Dictionary<string, Component> ComponentDictionary = [];

    public override void _Ready()
    {
        base._Ready();

        var prototypePath = GetParent().SceneFilePath;
        var protoNameWithExtension = prototypePath.Remove(0, prototypePath.LastIndexOf('/') + 1);
        PrototypeName = protoNameWithExtension.Substring(0, protoNameWithExtension.LastIndexOf('.'));
    }
}