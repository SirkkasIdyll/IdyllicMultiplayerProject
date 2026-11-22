using Godot;
using Game.Temperance.EntityComponentSystem;

namespace Game.Shared.Systems.Movement;

/// <summary>
/// Allows movement, determines speed when moving, translates input to a movement direction
/// </summary>
[GlobalClass][Tool]
public partial class MovementComponent : Component
{
    [Export]
    public CharacterBody3D? CharacterBody;
    
    [Export]
    public CollisionShape3D? CollisionShape;
    
    [SynchronizedField(Min = 0f, Max = 1f, Precision = 0.1f)]
    public Vector2 InputDirection = Vector2.Zero;
    
    [Export]
    [SynchronizedField(OnChange = true, Min = 0f, Max = 20f, Precision = 0.01f)]
    public float MovementSpeed = 7.5f;
    
    public override void _Ready()
    {
        base._Ready();

        if (CharacterBody == null)
        {
            CharacterBody = new CharacterBody3D();
            AddChild(CharacterBody);
            CharacterBody.Owner = GetTree().EditedSceneRoot;
        }
        
        if (CollisionShape == null)
        {
            CollisionShape = new CollisionShape3D();
            CharacterBody.AddChild(CollisionShape);
            CollisionShape.Owner = GetTree().EditedSceneRoot;
        }
    }
}