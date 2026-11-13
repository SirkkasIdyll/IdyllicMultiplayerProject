using ENet;
using Game.Shared.Systems.Movement;

namespace Game.Temperance.Network.Server;

public partial class ENetServer
{
    public void ProcessUserInput(Peer peer, ref BufferReader bufferReader)
    {
        foreach (var (networkId, controlledByPeer) in _nodesControlledByPeers)
        {
            if (!_nodeManager.NetworkIdNodeDictionary.TryGetValue(networkId, out var node))
                continue;
            
            if (controlledByPeer == null)
                continue;
            
            if (peer.ID != controlledByPeer.Value.ID)
                continue;
            
            // Read client tick
            bufferReader.Read(sizeof(uint) * 8, out var clientTick);
        
            // Read user input
            var userInput = bufferReader.ReadVector2(-1f, 1f, 0.01f);

            if (!_componentManager.TryGetComponent<MovementComponent>(node, out var movementComponent))
                return;
            
            movementComponent.InputDirection = userInput;
        }
    }
}