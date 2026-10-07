using System;
using Game.Client.Scenes;
using Godot;
using Game.Temperance.NCS;
using Game.Temperance.Network;
using Game.Temperance.Signals;

namespace Game.Shared.Systems.Camera;

[GlobalClass]
public partial class CameraSystem : NodeSystem
{
    [InjectedDependency] private readonly ComponentManager _componentManager = null!;
    [InjectedDependency] private readonly NodeManager _nodeManager = null!;
    // [InjectedDependency] private readonly NodeSystemManager _nodeSystemManager = null!;
    [InjectedDependency] private readonly SignalBus _signalBus = null!;

    private const string DebugOptionName = "Camera Aiming";
    private const float CameraAimSpeed = 3f;
    private const float CameraResetSpeed = 9f;
    private const float MaxCameraDistance = 3f;
    private const double AimingThresholdTime = 0.3;
    private double _timeHeldAiming = 0;
    
    public override void _Ready()
    {
        base._Ready();

        _signalBus.FetchDebugMenuOptions += OnFetchDebugMenuOptions;
        _signalBus.UpdateDebugLabel += OnUpdateDebugLabel;
        _signalBus.NodeSpawnedSignal += OnNodeSpawned;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        if (Networking.IsServer())
            return;
        
        if (!_nodeManager.NetGuidDictionary.TryGetValue(ENetClient.Instance.EnetGuid, out var nodeUpdateInfo))
            return;

        var node = nodeUpdateInfo.Node;

        if (!_componentManager.TryGetComponent<CameraComponent>(node, out var cameraComponent))
            return;

        if (!Input.IsActionPressed("aim"))
        {
            _timeHeldAiming = 0;
            ResetCamera((node, cameraComponent), delta);
            return;
        }

        _timeHeldAiming += delta;
        if (_timeHeldAiming < AimingThresholdTime)
            return;
        
        AimCamera((node, cameraComponent), delta);
    }

    private void OnFetchDebugMenuOptions(ref FetchDebugMenuOptions args)
    {
        args.SystemsList?.AddItem(DebugOptionName);
    }

    private void OnNodeSpawned(Guid netGuid, ref NodeSpawnedSignal args)
    {
        if (Networking.IsServer())
            return;
        
        if (ENetClient.Instance.EnetGuid != netGuid)
            return;
        
        if (!_nodeManager.NetGuidDictionary.TryGetValue(netGuid, out var nodeUpdateInfo))
            return;

        if (!_componentManager.TryGetComponent<CameraComponent>(nodeUpdateInfo.Node, out var cameraComponent))
            return;
        
        cameraComponent.Camera?.MakeCurrent();

        if (!_componentManager.TryGetComponent<PersonalVisionComponent>(nodeUpdateInfo.Node,
                out var personalVisionComponent))
            return;

        personalVisionComponent.VisionCone?.SpotAngle = personalVisionComponent.VisionConeAngle;
        personalVisionComponent.VisionCone?.SpotAttenuation = personalVisionComponent.VisionConeAttenuation;
        personalVisionComponent.VisionCone?.SpotRange = personalVisionComponent.VisionConeRange;
        personalVisionComponent.VisionCone?.LightEnergy = personalVisionComponent.VisionConeEnergy;
        personalVisionComponent.VisionCone?.LightColor = personalVisionComponent.VisionConeColor;
        personalVisionComponent.VisionCone?.SetVisible(true);
        
        personalVisionComponent.VisionCircle?.OmniAttenuation = personalVisionComponent.VisionCircleAttenuation;
        personalVisionComponent.VisionCircle?.LightColor = personalVisionComponent.VisionCircleColor;
        personalVisionComponent.VisionCircle?.LightEnergy = personalVisionComponent.VisionCircleEnergy;
        personalVisionComponent.VisionCircle?.OmniRange = personalVisionComponent.VisionCircleRange;
        personalVisionComponent.VisionCircle?.SetVisible(true);
    }
    
