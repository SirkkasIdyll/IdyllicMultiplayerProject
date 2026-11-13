using System;

namespace Game.Temperance.Network.Server;

public partial class ENetServer
{
    private void BroadcastStateUpdates()
    {
        if (_nodeManager.NodeNetworkIdDictionary.Count == 0)
            return;
        
        var buffer = new byte[10000];
        var bufferWriter = new BufferWriter(buffer);
        
        // Write packet type
        bufferWriter.WriteInt((byte)UnreliableServerPacketType.StateUpdates, 0, Networking.MaxPacketTypesPerChannel);
        
        // Write server tick
        bufferWriter.StoreOrWrite(_serverTick, sizeof(uint) * 8);
        
        var byteIndex = SendNodeComponentUpdate(ref bufferWriter, false, [.. _nodeManager.NodeNetworkIdDictionary.Keys]);
        Array.Resize(ref buffer, byteIndex);
        Broadcast(ENetChannels.UnreliableServer, buffer);
    }
}