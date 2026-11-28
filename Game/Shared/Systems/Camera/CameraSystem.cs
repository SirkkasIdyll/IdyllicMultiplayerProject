using Game.Client.Scenes;
using Game.Shared.Systems.Visuals;
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

        _signalBus.RegisterDebugMenuOptions += OnRegisterDebugMenuOptions;
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

        var entity = ENetClient.Instance.ClientControlledEntity;

        if (!_componentManager.TryGetComponent<CameraComponent>(entity, out var cameraComponent))
            return;

        if (_controlledEntityChanged)
        {
            cameraComponent.Camera?.MakeCurrent();
            _controlledEntityChanged = false;
        }
            
        if (!Input.IsActionPressed("aim"))
        {
            _timeHeldAiming = 0;
            cameraComponent.CameraAimInput = Vector2.Zero;
            // ResetCamera((entity, cameraComponent), delta);
            return;
        }
        
        _timeHeldAiming += delta;
        if (_timeHeldAiming < AimingThresholdTime)
            return;
        
        var mouseOffsetFromCenter = GetMouseOffsetFromCenter();
        cameraComponent.CameraAimInput = mouseOffsetFromCenter.Normalized();;
        // AimCamera((entity, cameraComponent), delta);
    }
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        
        // Set a node's rotation to face the same angle that their camera is looking at
        _componentManager.GetEntitiesWithComponent<CameraComponent>(out var cameraEntities);
        foreach (var entity in cameraEntities)
        {
            if (!_componentManager.TryGetComponent<CameraComponent>(entity, out var cameraComponent))
                continue;

            if (cameraComponent.CameraAimInput == Vector2.Zero)
                continue;
            
            entity.Basis = new Basis(Vector3.Up, cameraComponent.CameraAimInput.Angle());
        }
    }

    /// <summary>
    /// Add camera system to debug display options
    /// </summary>
    /// <param name="args"></param>
    private void OnRegisterDebugMenuOptions(ref RegisterDebugMenuOptions args)
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
        args.DebugLabel.AppendText("Player Position: " + entity.GlobalPosition.ToString("0.00") + "\n");
        args.DebugLabel.AppendText("Mouse Offset from Center: " + GetMouseOffsetFromCenter() + "\n");
        args.DebugLabel.AppendText("Viewport Size: " + GetViewport().GetVisibleRect().Size + "\n");
        args.DebugLabel.AppendText("New Camera Position: " + GetNewCameraPosition((entity, cameraComponent), MaxCameraDistance) + "\n");
    }
    
    /// <summary>
    /// Lerps the camera's global position towards the camera's aim input
    /// </summary>
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
    }
    
    /// <summary>
    /// The character is always displayed at the center of the screen by default
    /// So to aim the camera, we just need to figure out where the mouse is from the center of the screen
    /// </summary>
    private Vector2 GetMouseOffsetFromCenter()
    {
        return GetViewport().GetMousePosition() - GetViewport().GetVisibleRect().Size / 2.0f;
    }
    
    /// <summary>
    /// Allows the camera position to drift a set max distance away from the entity it belongs to
    /// based on the camera's aim input
    /// </summary>
    private Vector3? GetNewCameraPosition(Entity<CameraComponent> entity, float maxDistance)
    {
        var visualGlobalPosition = entity.Owner.VisualGlobalTransform.Origin;
        
        if (entity.Comp.Camera is null)
            return null;
        
        // Camera size gives us the diameter of the height, which is our overall scale
        var viewportSize = GetViewport().GetVisibleRect().Size;
        var scale = viewportSize.Y / entity.Comp.Camera.Size;
        
        // Target position gets clamped by how far we actually want the camera to be able to go from the player
        var targetPosition = new Vector3(
            visualGlobalPosition.X + entity.Comp.CameraAimInput.X / scale,
            entity.Comp.Camera.GlobalPosition.Y,
            visualGlobalPosition.Z + entity.Comp.CameraAimInput.Y / scale);

        if (visualGlobalPosition.DistanceTo(targetPosition) > maxDistance)
            return visualGlobalPosition + (targetPosition - visualGlobalPosition).Normalized() * maxDistance;
        
        return targetPosition;
    }
    
    /// <summary>
    /// Slowly lerps/resets the camera's global position back to the entity it belongs to
    /// </summary>
    private void ResetCameraPosition(Entity<CameraComponent> entity, double delta)
    {
        var visualGlobalPosition = entity.Owner.VisualGlobalTransform.Origin;
        
        if (entity.Comp.Camera is null)
            return;
        
        var cameraGlobalPosition = entity.Comp.Camera.GlobalPosition;

        if (visualGlobalPosition.DistanceTo(cameraGlobalPosition) < 0.01)
            return;
        
        var weight = 1f - Mathf.Exp(-CameraResetSpeed * (float)delta);
        entity.Comp.Camera.SetGlobalPosition(cameraGlobalPosition.Lerp(visualGlobalPosition, weight));
    }
}