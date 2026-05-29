using System;
using System.Threading;
using System.Threading.Tasks;
using Game.Resources.ProtocolBuffers;
using Google.Protobuf.Collections;
using Grpc.Core;
using Game.Temperance.Network;
using Game.Temperance.Signals;

namespace Game.Client.Services.GRpc.Spawn;

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
        
        // RECEIVE NODE NAMES AND SPAWN THEM LOCALLY
        var readTask = Task.Run(async () =>
        {
            using var stream = NodeSpawnStream(headers, null, cancellationToken);
            await foreach (var response in stream.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken))
            {
                if (!Guid.TryParse(response.NodeNetworkGuid, out var nodeNetworkGuid))
                    continue;
                
                var signal = new RequestSpawnSignal
                {
                    NetGuid = nodeNetworkGuid,
                    ProtoName = response.NodeName,
                    Components = response.Components
                };
                _signalBus.EmitRequestSpawnSignal(ref signal);
            }
        });

        // SEND NETWORK GUIDS THAT WE HAVE NO IDEA ABOUT TO SERVER
        while (!readTask.IsCompleted)
        {
            await Task.Delay(Networking.ServerTickSpan);
        }
    
        await readTask;
    }
}

public class RequestSpawnSignal : UserSignalArgs
{
    public Guid NetGuid;
    public string ProtoName = "";
    public RepeatedField<string> Components = new();
}