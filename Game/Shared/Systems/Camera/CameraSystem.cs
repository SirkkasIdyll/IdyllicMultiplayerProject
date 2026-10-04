using System;
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
    
    public override void _Ready()
    {
        base._Ready();

        _signalBus.NodeSpawnedSignal += OnNodeSpawned;
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
}