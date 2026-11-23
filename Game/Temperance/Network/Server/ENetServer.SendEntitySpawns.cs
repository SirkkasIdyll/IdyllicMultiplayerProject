using System;
using System.Collections.Generic;
using System.Text;
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

        var entities = _newEntitySpawnsToBroadcast;
        var buffer = new byte[10000];
        var bufferWriter = new BufferWriter(buffer);

        // Write packet type
        bufferWriter.WriteInt((byte)ReliableServerPacketType.EntitySpawns, 0, Networking.MaxPacketTypesPerChannel);
        
        // Write server tick
        bufferWriter.StoreOrWrite(_serverTick, sizeof(uint) * 8);
        
        // Write entities in packet
        bufferWriter.StoreOrWrite((uint)entities.Count, 10);

        foreach (var entity in entities)
        {
            if (!_entityManager.EntityNetworkIdDictionary.TryGetValue(entity, out var networkId))
                continue;
            
            // Write length of entity name
            // (I'm just gonna assume we're not gonna need more than 32 bytes to write a simple name man)
            bufferWriter.WriteInt(Encoding.UTF8.GetByteCount(_entityManager.GetNodeName(entity)), 0, 32);
                
            // Write entity name for spawning, shorter than the full file path
            bufferWriter.StoreOrWrite(Encoding.UTF8.GetBytes(_entityManager.GetNodeName(entity)));
            
            // Write entity network ID
            bufferWriter.StoreOrWrite(networkId, sizeof(ushort) * 8);
            
            // Write entity position
            bufferWriter.WriteVector3(entity.GlobalPosition, -5000, 5000, 0.01f);

            // Write entity rotation
            bufferWriter.WriteQuaternion(entity.Quaternion);
            
            for (ushort i = 0; i < _componentManager.ComponentIdToComponentNameDictionary.Count; i++)
                bufferWriter.WriteBool(_componentManager.HasComponent(entity, i));
            
            // Write out the details for each component in order
            var components = entity.Components;
            for (ushort i = 0; i < _componentManager.ComponentIdToComponentNameDictionary.Count; i++)
            {
                var componentName = _componentManager.ComponentIdToComponentNameDictionary[i];

                if (!components.TryGetValue(componentName, out var component))
                    continue;
                
                component.WriteReliableChannel(ref bufferWriter);
            }
        }
        
        bufferWriter.FinalWrite(out var byteIndex);
        Array.Resize(ref buffer, byteIndex);
        Broadcast(ENetChannels.ReliableServer, buffer, PacketFlags.Reliable);
        _newEntitySpawnsToBroadcast.Clear();
    }
}