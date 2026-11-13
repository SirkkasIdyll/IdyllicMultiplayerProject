using System;
using System.Collections.Generic;
using ENet;

namespace Game.Temperance.Network.Server;

public partial class ENetServer
{
    private readonly List<Peer> _newPeersThatNeedSynchronization = [];

    /// <summary>
    /// When a peer connects to the server,
    /// queue them up as part of the peers that need to have
    /// all the nodes on the server communicated to them
    /// </summary>
    private void OnPeerConnected(Event netEvent, ref PeerConnectedSignal args)
    {
        _newPeersThatNeedSynchronization.Add(netEvent.Peer);
    }
    
    /// <summary>
    /// Send newly connected clients all the nodes that are spawned on the server
    /// </summary>
    private void BroadcastLateJoinSynchronization()
    {
        if (_newPeersThatNeedSynchronization.Count == 0)
            return;
        
        var buffer = new byte[10000];
        var bufferWriter = new BufferWriter(buffer);
        
        // Write packet type
        bufferWriter.WriteInt((byte)ReliableServerPacketType.LateJoinSynchronization, 0, Networking.MaxPacketTypesPerChannel);

        // Write server tick
        bufferWriter.StoreOrWrite(_serverTick, sizeof(uint) * 8);
        
        var byteIndex = SendNodeComponentUpdate(ref bufferWriter, true, [.. _nodeManager.NodeNetworkIdDictionary.Keys]);
        Array.Resize(ref buffer, byteIndex);
        Broadcast(ENetChannels.ReliableServer, buffer, [.. _newPeersThatNeedSynchronization], PacketFlags.Reliable);
        _newPeersThatNeedSynchronization.Clear();
    }
}