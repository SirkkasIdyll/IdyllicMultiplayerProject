using System;
using System.Collections.Generic;
using ENet;
using Game.Temperance.EntityComponentSystem;

namespace Game.Temperance.Network.Server;

// In the interest of hiding all the non-network related stuff from the main class
// Here's a file with all the packet formations
public partial class ENetServer
{
    private readonly List<Entity> _newEntitySpawnsToBroadcast = [];
    
    /// <summary>
    /// When a entity gets spawned on the server, queue it up to be sent on the next 
    /// </summary>
    private void OnEntitySpawned(Entity entity, ref EntitySpawnedSignal args)
    {
        _newEntitySpawnsToBroadcast.Add(entity);
    }
    
    /// <summary>
    /// Send connected clients the entities that just spawned
    /// </summary>
    private void BroadcastEntitySpawns()
    {
        if (_newEntitySpawnsToBroadcast.Count == 0)
            return;
        
        var buffer = new byte[10000];
        var bufferWriter = new BufferWriter(buffer);

        // Write packet type
        bufferWriter.WriteInt((byte)ReliableServerPacketType.EntitySpawns, 0, Networking.MaxPacketTypesPerChannel);
        
        // Write server tick
        bufferWriter.StoreOrWrite(_serverTick, sizeof(uint) * 8);
        
        var byteIndex = SendEntityComponentUpdate(ref bufferWriter, true, [.. _newEntitySpawnsToBroadcast]);
        Array.Resize(ref buffer, byteIndex);
        Broadcast(ENetChannels.ReliableServer, buffer, PacketFlags.Reliable);
        _newEntitySpawnsToBroadcast.Clear();
    }
}