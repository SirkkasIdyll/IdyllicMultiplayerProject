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
    
    public override void _Ready()
    {
        base._Ready();

        _signalBus.RegisterDebugMenuOptions += OnRegisterDebugMenuOptions;
        _signalBus.UpdateDebugLabel += OnUpdateDebugLabel;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
    
        if (Networking.IsServer())
            return;
        
        _componentManager.GetEntitiesWithComponent<SpriteComponent>(out var spriteEntities);
        foreach (var entity in spriteEntities)
        {
            if (!_componentManager.TryGetComponent<SpriteComponent>(entity, out var spriteComponent))
                continue;
            
            OrientSpriteToParentRotation((entity, spriteComponent));
            spriteComponent.Sprite3D?.GlobalPosition =
                entity.VisualGlobalTransform.Origin.Lerp(entity.GlobalTransform.Origin, 0.95f);
        }
    }
    
    /// <summary>
    /// Add visual system to debug display options
    /// </summary>
    /// <param name="args"></param>
    private void OnRegisterDebugMenuOptions(ref RegisterDebugMenuOptions args)
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

        var entity = ENetClient.Instance.ClientControlledEntity;

        if (!_componentManager.TryGetComponent<SpriteComponent>(entity, out var spriteComponent))
            return;
        
        var horizontalFacing = spriteComponent.Sprite3D?.FlipH == true ? "left" : "right";
        var verticalFacing = spriteComponent.Sprite3D?.Frame == 0 ? "down" : "up";

        args.DebugLabel.Clear();
        args.DebugLabel.AppendText("Sprite is facing " + horizontalFacing + " and " + verticalFacing + "\n");
        args.DebugLabel.AppendText("Sprite's basis is " + entity.Basis.ToString("0.00") + "\n");
        args.DebugLabel.AppendText("Sprite's quaternion is " + entity.Basis.GetRotationQuaternion().ToString("0.00") + "\n");
        args.DebugLabel.AppendText("Sprite's basis angle is " + entity.Basis.GetRotationQuaternion().GetAngle().ToString("0.00") + "\n");
        GD.Print(entity.Basis.GetRotationQuaternion().GetAngle());
    }
}

public partial class UpdateVisualTransform(Entity entity) : UserSignalArgs
{
    private Entity _entity = entity;
}