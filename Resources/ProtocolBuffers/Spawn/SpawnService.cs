using System;
using System.Threading.Tasks;
using Godot;
using Grpc.Core;
using IdyllicMultiplayerProject.Temperance.Networking;
using Resources.ProtocolBuffers.Spawn;
using static Resources.ProtocolBuffers.Spawn.Spawner;

namespace IdyllicMultiplayerProject.Resources.ProtocolBuffers.Spawn;

public class SpawnService : SpawnerBase
{
    public override async Task SpawnStream(IAsyncStreamReader<SpawnInfoRequest> requestStream,
        IServerStreamWriter<SpawnInfoReply> responseStream, ServerCallContext context)
    {
        if (context.RequestHeaders.Get("Authorization") == null)
            return;

        if (!Guid.TryParse(context.RequestHeaders.Get("Authorization")?.Value, out var guid))
            return;
        
        if (!ENetServer.Instance.IsPeerVerified(guid))
            return;
        
        var physicsTickLength = (long)1 / Engine.GetPhysicsTicksPerSecond();
        
        var i = 60;
        while (!context.CancellationToken.IsCancellationRequested)
        {
            await responseStream.WriteAsync(new SpawnInfoReply { NodeNetworkId = 1, NodeName = "Test Node"});
            await Task.Delay(TimeSpan.FromSeconds(i), context.CancellationToken);
            i += 60;
            GD.Print("Time waited this time: " + i);
        }
    }
}