using Game.Temperance.EntityComponentSystem;
using Game.Temperance.Signals;
using Godot;

namespace Game.Shared.Systems.Visuals;

/// <summary>
/// 
/// </summary>
public partial class VisualSystem
{
    /// <summary>
    /// Given a normalized vector, sets the rotation of a character
    /// and then updates their sprite and hitboxes if necessary
    /// </summary>
    /// <param name="node"></param>
    /// <param name="normalized"></param>
    private void OrientSpriteToParentRotation(Entity<SpriteComponent> entity)
    {
        if (entity.Comp?.Sprite3D == null)
            return;
        
        var signal = new UpdateSpriteRotationSignal(entity);
        _signalBus.EmitUpdateSpriteRotationSignal(entity, ref signal);
        
        // Sprites shows back between 30 and 150 degrees, front otherwise
        entity.Comp.Sprite3D.Frame = entity.Owner.Basis.GetRotationQuaternion().GetAngle() is <= -Mathf.Pi / 6f and >= -5f * Mathf.Pi / 6f ? 1 : 0;
        
        // Sprite faces right from 270 and 90 degrees
        var initialFlipH = entity.Comp.Sprite3D.FlipH;
        entity.Comp.Sprite3D.FlipH = entity.Owner.Basis.GetRotationQuaternion().GetAngle() is < -Mathf.Pi / 2 or > Mathf.Pi / 2f;
        
        if (initialFlipH == entity.Comp.Sprite3D.FlipH)
            return;
        
        // Flippy animation tween when looking left-right
        var scale = entity.Comp.Sprite3D.FlipH ? -1f : 1f;
        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(entity.Comp.Sprite3D, "scale", new Vector3(scale, 1, 1), 0.15f);
        
        // NOTE: I DON'T KNOW IF THIS ACTUALLY WORKS
        // Turn the hitbox as well
        // entity.Comp?.CollisionShape3D?.Scale = new Vector3(scale, 1, 1);
    }
}

public class UpdateSpriteRotationSignal(Entity<SpriteComponent> entity) : UserSignalArgs
{
    private Entity<SpriteComponent> _entity = entity;
}