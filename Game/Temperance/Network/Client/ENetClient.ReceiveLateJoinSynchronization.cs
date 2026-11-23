using System.Text;
using Godot;

namespace Game.Temperance.Network.Client;

public partial class ENetClient
{
    public void ProcessLateJoinSynchronizationPacket(ref BufferReader bufferReader)
    {
        // Read server tick
        bufferReader.Read(sizeof(uint) * 8, out var serverTick);
        
        // Read count of entities in the packet
        bufferReader.Read(10, out var entityCount);
        
        for (var i = 0; i < entityCount; i++)
        {
            // Read how long the name of the entity is
            var entityNameByteCount = bufferReader.ReadInt(0, 32);

            // Read the entity name
            var entityName = Encoding.UTF8.GetString(bufferReader.Read(entityNameByteCount));
            
            // Read the entity network ID
            bufferReader.Read(sizeof(ushort) * 8, out var entityNetworkId);
            
            // Read entity position
            var globalPosition = bufferReader.ReadVector3(-5000, 5000, 0.01f);
            var globalRotation = bufferReader.ReadQuaternion();
            
            // Spawn the entity now that we have the name and ID if this came from a reliable channel
            if (!_entityManager.TrySpawnEntity(entityName, globalPosition, globalRotation, (ushort)entityNetworkId, out var entity))
                continue;
            
            // Loop through all known components, check if the packet says the entity has it,
            // and then update component details as necessary
            for (var j = 0; j < _componentManager.ComponentIdToComponentNameDictionary.Count; j++)
            {
                var componentId = (ushort)j;
                var hasComponent = bufferReader.ReadBool();
                
                // If server says it doesn't have a component, then we don't have the component
                // Remove it and move on
                if (!hasComponent)
                {
                    _componentManager.RemoveComponent(entity, componentId);
                    continue;
                }
                    
                // We only add the component for messages coming from the reliable channels
                // Ensure that the component exists on the node
                _componentManager.TryAddComponent(entity, componentId);
            }

            for (var j = 0; j < _componentManager.ComponentIdToComponentNameDictionary.Count; j++)
            {
                var componentId = (ushort)j;
                
                if (!_componentManager.TryGetComponent(entity, componentId, out var component))
                    continue;

                // Different fields will be communicated,
                // depending on whether the message was intended for the reliable channel
                // or unreliable channel
                component.ReadReliableChannel(ref bufferReader);
            }
        }
    }
}