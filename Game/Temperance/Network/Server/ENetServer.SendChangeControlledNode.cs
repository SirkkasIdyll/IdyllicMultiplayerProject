using System;
using System.Collections.Generic;
using ENet;

namespace Game.Temperance.Network.Server;

public partial class ENetServer
{
    private readonly Dictionary<ushort, Peer> _changedControlledNodes = [];
    
    private void BroadcastChangeControlledNode()
    {
        foreach (var (networkId, peer) in _changedControlledNodes)
        {
            var buffer = new byte[10000];
            var bufferWriter = new BufferWriter(buffer);
            
            // Write packet type
            bufferWriter.WriteInt((byte)ReliableServerPacketType.ChangeControlledNode, 0, Networking.MaxPacketTypesPerChannel);

            // Write server tick
            bufferWriter.StoreOrWrite(_serverTick, sizeof(uint) * 8);
            
            // Write node network ID
            bufferWriter.StoreOrWrite(networkId, sizeof(ushort) * 8);
            
            bufferWriter.FinalWrite(out var byteIndex);
            Array.Resize(ref buffer, byteIndex);
            Broadcast(ENetChannels.ReliableServer, buffer, [peer], PacketFlags.Reliable);
        }
        
        _changedControlledNodes.Clear();
    }
}