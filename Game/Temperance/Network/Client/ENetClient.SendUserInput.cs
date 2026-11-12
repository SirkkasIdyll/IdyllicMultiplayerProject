using System;
using Game.Shared.Systems.Movement;

namespace Game.Temperance.Network.Client;

public partial class ENetClient
{
    private void BroadcastUserInput()
    {
        if (ClientControlledNode == null)
            return;

        if (!_componentManager.TryGetComponent<MovementComponent>(ClientControlledNode, out var movementComponent))
            return;
        
        var buffer = new byte[10000];
        var bufferWriter = new BufferWriter(buffer);
        
        // Write packet type
        bufferWriter.WriteInt((byte)ReliableServerPacketType.LateJoinSynchronization, 0, Networking.MaxPacketTypesPerChannel);

        // Write client tick
        bufferWriter.StoreOrWrite(_clientTick, sizeof(uint) * 8);

        // Write input vector
        bufferWriter.WriteVector2(movementComponent.InputDirection, -1f, 1f, 0.01f);
        
        bufferWriter.FinalWrite(out var byteIndex);
        Array.Resize(ref buffer, byteIndex);
        Send(ENetChannels.UnreliableClient, buffer);
    }
}