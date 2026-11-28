using System.Collections.Generic;
using ENet;
using Game.Shared.Systems.Camera;
using Game.Shared.Systems.Movement;
using Godot;

namespace Game.Temperance.Network.Server;

public partial class ENetServer
{
    private Dictionary<Peer, uint> _lastInputTickFromPeer = [];
    
    public void ProcessUserInput(Peer peer, ref BufferReader bufferReader)
    {
        if (!_entitiesControlledByPeers.Inverse.TryGetValue(peer, out var networkId))
            return;
            
        // Read client tick and store it as the last input tick received from that particular client
        bufferReader.Read(sizeof(uint) * 8, out var clientTick);
        _lastInputTickFromPeer[peer] = clientTick;
        
        // Read user input
        var movementInput = bufferReader.ReadVector2(-1f, 1f, 0.01f);
            
        // Read camera angle
        var cameraAimInput = bufferReader.ReadVector2(-1f, 1f, 0.01f);
            
        if (!_entityManager.EntityNetworkIdDictionary.Inverse.TryGetValue(networkId, out var entity))
            return;

        if (!_componentManager.TryGetComponent<MovementComponent>(entity, out var movementComponent))
            return;
            
        movementComponent.InputDirection = movementInput;

        if (!_componentManager.TryGetComponent<CameraComponent>(entity, out var cameraComponent))
            return;

        cameraComponent.CameraAimInput = cameraAimInput;
    }
}