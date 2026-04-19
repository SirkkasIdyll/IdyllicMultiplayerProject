using Godot;
using IdyllicMultiplayerProject.Temperance.NCS;
using static Godot.SceneReplicationConfig;

namespace IdyllicMultiplayerProject.Shared.Systems.Movement;

/// <summary>
/// Allows movement, determines speed when moving, translates input to a movement direction
/// </summary>
[GlobalClass, Synchronized]
public partial class MovementComponent : Component
{
    [SynchronizedField(ReplicationMode = ReplicationMode.OnChange)]
    public float MovementSpeed = 2f;
    
    public Vector2 InputDirection = Vector2.Zero;
}