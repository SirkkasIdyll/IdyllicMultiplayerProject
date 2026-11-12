using System;
using System.Collections.Generic;
using System.Text;
using ENet;
using Game.Temperance.NCS;
using Godot;

namespace Game.Temperance.Network.Server;

// In the interest of hiding all the non-network related stuff from the main class
// Here's a file with all the packet formations
public partial class ENetServer
{
    private readonly List<Node> _newNodeSpawnsToBroadcast = [];
    
    /// <summary>
    /// When a node gets spawned on the server, queue it up to be sent on the next 
    /// </summary>
    private void OnNodeSpawned(Node node, ref NodeSpawnedSignal args)
    {
        _newNodeSpawnsToBroadcast.Add(node);
    }
    
    /// <summary>
    /// Send connected clients the nodes that just spawned
    /// </summary>
    private void BroadcastNodeSpawns()
    {
        if (_newNodeSpawnsToBroadcast.Count == 0)
            return;
        
        var buffer = new byte[10000];
        var bufferWriter = new BufferWriter(buffer);

        // Write packet type
        bufferWriter.WriteInt((byte)ReliableServerPacketType.NodeSpawns, 0, Networking.MaxPacketTypesPerChannel);
        
        // Write server tick
        bufferWriter.StoreOrWrite(_serverTick, sizeof(uint) * 8);
        
        var byteIndex = SendNodeComponentUpdate(ref bufferWriter, true, [.. _newNodeSpawnsToBroadcast]);
        Array.Resize(ref buffer, byteIndex);
        Broadcast(ENetChannels.ReliableServer, buffer, PacketFlags.Reliable);
        _newNodeSpawnsToBroadcast.Clear();
    }
}