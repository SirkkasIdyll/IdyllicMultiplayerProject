using Godot;
using Game.Temperance.NCS;

namespace Game.Shared.Systems.Movement;

/// <summary>
/// Allows movement, determines speed when moving, translates input to a movement direction
/// </summary>
[GlobalClass]
public partial class MovementComponent : Component
{
    [SynchronizedField(Min = 0f, Max = 1f, Precision = 0.1f)]
    public Vector2 InputDirection = Vector2.Zero;
    
    [Export]
    [SynchronizedField(OnChange = true, Min = 0f, Max = 20f, Precision = 0.01f)]
    public float MovementSpeed = 7.5f;
}