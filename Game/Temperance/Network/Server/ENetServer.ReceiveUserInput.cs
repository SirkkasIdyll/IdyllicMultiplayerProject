using ENet;
using Game.Shared.Systems.Camera;
using Game.Shared.Systems.Movement;
using Godot;

namespace Game.Temperance.Network.Server;

public partial class ENetServer
{
    public void ProcessUserInput(Peer peer, ref BufferReader bufferReader)
    {
        foreach (var (networkId, controlledByPeer) in _entitiesControlledByPeers)
        {
            if (!_entityManager.NetworkIdEntityDictionary.TryGetValue(networkId, out var entity))
                continue;
            
            if (controlledByPeer == null)
                continue;
            
            if (peer.ID != controlledByPeer.Value.ID)
                continue;
            
            // Read client tick
            bufferReader.Read(sizeof(uint) * 8, out var clientTick);
        
            // Read user input
            var userInput = bufferReader.ReadVector2(-1f, 1f, 0.01f);
            
            // Read camera angle
            var cameraAngle = bufferReader.ReadFloat(-2 * Mathf.Pi, 2 * Mathf.Pi, 0.01f);

            if (!_componentManager.TryGetComponent<MovementComponent>(entity, out var movementComponent))
                return;
            
            movementComponent.InputDirection = userInput;

            if (!_componentManager.TryGetComponent<CameraComponent>(entity, out var cameraComponent))
                return;
            
            cameraComponent.AngleOfCameraPositionFromDefaultPosition = cameraAngle;
        }
    }
}