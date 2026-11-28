using System;
using System.Collections.Generic;
using Game.Shared.Systems.Camera;
using Game.Shared.Systems.Movement;
using Godot;

namespace Game.Temperance.Network.Client;

public partial class ENetClient
{
    public Dictionary<uint, Vector2> MovementInputHistory = [];
    public Dictionary<uint, Vector2> CameraAimInputHistory = [];
    
    private void BroadcastUserInput()
    {
        if (ClientControlledEntity == null)
            return;

        if (!_componentManager.TryGetComponent<MovementComponent>(ClientControlledEntity, out var movementComponent))
            return;

        if (!_componentManager.TryGetComponent<CameraComponent>(ClientControlledEntity, out var cameraComponent))
            return;
        
        var buffer = new byte[10000];
        var bufferWriter = new BufferWriter(buffer);
        
        // Write packet type
        bufferWriter.WriteInt((byte)ReliableServerPacketType.LateJoinSynchronization, 0, Networking.MaxPacketTypesPerChannel);

        // Write client tick
        bufferWriter.StoreOrWrite(_clientTick, sizeof(uint) * 8);

        // Write input vector
        bufferWriter.WriteVector2(movementComponent.InputDirection, -1f, 1f, 0.01f);
        MovementInputHistory[_clientTick] = movementComponent.InputDirection;
        
        // Write camera angle
        bufferWriter.WriteVector2(cameraComponent.CameraAimInput, -1f, 1f, 0.01f);
        CameraAimInputHistory[_clientTick] = cameraComponent.CameraAimInput;
        
        bufferWriter.FinalWrite(out var byteIndex);
        Array.Resize(ref buffer, byteIndex);
        Send(ENetChannels.UnreliableClient, buffer);
    }
}