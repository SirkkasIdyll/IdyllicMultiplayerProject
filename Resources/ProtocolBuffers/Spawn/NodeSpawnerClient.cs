using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Grpc.Core;
using IdyllicMultiplayerProject.Temperance.Network;
using Resources.ProtocolBuffers.Spawn;

namespace IdyllicMultiplayerProject.Resources.ProtocolBuffers.Spawn;

public partial class NodeSpawnerClient : NodeSpawner.NodeSpawnerClient
{
    public NodeSpawnerClient(ChannelBase channel) : base(channel) { }

    /// <summary>
    /// Bidirectional stream
    /// Receive: Name of node to spawn and the network guid associated with it
    /// Send: Network guids that the client doesn't have information on so it can spawn it and update it properly
    /// </summary>
    public async Task Run(Metadata headers, CancellationToken cancellationToken)
    {
        using var stream = NodeSpawnStream(headers, null, cancellationToken);
        
        // RECEIVE NODE NAMES AND SPAWN THEM LOCALLY
        var readTask = Task.Run(async () =>
        {
            await foreach (var response in stream.ResponseStream.ReadAllAsync())
            {
                GD.Print("ID: " + response.NodeNetworkGuid + " - Name: " + response.NodeName);
            }
        });

        // SEND NETWORK GUIDS THAT WE HAVE NO IDEA ABOUT TO SERVER
        while (!readTask.IsCompleted)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(Networking.PhysicsTickLength));
        }
    
        await readTask;
    }
}