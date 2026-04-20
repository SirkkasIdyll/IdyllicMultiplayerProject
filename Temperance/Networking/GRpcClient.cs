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
    
    public override void _ExitTree()
    {
        base._ExitTree();
        
        _grpcChannel?.Dispose();
    }

    /// <summary>
    /// Required configurations to keep the connection with the server alive during periods without messages
    /// </summary>
    /// <returns></returns>
    public void ConfigureGrpcChannel(string host, ushort port)
    {
        var handler = new SocketsHttpHandler
        {
            KeepAlivePingDelay = TimeSpan.FromMinutes(1),
            KeepAlivePingTimeout = TimeSpan.FromMinutes(1)
        };
        
        _grpcChannel = GrpcChannel.ForAddress("https://" + host + ":" + port, new GrpcChannelOptions()
        {
            HttpHandler =  handler
        });
        
        var headers = new Metadata();
        headers.Add("Authorization", $"{ENetClient.Instance.EnetGuid}");
        _ = SetupSpawner(new SpawnerClient(_grpcChannel), headers);
    }

    private async Task SetupSpawner(SpawnerClient client, Metadata headers)
    {
        using var stream = client.SpawnStream(headers);
        
        // Infinitely going read task, I think?
        var readTask = Task.Run(async () =>
        {
            await foreach (var response in stream.ResponseStream.ReadAllAsync())
            {
                GD.Print("ID: " + response.NodeNetworkId + " - Name: " + response.NodeName);
            }
        });

        // while (!readTask.IsCompleted)
        // {
        //     
        // }
    
        await readTask;
    }
}