using Game.Client.Scenes;
using Godot;
using Game.Temperance.NCS;
using Game.Temperance.Network;
using Game.Temperance.Network.Client;
using Game.Temperance.Signals;

namespace Game.Shared.Systems.Camera;

[GlobalClass]
public partial class CameraSystem : NodeSystem
{
    [InjectedDependency] private readonly ComponentManager _componentManager = null!;
    [InjectedDependency] private readonly NodeManager _nodeManager = null!;
    [InjectedDependency] private readonly NodeSystemManager _nodeSystemManager = null!;
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
    private bool _controlledNodeChanged;
    
    public override void _Ready()
    {
        base._Ready();

        _signalBus.FetchDebugMenuOptions += OnFetchDebugMenuOptions;
        _signalBus.UpdateDebugLabel += OnUpdateDebugLabel;
        _signalBus.UpdateClientControlledNodeSignal += OnUpdateClientControlledNodeSignal;
        // _signalBus.UpdateRotationSignal += OnUpdateRotation;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        if (Networking.IsServer())
            return;

        if (ENetClient.Instance.ClientControlledNode == null)
            return;

        if (!_componentManager.TryGetComponent<CameraComponent>(ENetClient.Instance.ClientControlledNode,
                out var cameraComponent))
            return;

        if (_controlledNodeChanged)
        {
            cameraComponent.Camera?.MakeCurrent();
            cameraComponent.Camera?.Size = cameraComponent.CameraSize;
            _controlledNodeChanged = false;
        }
            
        if (!Input.IsActionPressed("aim"))
        {
            _timeHeldAiming = 0;
            ResetCamera((ENetClient.Instance.ClientControlledNode, cameraComponent), delta);
            return;
        }
        
        _timeHeldAiming += delta;
        if (_timeHeldAiming < AimingThresholdTime)
            return;
        
        AimCamera((ENetClient.Instance.ClientControlledNode, cameraComponent), delta);
    }

    private void OnFetchDebugMenuOptions(ref FetchDebugMenuOptions args)
    {
        args.SystemsList?.AddItem(DebugOptionName);
    }

    private void OnUpdateClientControlledNodeSignal(Node node, ref UpdateClientControlledNodeSignal args)
    {
        _controlledNodeChanged = true;
    }

    // private void OnComponentsRegistered(Node node, ref ComponentsRegisteredSignal args)
    // {
    //     if (Networking.IsServer() || ENetClient.Instance.ClientControlledNode == null)
    //         return;
    //
    //     if (node != ENetClient.Instance.ClientControlledNode)
    //         return;
    //
    //     if (!_componentManager.TryGetComponent<CameraComponent>(node, out var cameraComponent))
    //         return;
    //     
    //     GD.Print("Setting camera and camera size");
    //     cameraComponent.Camera?.MakeCurrent();
    //     cameraComponent.Camera?.Size = 20f;
    //     
    //     if (!_componentManager.TryGetComponent<PersonalVisionComponent>(node,
    //             out var personalVisionComponent))
    //         return;
    //     
    //     personalVisionComponent.VisionCone?.SpotAngle = personalVisionComponent.VisionConeAngle;
    //     personalVisionComponent.VisionCone?.SpotAttenuation = personalVisionComponent.VisionConeAttenuation;
    //     personalVisionComponent.VisionCone?.SpotRange = personalVisionComponent.VisionConeRange;
    //     personalVisionComponent.VisionCone?.LightEnergy = personalVisionComponent.VisionConeEnergy;
    //     personalVisionComponent.VisionCone?.LightColor = personalVisionComponent.VisionConeColor;
    //     personalVisionComponent.VisionCone?.SetVisible(true);
    //     
    //     personalVisionComponent.VisionCircle?.OmniAttenuation = personalVisionComponent.VisionCircleAttenuation;
    //     personalVisionComponent.VisionCircle?.LightColor = personalVisionComponent.VisionCircleColor;
    //     personalVisionComponent.VisionCircle?.LightEnergy = personalVisionComponent.VisionCircleEnergy;
    //     personalVisionComponent.VisionCircle?.OmniRange = personalVisionComponent.VisionCircleRange;
    //     personalVisionComponent.VisionCircle?.SetVisible(true);
    // }

