using Game.Temperance.EntityComponentSystem;
using Game.Temperance.Signals;

namespace Game.Temperance.Network.Client;

public partial class ENetClient
{
    public void ProcessChangeControlledEntity(ref BufferReader bufferReader)
    {
        // Read server tick
        bufferReader.Read(sizeof(uint) * 8, out var serverTick);
        
        // Read the entity network ID
        bufferReader.Read(sizeof(ushort) * 8, out var entityNetworkId);

        if (!_entityManager.EntityNetworkIdDictionary.Inverse.TryGetValue((ushort)entityNetworkId, out var entity))
            return;

        ClientControlledEntity = entity;
        var signal = new UpdateClientControlledEntitySignal(entity);
        _signalBus.EmitUpdateClientControlledEntitySignal(entity, ref signal);
    }
}

public class UpdateClientControlledEntitySignal(Entity entity) : UserSignalArgs
{
    private Entity _entity = entity;
}