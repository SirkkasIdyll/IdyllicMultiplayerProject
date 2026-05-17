using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Grpc.Core;
using IdyllicMultiplayerProject.Temperance.NCS;
using IdyllicMultiplayerProject.Temperance.Network;
using IdyllicMultiplayerProject.Temperance.Signals;
using Resources.ProtocolBuffers.Spawn;

namespace IdyllicMultiplayerProject.Resources.ProtocolBuffers.Spawn;

public partial class NodeSpawnerClient(ChannelBase channel) : NodeSpawner.NodeSpawnerClient(channel)
{
    private readonly SignalBus _signalBus = SignalBus.Instance;
    
    /// <summary>
    /// Bidirectional stream
    /// 
    /// Receives: Name of node to spawn and the network guid associated with it
    /// Sends: Network guids that the client doesn't have information on so it can spawn it and update it properly
    /// </summary>
    public async Task Run(Metadata headers, CancellationToken cancellationToken)
    {
        using var stream = NodeSpawnStream(headers, null, cancellationToken);
        
        // RECEIVE NODE NAMES AND SPAWN THEM LOCALLY
        var readTask = Task.Run(async () =>
        {
            await foreach (var response in stream.ResponseStream.ReadAllAsync())
            {
                if (Guid.TryParse(response.NodeNetworkGuid, out var nodeNetworkGuid))
                    _signalBus.EmitRequestSpawnNodeSignal(nodeNetworkGuid, response.NodeName, response.Components);
            }
        });

        // SEND NETWORK GUIDS THAT WE HAVE NO IDEA ABOUT TO SERVER
        while (!readTask.IsCompleted)
        {
            await Task.Delay(Networking.PhysicsTickSpan);
        }
    
        await readTask;
    }
}