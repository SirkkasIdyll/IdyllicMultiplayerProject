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
    public Vector2 InputDirection = Vector2.Zero;
    
    [SynchronizedField(OnChange = true)]
    public float MovementSpeed = 4.5f;
}