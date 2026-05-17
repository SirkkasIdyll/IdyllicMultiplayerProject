using Godot;

namespace IdyllicMultiplayerProject.Temperance.NCS;

/// <summary>
/// NCS - Node, Component, (Node)System architecture
///
/// USE THE GLOBALCLASS ATTRIBUTE
/// GLOBALCLASS ONLY ADDED TO THE GODOT SCENE TREE BY THE <see cref="NodeSystemManager"/>
/// NodeSystems implement specific logic and coordinate the application of signals to nodes
/// </summary>
public abstract partial class NodeSystem : Node
{
    /// <summary>
    /// Set node name to the type that it is for easier retrieval in <see cref="NodeSystemManager"/>
    /// and for injecting dependencies by checking type by name
    /// Otherwise default name will look like @Node3D@1
    /// </summary>
    protected NodeSystem() { SetName(GetType().Name); }
}