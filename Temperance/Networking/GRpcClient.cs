using System;
using System.Net.Http;
using System.Threading.Tasks;
using Godot;
using Grpc.Core;
using Grpc.Net.Client;
using static Resources.ProtocolBuffers.Spawn.Spawner;

namespace IdyllicMultiplayerProject.Temperance.Networking;

public partial class GRpcClient : Node
{
    public static GRpcClient Instance { get; } = new();

    private GrpcChannel? _grpcChannel;
    
    public override void _Ready()
    {
        base._Ready();

        var handler = new SocketsHttpHandler
        {
            KeepAlivePingDelay = TimeSpan.FromMinutes(1),
            KeepAlivePingTimeout = TimeSpan.FromMinutes(1)
        };
        
        var grpcChannelOptions = new GrpcChannelOptions() { HttpHandler =  handler };
        _grpcChannel = GrpcChannel.ForAddress("https://" + GRpcServer.Ip + ":" + GRpcServer.Port, grpcChannelOptions);
        _ = SetupSpawner(new SpawnerClient(_grpcChannel));
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        
        _grpcChannel?.Dispose();
    }

    private async Task SetupSpawner(SpawnerClient client)
    {
        using var stream = client.SpawnStream();
        // Infinitely going read task, I think?
        var readTask = Task.Run(async () =>
        {
            await foreach (var response in stream.ResponseStream.ReadAllAsync())
            {
                GD.Print("ID: " + response.NodeNetworkId + " - Name: " + response.NodeName);
            }
        });
    
        await readTask;
    }
}