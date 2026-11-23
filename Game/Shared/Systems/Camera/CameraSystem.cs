using Game.Client.Scenes;
using Godot;
using Game.Temperance.EntityComponentSystem;
using Game.Temperance.Network;
using Game.Temperance.Network.Client;
using Game.Temperance.Signals;

namespace Game.Shared.Systems.Camera;

[GlobalClass]
public partial class CameraSystem : EntitySystem
{
    [InjectedDependency] private readonly ComponentManager _componentManager = null!;
    [InjectedDependency] private readonly EntityManager _entityManager = null!;
    [InjectedDependency] private readonly EntitySystemManager _entitySystemManager = null!;
    [InjectedDependency] private readonly SignalBus _signalBus = null!;

    private const string DebugOptionName = "Camera Aiming";
    private const float CameraAimSpeed = 3f;
    private const float CameraResetSpeed = 9f;
    private const float MaxCameraDistance = 3f;
    private const double AimingThresholdTime = 0.3;
    private double _timeHeldAiming = 0;
    
    /// <summary>
    /// Flag used for changing camera to camera of controlled node
    /// </summary>
    private bool _controlledEntityChanged;
    
    public override void _Ready()
    {
        base._Ready();

        _signalBus.FetchDebugMenuOptions += OnFetchDebugMenuOptions;
        _signalBus.UpdateDebugLabel += OnUpdateDebugLabel;
        _signalBus.UpdateClientControlledEntitySignal += OnUpdateClientControlledEntitySignal;
        // _signalBus.UpdateRotationSignal += OnUpdateRotation;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        if (Networking.IsServer())
            return;

        if (ENetClient.Instance.ClientControlledEntity == null)
            return;

        if (!_componentManager.TryGetComponent<CameraComponent>(ENetClient.Instance.ClientControlledEntity,
                out var cameraComponent))
            return;

        if (_controlledEntityChanged)
        {
            cameraComponent.Camera?.MakeCurrent();
            _controlledEntityChanged = false;
        }
            
        if (!Input.IsActionPressed("aim"))
        {
            _timeHeldAiming = 0;
            ResetCamera((ENetClient.Instance.ClientControlledEntity, cameraComponent), delta);
            return;
        }
        
        _timeHeldAiming += delta;
        if (_timeHeldAiming < AimingThresholdTime)
            return;
        
        AimCamera((ENetClient.Instance.ClientControlledEntity, cameraComponent), delta);
    }
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (Networking.IsServer())
            return;
        
        // Set a node's rotation to face the same angle that their camera is looking at
        _componentManager.GetEntitiesWithComponent<CameraComponent>(out var cameraEntities);
        foreach (var entity in cameraEntities)
        {
            if (!_componentManager.TryGetComponent<CameraComponent>(entity, out var cameraComponent))
                continue;
            
            entity.Basis = new Basis(Vector3.Up, cameraComponent.AngleOfCameraPositionFromDefaultPosition);
        }
    }

    /// <summary>
    /// Add camera system to debug display options
    /// </summary>
    /// <param name="args"></param>
    private void OnFetchDebugMenuOptions(ref FetchDebugMenuOptions args)
    {
        args.SystemsList?.AddItem(DebugOptionName);
    }

    private void OnUpdateClientControlledEntitySignal(Entity entity, ref UpdateClientControlledEntitySignal args)
    {
        _controlledEntityChanged = true;
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
        
        if (!_componentManager.TryGetComponent<CameraComponent>(entity, out var cameraComponent))
            return;
        
        args.DebugLabel.Clear();
        args.DebugLabel.AppendText("Player Position: " + entity.GlobalPosition + "\n");
        args.DebugLabel.AppendText("Mouse Offset from Center: " + GetMouseOffsetFromCenter() + "\n");
        args.DebugLabel.AppendText("Viewport Size: " + GetViewport().GetVisibleRect().Size + "\n");
        args.DebugLabel.AppendText("New Camera Position: " + GetNewCameraPosition((entity, cameraComponent), MaxCameraDistance) + "\n");
    }

    // private void OnUpdateRotation(Entity<RotationComponent> entity, ref UpdateRotationSignal args)
    // {
    //     if (!_componentManager.TryGetComponent<PersonalVisionComponent>(entity,
    //             out var personalVisionComponent))
    //         return;
    //
    //     // Vision cone faces -Z by default instead of X, so we need to rotate it by -90 degrees after we set it
    //     personalVisionComponent.VisionCone?.Basis = entity.Comp.Basis.Inverse().Rotated(Vector3.Up, -Mathf.Pi / 2f);
    // }

    private void ResetCamera(Entity<CameraComponent> entity, double delta)
    {
        var visualGlobalPosition = entity.Owner.VisualGlobalTransform3D.Origin;
        
        if (entity.Comp.Camera is null)
            return;
        
        var cameraGlobalPosition = entity.Comp.Camera.GlobalPosition;

        if (visualGlobalPosition.DistanceTo(cameraGlobalPosition) < 0.01)
            return;
        
        var weight = 1f - Mathf.Exp(-CameraResetSpeed * (float)delta);
        entity.Comp.Camera.SetGlobalPosition(cameraGlobalPosition.Lerp(visualGlobalPosition, weight));
    }

    private void AimCamera(Entity<CameraComponent> entity, double delta)
    {
        if (entity.Comp?.Camera == null)
            return;
        
        var cameraGlobalPosition = entity.Comp.Camera.GlobalPosition;
        var newCameraPosition = GetNewCameraPosition(entity, MaxCameraDistance);
        var weight = 1f - Mathf.Exp(-CameraAimSpeed * (float)delta);
        if (newCameraPosition == null)
            return;

        entity.Comp.Camera.SetGlobalPosition(cameraGlobalPosition.Lerp(newCameraPosition.Value, weight));

        var mouseOffsetFromCenter = GetMouseOffsetFromCenter();

        if (mouseOffsetFromCenter is null)
            return;
        
        // We rotate the entity around the Y axis since we're a top-down game
        entity.Comp.AngleOfCameraPositionFromDefaultPosition = mouseOffsetFromCenter.Value.Normalized().Angle();
        // entity.Basis = new Basis(Vector3.Up, mouseOffsetFromCenter.Value.Normalized().Angle());
    }

    private Vector2? GetMouseOffsetFromCenter()
    {
        return GetViewport().GetMousePosition() - GetViewport().GetVisibleRect().Size / 2.0f;
    }

    private Vector3? GetNewCameraPosition(Entity<CameraComponent> entity, float maxDistance)
    {
        var visualGlobalPosition = entity.Owner.VisualGlobalTransform3D.Origin;
        
        if (entity.Comp.Camera is null)
            return null;
        
        var mouseOffsetFromCenter = GetMouseOffsetFromCenter();
        
        if (mouseOffsetFromCenter is null)
            return null;
        
        // Camera size gives us the diameter of the height, which is our overall scale
        var viewportSize = GetViewport().GetVisibleRect().Size;
        var scale = viewportSize.Y / entity.Comp.Camera.Size;
        
        // Target position clamped by how far we actually want the camera to be able to go from the player
        var targetPosition = new Vector3(
            visualGlobalPosition.X + mouseOffsetFromCenter.Value.X / scale,
            visualGlobalPosition.Y,
            visualGlobalPosition.Z + mouseOffsetFromCenter.Value.Y / scale);

        if (visualGlobalPosition.DistanceTo(targetPosition) > maxDistance)
            return visualGlobalPosition + (targetPosition - visualGlobalPosition).Normalized() * maxDistance;
        
        return targetPosition;
    }
}