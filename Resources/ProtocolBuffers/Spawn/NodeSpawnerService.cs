using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using IdyllicMultiplayerProject.Temperance.Network;
using Resources.ProtocolBuffers.Spawn;
using static Resources.ProtocolBuffers.Spawn.NodeSpawner;

namespace IdyllicMultiplayerProject.Resources.ProtocolBuffers.Spawn;

public partial class NodeSpawnerService : NodeSpawnerBase
{
    public Queue<string> spawnQueue = new();
    
    public override async Task NodeSpawnStream(IAsyncStreamReader<RequestNodeSpawnInfo> requestStream, IServerStreamWriter<ReplyNodeSpawnInfo> responseStream, ServerCallContext context)
    {
        if (context.RequestHeaders.Get("Authorization") == null)
            return;

        if (!Guid.TryParse(context.RequestHeaders.Get("Authorization")?.Value, out var guid))
            return;
        
        if (!await ENetServer.Instance.IsPeerVerifiedAsync(guid, context.CancellationToken, 20))
            return;
        
        spawnQueue.Enqueue("TheWorld");
        while (!context.CancellationToken.IsCancellationRequested)
        {
            if (spawnQueue.TryDequeue(out var nodeName))
            {
                var nodeNetworkGuid = Guid.CreateVersion7().ToString();
                
                await responseStream.WriteAsync(new ReplyNodeSpawnInfo
                {
                    NodeNetworkGuid = nodeNetworkGuid,
                    NodeName = nodeName
                });
            }
            
            await Task.Delay(TimeSpan.FromMilliseconds(Networking.PhysicsTickLength), context.CancellationToken);
        } 
    }
}