using System;

namespace Game.Temperance.Network.Server;

public partial class ENetServer
{
    private void BroadcastStateUpdates()
    {
        if (_entityManager.EntityNetworkIdDictionary.Count == 0)
            return;

        var entities = _entityManager.EntityNetworkIdDictionary.Keys;
        var buffer = new byte[10000];
        var bufferWriter = new BufferWriter(buffer);
        
        // Write packet type
        bufferWriter.WriteInt((byte)UnreliableServerPacketType.StateUpdates, 0, Networking.MaxPacketTypesPerChannel);
        
        // Write server tick
        bufferWriter.StoreOrWrite(_serverTick, sizeof(uint) * 8);
        
        // Write count of entities
        bufferWriter.StoreOrWrite((uint)entities.Count, 10);
        
        foreach (var entity in entities)
        {
            if (!_entityManager.EntityNetworkIdDictionary.TryGetValue(entity, out var networkId))
                continue;
            
            // Write entity network ID
            bufferWriter.StoreOrWrite(networkId, sizeof(ushort) * 8);
            
            // Write entity position
            bufferWriter.WriteVector3(entity.GlobalPosition, -5000, 5000, 0.01f);

            // Write entity rotation
            bufferWriter.WriteQuaternion(entity.Quaternion);
            
            // Write what components a entity has in ascending ID order
            for (ushort i = 0; i < _componentManager.ComponentIdToComponentNameDictionary.Count; i++)
                bufferWriter.WriteBool(_componentManager.HasComponent(entity, i));
            
            // Write out the details for each component in order
            var components = entity.Components;
            for (ushort i = 0; i < _componentManager.ComponentIdToComponentNameDictionary.Count; i++)
            {
                var componentName = _componentManager.ComponentIdToComponentNameDictionary[i];

                if (!components.TryGetValue(componentName, out var component))
                    continue;
                
                component.WriteUnreliableChannel(ref bufferWriter);
            }
        }
        
        bufferWriter.FinalWrite(out var byteIndex);
        Array.Resize(ref buffer, byteIndex);
        Broadcast(ENetChannels.UnreliableServer, buffer);
    }
}