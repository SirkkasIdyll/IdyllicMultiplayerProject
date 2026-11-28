namespace Game.Temperance.Network.Client;

public partial class ENetClient
{
    public void ProcessStateUpdates(ref BufferReader bufferReader)
    {
        // Read server tick
        bufferReader.Read(sizeof(uint) * 8, out var serverTick);
        
        // Read count of entities in the packet
        bufferReader.Read(10, out var entities);

        if (entities == 0)
            return;
        
        for (var i = 0; i < entities; i++)
        {
            // Read the entity network ID
            bufferReader.Read(sizeof(ushort) * 8, out var entityNetworkId);
            
            // If we're on an unreliable channel, we'll just skip it if it isn't spawned yet
            if (!_entityManager.EntityNetworkIdDictionary.Inverse.TryGetValue((ushort)entityNetworkId, out var entity))
                continue;
            
            // Read entity position
            entity.SetGlobalPosition(bufferReader.ReadVector3(-5000, 5000, 0.01f));
                
            // Read entity rotation
            entity.SetQuaternion(bufferReader.ReadQuaternion());
            
            // Loop through all known components, check if the packet says the entity has it,
            // and then update component details as necessary
            for (var j = 0; j < _componentManager.ComponentIdToComponentNameDictionary.Count; j++)
                bufferReader.ReadBool();

            for (var j = 0; j < _componentManager.ComponentIdToComponentNameDictionary.Count; j++)
            {
                var componentId = (ushort)j;
                
                if (!_componentManager.TryGetComponent(entity, componentId, out var component))
                    continue;

                // Different fields will be communicated,
                // depending on whether the message was intended for the reliable channel
                // or unreliable channel
                component.ReadUnreliableChannel(ref bufferReader);
            }
        }
    }
}