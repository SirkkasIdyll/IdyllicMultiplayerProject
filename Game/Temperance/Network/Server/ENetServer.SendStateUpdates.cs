using System;

namespace Game.Temperance.Network.Server;

public partial class ENetServer
{
    private void BroadcastStateUpdates()
    {
        if (_entityManager.EntityNetworkIdDictionary.Count == 0)
            return;
        
        var buffer = new byte[10000];
        var bufferWriter = new BufferWriter(buffer);
        
        // Write packet type
        bufferWriter.WriteInt((byte)UnreliableServerPacketType.StateUpdates, 0, Networking.MaxPacketTypesPerChannel);
        
        // Write server tick
        bufferWriter.StoreOrWrite(_serverTick, sizeof(uint) * 8);
        
        var byteIndex = SendEntityComponentUpdate(ref bufferWriter, false, [.. _entityManager.EntityNetworkIdDictionary.Keys]);
        Array.Resize(ref buffer, byteIndex);
        Broadcast(ENetChannels.UnreliableServer, buffer);
    }
}