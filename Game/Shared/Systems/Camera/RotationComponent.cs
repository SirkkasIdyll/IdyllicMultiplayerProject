using Game.Temperance.NCS;
using Godot;

namespace Game.Shared.Systems.Camera;

/// <summary>
/// Solely responsible for storing the "real" rotation angle of the node
/// without immediately affecting the hitbox or appearance of a player
/// until the rotation hits certain thresholds
/// </summary>
[GlobalClass]
public partial class RotationComponent : Component
{
    // Unique hitboxes should be rotated in time with the sprite's Flip H
    // Y rotation should be set to 180
    [Export]
    public CollisionShape3D? CollisionShape3D;

    // Sprite to be oriented based on what the node's rotation is
    // Based on a sprite with two Hframes
    // Animation frame 0 = Front facing
    // Animation frame 1 = Back facing
    // Flip H false = Right facing
    // Flip H true = Left Facing
    [Export]
    public Sprite3D? Sprite3D;
}