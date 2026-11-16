using Game.Shared.Systems.Camera;
using Game.Temperance.NCS;
using Game.Temperance.Network;
using Game.Temperance.Signals;
using Godot;

namespace Game.Shared.Systems.Visuals;

/// <summary>
/// Controls various visual elements, at the moment deals with the desynchronization of
/// the visual location/rotation of a sprite versus the actual underlying position/rotation of the parent node
/// </summary>
[GlobalClass]
public partial class VisualSystem : NodeSystem
{
    [InjectedDependency] private readonly ComponentManager _componentManager = null!;
    [InjectedDependency] private readonly NodeManager _nodeManager = null!;
    [InjectedDependency] private readonly NodeSystemManager _nodeSystemManager = null!;
    [InjectedDependency] private readonly SignalBus _signalBus = null!;
    
    public override void _Ready()
    {
        base._Ready();

        _signalBus.ComponentsRegisteredSignal += OnComponentsRegistered;
    }
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (Networking.IsServer())
            return;
        
        _componentManager.GetNodesWithComponent<CameraComponent>(out var cameraNodes);
        foreach (var node in cameraNodes)
        {
            if (!_componentManager.TryGetComponent<CameraComponent>(node, out var cameraComponent))
                continue;

            if (node is not Node3D node3D)
                continue;
            
            node3D.Basis = new Basis(Vector3.Up, cameraComponent.AngleOfCameraPositionFromDefaultPosition);
        }

        _componentManager.GetNodesWithComponent<SpriteComponent>(out var spriteNodes);
        foreach (var node in spriteNodes)
        {
            if (!_componentManager.TryGetComponent<SpriteComponent>(node, out var spriteComponent))
                continue;
            
            OrientSpriteToParentRotation((node, spriteComponent));
        }
    }

    private void OnComponentsRegistered(Node node, ref ComponentsRegisteredSignal args)
    {
        SetTopLevelForVisualComponents(node);
    }
}