    private void OnUpdateDebugLabel(string selectedSystem, ref UpdateDebugLabel args)
    {
        if (args.DebugLabel is null)
            return;

        if (selectedSystem != DebugOptionName)
            return;
        
        if (Networking.IsServer())
            return;
        
        if (!_nodeManager.NetGuidDictionary.TryGetValue(ENetClient.Instance.EnetGuid, out var nodeUpdateInfo))
            return;

        var node = nodeUpdateInfo.Node;

        if (!_componentManager.TryGetComponent<CameraComponent>(node, out var cameraComponent))
            return;

        args.DebugLabel.Clear();
        args.DebugLabel.AppendText("Player Position: " + node.GlobalPosition.ToString() + "\n");
        args.DebugLabel.AppendText("Camera Offset: " + GetCameraOffset((node, cameraComponent)) + "\n");
        args.DebugLabel.AppendText("Mouse Offset from Center: " + GetMouseOffsetFromCenter() + "\n");
        args.DebugLabel.AppendText("Viewport Size: " + GetViewport().GetVisibleRect().Size + "\n");
        args.DebugLabel.AppendText("New Camera Position: " + GetNewCameraPosition((node, cameraComponent), MaxCameraDistance) + "\n");
    }

    private void ResetCamera(Node<CameraComponent> node, double delta)
    {
        if (node.Owner is not Node3D node3D)
            return;

        if (node.Comp.Camera is null)
            return;

        if (node3D.GlobalPosition.DistanceTo(node.Comp.GlobalPosition) < 0.01)
            return;
        
        var weight = 1f - Mathf.Exp(-CameraResetSpeed * (float)delta);
        node.Comp.SetGlobalPosition(node.Comp.GlobalPosition.Lerp(node3D.GlobalPosition, weight));
    }

    private void AimCamera(Node<CameraComponent> node, double delta)
    {
        if (node.Owner is not Node3D node3D)
            return;

        if (node.Comp.Camera is null)
            return;
        
        var newCameraPosition = GetNewCameraPosition(node, MaxCameraDistance);
        var weight = 1f - Mathf.Exp(-CameraAimSpeed * (float)delta);
        if (newCameraPosition == null)
            return;
        
        node.Comp.SetGlobalPosition(node.Comp.GlobalPosition.Lerp(newCameraPosition.Value, weight));
    }

    private Vector2? GetCameraOffset(Node<CameraComponent> node)
    {
        if (node.Owner is not Node3D node3D)
            return null;

        if (node.Comp.Camera is null)
            return null;
        
        return new Vector2(node.Comp.GlobalPosition.Z, node.Comp.GlobalPosition.X) - new Vector2(node3D.GlobalPosition.Z, node3D.GlobalPosition.X);
    }

    private Vector2? GetMouseOffsetFromCenter()
    {
        return GetViewport().GetMousePosition() - GetViewport().GetVisibleRect().Size / 2.0f;
    }

    private Vector3? GetNewCameraPosition(Node<CameraComponent> node, float maxDistance)
    {
        if (node.Owner is not Node3D node3D)
            return null;

        if (node.Comp.Camera is null)
            return null;
        
        var mouseOffsetFromCenter = GetMouseOffsetFromCenter();
        
        if (mouseOffsetFromCenter is null)
            return null;
        
        // Camera size gives us the diameter of the height, which is our overall scale
        var viewportSize = GetViewport().GetVisibleRect().Size;
        var scale = viewportSize.Y / node.Comp.Camera.Size;
        
        // Target position clamped by how far we actually want the camera to be able to go from the player
        var targetPosition = new Vector3(
            node3D.GlobalPosition.X + mouseOffsetFromCenter.Value.X / scale,
            node3D.GlobalPosition.Y,
            node3D.GlobalPosition.Z + mouseOffsetFromCenter.Value.Y / scale);

        if (node3D.GlobalPosition.DistanceTo(targetPosition) > maxDistance)
            return node3D.GlobalPosition + (targetPosition - node3D.GlobalPosition).Normalized() * maxDistance;
        
        return targetPosition;
    }
}