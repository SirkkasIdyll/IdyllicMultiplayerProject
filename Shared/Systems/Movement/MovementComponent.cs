using System;
using Godot;
using IdyllicMultiplayerProject.Temperance;
using static Godot.SceneReplicationConfig;

namespace IdyllicMultiplayerProject.Shared.Systems.Movement;

/// <summary>
/// Allows movement, determines speed when moving, translates input to a movement direction
/// </summary>
[GlobalClass, Serializable]
public partial class MovementComponent : Component
{
    [SynchronizedField(ReplicationMode = ReplicationMode.OnChange)]
    public float MovementSpeed = 2f;
    
    [SynchronizedField]
    public Vector2 InputDirection = Vector2.Zero;
}