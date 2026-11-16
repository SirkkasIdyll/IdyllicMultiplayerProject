using Game.Shared.Systems.Camera;
using Game.Temperance.NCS;
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
    private void OrientSpriteToParentRotation(Node<SpriteComponent> node)
    {
        if (node.Owner is not Node3D node3D)
            return;
        
        if (node.Comp?.Sprite3D == null)
            return;
        
        var signal = new UpdateSpriteRotationSignal(node);
        _signalBus.EmitUpdateSpriteRotationSignal(node, ref signal);
        
        // Sprites shows back between 30 and 150 degrees, front otherwise
        node.Comp.Sprite3D.Frame = node3D.Basis.GetEuler().Y is <= -Mathf.Pi / 6f and >= -5f * Mathf.Pi / 6f ? 1 : 0;
        
        // Sprite faces right from 270 and 90 degrees
        var initialFlipH = node.Comp.Sprite3D.FlipH;
        node.Comp.Sprite3D.FlipH = node3D.Basis.GetEuler().Y is < -Mathf.Pi / 2 or > Mathf.Pi / 2f;
        
        if (initialFlipH == node.Comp.Sprite3D.FlipH)
            return;
        
        // Flippy animation tween when looking left-right
        var scale = node.Comp.Sprite3D.FlipH ? -1f : 1f;
        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(node.Comp.Sprite3D, "scale", new Vector3(scale, 1, 1), 0.15f);
        
        // NOTE: I DON'T KNOW IF THIS ACTUALLY WORKS
        // Turn the hitbox as well
        // node.Comp?.CollisionShape3D?.Scale = new Vector3(scale, 1, 1);
    }
}

public class UpdateSpriteRotationSignal(Node<SpriteComponent> node) : UserSignalArgs
{
    private Node<SpriteComponent> _node = node;
}