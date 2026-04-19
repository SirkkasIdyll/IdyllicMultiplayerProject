using System;
using System.Threading.Tasks;
using Godot;
using Grpc.Core;
using Resources.ProtocolBuffers;
using static Resources.ProtocolBuffers.Spawner;

namespace IdyllicMultiplayerProject.Resources.ProtocolBuffers;

public class SpawnerService : SpawnerBase
{
    public override async Task SpawnStream(IAsyncStreamReader<SpawnInfoRequest> requestStream,
        IServerStreamWriter<SpawnInfoReply> responseStream, ServerCallContext context)
    {
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