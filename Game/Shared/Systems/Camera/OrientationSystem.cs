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
}