    // private void OnNodeSpawned(Guid netGuid, ref NodeSpawnedSignal args)
    // {
    //     if (Networking.IsServer())
    //         return;
    //     
    //     if (ENetClient.Instance.EnetGuid != netGuid)
    //         return;
    //     
    //     if (!_nodeManager.NetGuidDictionary.TryGetValue(netGuid, out var nodeUpdateInfo))
    //         return;
    //
    //     if (!_componentManager.TryGetComponent<CameraComponent>(nodeUpdateInfo.Node, out var cameraComponent))
    //         return;
    //     
    //     cameraComponent.Camera?.MakeCurrent();
    //
    //     if (!_componentManager.TryGetComponent<PersonalVisionComponent>(nodeUpdateInfo.Node,
    //             out var personalVisionComponent))
    //         return;
    //
    //     personalVisionComponent.VisionCone?.SpotAngle = personalVisionComponent.VisionConeAngle;
    //     personalVisionComponent.VisionCone?.SpotAttenuation = personalVisionComponent.VisionConeAttenuation;
    //     personalVisionComponent.VisionCone?.SpotRange = personalVisionComponent.VisionConeRange;
    //     personalVisionComponent.VisionCone?.LightEnergy = personalVisionComponent.VisionConeEnergy;
    //     personalVisionComponent.VisionCone?.LightColor = personalVisionComponent.VisionConeColor;
    //     personalVisionComponent.VisionCone?.SetVisible(true);
    //     
    //     personalVisionComponent.VisionCircle?.OmniAttenuation = personalVisionComponent.VisionCircleAttenuation;
    //     personalVisionComponent.VisionCircle?.LightColor = personalVisionComponent.VisionCircleColor;
    //     personalVisionComponent.VisionCircle?.LightEnergy = personalVisionComponent.VisionCircleEnergy;
    //     personalVisionComponent.VisionCircle?.OmniRange = personalVisionComponent.VisionCircleRange;
    //     personalVisionComponent.VisionCircle?.SetVisible(true);
    // }
    
    private void OnUpdateDebugLabel(string selectedSystem, ref UpdateDebugLabel args)
    {
        if (args.DebugLabel is null)
            return;

        if (selectedSystem != DebugOptionName)
            return;
        
        if (Networking.IsServer())
            return;
        
        // if (!_nodeManager.NetGuidDictionary.TryGetValue(ENetClient.Instance.EnetGuid, out var nodeUpdateInfo))
        //     return;
        //
        // var node = nodeUpdateInfo.Node;
        //
        // if (!_componentManager.TryGetComponent<CameraComponent>(node, out var cameraComponent))
        //     return;
        //
        // args.DebugLabel.Clear();
        // args.DebugLabel.AppendText("Player Position: " + node.GlobalPosition.ToString() + "\n");
        // args.DebugLabel.AppendText("Mouse Offset from Center: " + GetMouseOffsetFromCenter() + "\n");
        // args.DebugLabel.AppendText("Viewport Size: " + GetViewport().GetVisibleRect().Size + "\n");
        // args.DebugLabel.AppendText("New Camera Position: " + GetNewCameraPosition((node, cameraComponent), MaxCameraDistance) + "\n");
    }

    // private void OnUpdateRotation(Node<RotationComponent> node, ref UpdateRotationSignal args)
    // {
    //     if (!_componentManager.TryGetComponent<PersonalVisionComponent>(node,
    //             out var personalVisionComponent))
    //         return;
    //
    //     // Vision cone faces -Z by default instead of X, so we need to rotate it by -90 degrees after we set it
    //     personalVisionComponent.VisionCone?.Basis = node.Comp.Basis.Inverse().Rotated(Vector3.Up, -Mathf.Pi / 2f);
    // }

    private void ResetCamera(Node<CameraComponent> node, double delta)
    {
        if (node.Comp.Camera is null)
            return;

        if (node.Comp.GlobalPosition.DistanceTo(node.Comp.Camera.GlobalPosition) < 0.01)
            return;
        
        var weight = 1f - Mathf.Exp(-CameraResetSpeed * (float)delta);
        node.Comp.Camera.SetGlobalPosition(node.Comp.Camera.GlobalPosition.Lerp(node.Comp.GlobalPosition, weight));
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
        
        node.Comp.Camera.SetGlobalPosition(node.Comp.Camera.GlobalPosition.Lerp(newCameraPosition.Value, weight));
        
        var mouseOffsetFromCenter = GetMouseOffsetFromCenter();

        if (mouseOffsetFromCenter is null)
            return;
        
        // We rotate the node around the Y axis since we're a top-down game
        node.Comp.AngleOfCameraPositionFromDefaultPosition = mouseOffsetFromCenter.Value.Normalized().Angle();
        // node3D.Basis = new Basis(Vector3.Up, mouseOffsetFromCenter.Value.Normalized().Angle());
    }

    private Vector2? GetMouseOffsetFromCenter()
    {
        return GetViewport().GetMousePosition() - GetViewport().GetVisibleRect().Size / 2.0f;
    }

    private Vector3? GetNewCameraPosition(Node<CameraComponent> node, float maxDistance)
    {
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
            node.Comp.GlobalPosition.X + mouseOffsetFromCenter.Value.X / scale,
            node.Comp.GlobalPosition.Y,
            node.Comp.GlobalPosition.Z + mouseOffsetFromCenter.Value.Y / scale);

        if (node.Comp.GlobalPosition.DistanceTo(targetPosition) > maxDistance)
            return node.Comp.GlobalPosition + (targetPosition - node.Comp.GlobalPosition).Normalized() * maxDistance;
        
        return targetPosition;
    }
}