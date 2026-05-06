using System;
using System.Threading.Tasks;
using Godot;
using Grpc.Core;
using IdyllicMultiplayerProject.Temperance.Network;
using Resources.ProtocolBuffers.Spawn;
using static Resources.ProtocolBuffers.Spawn.NodeSpawner;

namespace IdyllicMultiplayerProject.Resources.ProtocolBuffers.Spawn;

public partial class NodeSpawnerService : NodeSpawnerBase
{
    public override async Task NodeSpawnStream(IAsyncStreamReader<RequestNodeSpawnInfo> requestStream, IServerStreamWriter<ReplyNodeSpawnInfo> responseStream, ServerCallContext context)
    {
        GD.Print("hewoo");

        if (context.RequestHeaders.Get("Authorization") == null)
            return;

        if (!Guid.TryParse(context.RequestHeaders.Get("Authorization")?.Value, out var guid))
            return;
        
        if (!await ENetServer.Instance.IsPeerVerifiedAsync(guid, context.CancellationToken, 20))
            return;
        
        var i = 5;
        while (!context.CancellationToken.IsCancellationRequested)
        {
            GD.Print("Sending node spawn info");
            await responseStream.WriteAsync(new ReplyNodeSpawnInfo
            {
                NodeNetworkGuid = Guid.CreateVersion7().ToString(),
                NodeName = "Test Node"
            });
            await Task.Delay(TimeSpan.FromSeconds(i), context.CancellationToken);
            i += 5;
        } 
    }
}