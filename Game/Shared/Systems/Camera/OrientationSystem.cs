using Game.Temperance.NCS;
using Game.Temperance.Signals;
using Godot;

namespace Game.Shared.Systems.Camera;

[GlobalClass]
public partial class OrientationSystem : NodeSystem
{
    [InjectedDependency] private readonly ComponentManager _componentManager = null!;
    [InjectedDependency] private readonly NodeManager _nodeManager = null!;
    // [InjectedDependency] private readonly NodeSystemManager _nodeSystemManager = null!;
    [InjectedDependency] private readonly SignalBus _signalBus = null!;
    
    /// <summary>
    /// Given a normalized vector, sets the rotation of a character
    /// and then updates their sprite and hitboxes if necessary
    /// </summary>
    /// <param name="node"></param>
    /// <param name="normalized"></param>
    public void OrientCharacter(Node<RotationComponent> node, Vector2 normalized)
    {
        if (node.Comp == null)
            return;

        if (node.Comp.Sprite3D == null)
            return;
        
        // Sprites shows back between 30 and 150 degrees, front otherwise
        node.Comp.Sprite3D.Frame = normalized.Angle() is <= -Mathf.Pi / 6f and >= -5f * Mathf.Pi / 6f ? 1 : 0;
        
        // Sprite faces right from 270 and 90 degrees
        var initialFlipH = node.Comp.Sprite3D.FlipH;
        node.Comp.Sprite3D.FlipH = normalized.Angle() is < -Mathf.Pi / 2 or > Mathf.Pi / 2f;
        
        if (initialFlipH == node.Comp.Sprite3D.FlipH)
            return;
        
        // Flippy animation tween when looking left-right
        var scale = node.Comp.Sprite3D.FlipH ? 1f : -1f;
        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(node.Comp.Sprite3D, "scale", new Vector3(scale, 1, 1), 0.15f);
    }
    
    /// <summary>
    /// Orients the character's sprite to the correct facing given the inputs
    /// </summary>
    // public void OrientCharacterSprite(CharacterBody2D node, bool? faceRight = null, bool? faceForward = null)
    // {
    //     var canvasGroup = node.GetNode<CanvasGroup>("CanvasGroup");
    //
    //     // Swaps between the front-facing and back-facing sprites
    //     if (faceForward != null)
    //     {
    //         foreach (var child in canvasGroup.GetChildren())
    //         {
    //             if (child is not AnimatedSprite2D animatedSprite2D)
    //                 continue;
    //
    //             if (animatedSprite2D.SpriteFrames == null)
    //                 continue;
    //         
    //             animatedSprite2D.Animation = faceForward.Value ? "default" : "back";
    //         }
    //     }
    //
    //     
    //     // Turns the character left or right by flipping the x scale
    //     if (faceRight != null)
    //     {
    //         var orientationVector = faceRight.Value ? new Vector2(1, 1) : new Vector2(-1, 1);
    //         var tween = CreateTween();
    //         tween.SetEase(Tween.EaseType.Out);
    //         tween.TweenProperty(canvasGroup, "scale", orientationVector, 0.1f);
    //     }
    // }
}