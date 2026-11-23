using System;
using System.Collections.Generic;
using System.Text;
using ENet;

namespace Game.Temperance.Network.Server;

public partial class ENetServer
{
    private readonly List<Peer> _newPeersThatNeedSynchronization = [];

    /// <summary>
    /// When a peer connects to the server,
    /// queue them up as part of the peers that need to have
    /// all the entities on the server communicated to them
    /// </summary>
    private void OnPeerConnected(Event netEvent, ref PeerConnectedSignal args)
    {
        _newPeersThatNeedSynchronization.Add(netEvent.Peer);
    }
    
    /// <summary>
    /// Send newly connected clients all the entities that are spawned on the server
    /// </summary>
    private void BroadcastLateJoinSynchronization()
    {
        if (_newPeersThatNeedSynchronization.Count == 0)
            return;

        var entities = _entityManager.EntityNetworkIdDictionary.Keys;
        var buffer = new byte[10000];
        var bufferWriter = new BufferWriter(buffer);
        
        // Write packet type
        bufferWriter.WriteInt((byte)ReliableServerPacketType.LateJoinSynchronization, 0, Networking.MaxPacketTypesPerChannel);

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
        Broadcast(ENetChannels.ReliableServer, buffer, [.. _newPeersThatNeedSynchronization], PacketFlags.Reliable);
        _newPeersThatNeedSynchronization.Clear();
    }
}