using System;
using System.Net.Http;
using System.Threading;
using Godot;
using Grpc.Core;
using Grpc.Net.Client;
using Game.Client.Services.GRpc.Spawn;

namespace Game.Temperance.Network;

public partial class GRpcClient : Node
{
    public static GRpcClient Instance { get; } = new();
    
    private GrpcChannel? _grpcChannel;
    private CancellationTokenSource? _cancellationTokenSource;

    
    public override void _ExitTree()
    {
        base._ExitTree();
        
        _grpcChannel?.Dispose();
    }

    /// <summary>
    /// Gets rid of the gRPC channel and triggers cancellation of all gRPC services
    /// </summary>
    public void ToggleConnection(string host, ushort port)
    {
        if (_grpcChannel?.State == ConnectivityState.Ready)
        {
            _grpcChannel?.ShutdownAsync();
            _grpcChannel?.Dispose();
            _grpcChannel = null;
            
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            return;
        }
        
        ConfigureGrpcChannel(host, port);
    }

    /// <summary>
    /// Required configurations to keep the connection with the server alive during periods without messages
    /// </summary>
    /// <returns></returns>
    private void ConfigureGrpcChannel(string host, ushort port)
    {
        var handler = new SocketsHttpHandler
        {
            KeepAlivePingDelay = TimeSpan.FromMinutes(1),
            KeepAlivePingTimeout = TimeSpan.FromMinutes(1)
        };
        
        _cancellationTokenSource = new CancellationTokenSource();
        _grpcChannel = GrpcChannel.ForAddress("https://" + host + ":" + port, new GrpcChannelOptions()
        {
            HttpHandler =  handler
        });

        
        var headers = new Metadata();
        headers.Add("Authorization", $"{ENetClient.Instance.EnetGuid}");
        
        // Set up all relevant gRPC clients using authorization headers and cancellation tokens to end the clients
        _ = new NodeSpawnerClient(_grpcChannel).Run(headers, _cancellationTokenSource.Token);
    }
}