using Game.Client.Scenes;
using Game.Shared.Systems.Camera;
using Game.Temperance.EntityComponentSystem;
using Game.Temperance.Network;
using Game.Temperance.Network.Client;
using Game.Temperance.Signals;
using Godot;

namespace Game.Shared.Systems.Visuals;

/// <summary>
/// Controls various visual elements, at the moment deals with the desynchronization of
/// the visual location/rotation of a sprite versus the actual underlying position/rotation of the parent node
/// </summary>
[GlobalClass]
public partial class VisualSystem : EntitySystem
{
    [InjectedDependency] private readonly ComponentManager _componentManager = null!;
    [InjectedDependency] private readonly EntityManager _entityManager = null!;
    [InjectedDependency] private readonly EntitySystemManager _entitySystemManager = null!;
    [InjectedDependency] private readonly SignalBus _signalBus = null!;
    
    private const string DebugOptionName = "Sprite Orientation";
    
    /// <summary>
    /// The components that have a visual element to them,
    /// and need to have their transforms separated from the parent node's physics body
    /// for the purposes of smoothing appearance to underlying physics values
    /// </summary>
    private string[] _visualComponentNames =
    [
        nameof(CameraComponent),
        nameof(PersonalVisionComponent),
        nameof(SpriteComponent)
    ];
    
    public override void _Ready()
    {
        base._Ready();

        _signalBus.FetchDebugMenuOptions += OnFetchDebugMenuOptions;
        _signalBus.UpdateDebugLabel += OnUpdateDebugLabel;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Networking.IsServer())
            return;
        
        _componentManager.GetEntitiesWithComponent<SpriteComponent>(out var spriteEntities);
        foreach (var entity in spriteEntities)
        {
            if (!_componentManager.TryGetComponent<SpriteComponent>(entity, out var spriteComponent))
                continue;
            
            OrientSpriteToParentRotation((entity, spriteComponent));
        }

        // foreach (var (entity, _) in _componentManager.EntitiesWithComponentDictionary)
        // {
        //     LerpVisualComponentPositions(entity);
        // }
    }
    
    /// <summary>
    /// Add visual system to debug display options
    /// </summary>
    /// <param name="args"></param>
    private void OnFetchDebugMenuOptions(ref FetchDebugMenuOptions args)
    {
        args.SystemsList?.AddItem(DebugOptionName);
    }
    
    private void OnUpdateDebugLabel(string selectedSystem, ref UpdateDebugLabel args)
    {
        if (args.DebugLabel is null)
            return;

        if (selectedSystem != DebugOptionName)
            return;
        
        if (Networking.IsServer())
            return;

        if (ENetClient.Instance.ClientControlledEntity == null)
            return;

        if (!_componentManager.TryGetComponent<SpriteComponent>(ENetClient.Instance.ClientControlledEntity,
                out var spriteComponent))
            return;
        
        var horizontalFacing = spriteComponent.Sprite3D?.FlipH == true ? "left" : "right";
        var verticalFacing = spriteComponent.Sprite3D?.Frame == 0 ? "down" : "up";

        args.DebugLabel.Clear();
        args.DebugLabel.AppendText("Sprite is facing " + horizontalFacing + " and " + verticalFacing + "\n");
        args.DebugLabel.AppendText("Sprite's basis is " + ENetClient.Instance.ClientControlledEntity.Basis.GetRotationQuaternion().GetAngle() + "\n");
        GD.Print(ENetClient.Instance.ClientControlledEntity.Basis.GetRotationQuaternion().GetAngle());
    }
}