using Godot;
using Game.Temperance.NCS;

namespace Game.Shared.Systems.Movement;

/// <summary>
/// Allows movement, determines speed when moving, translates input to a movement direction
/// </summary>
[GlobalClass, Synchronized]
public partial class MovementComponent : Component
{
    [SynchronizedField]
    public float MovementSpeed = 2f;
    
    public Vector2 InputDirection = Vector2.Zero;
}