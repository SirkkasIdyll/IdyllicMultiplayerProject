using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Grpc.Core;
using IdyllicMultiplayerProject.Temperance.NCS;
using IdyllicMultiplayerProject.Temperance.Network;
using IdyllicMultiplayerProject.Temperance.Signals;
using Resources.ProtocolBuffers.Spawn;
using static Resources.ProtocolBuffers.Spawn.NodeSpawner;

namespace IdyllicMultiplayerProject.Resources.ProtocolBuffers.Spawn;

/// <summary>
/// Server-side gRPC service that is instantiated each time a new connection with a client is made
/// </summary>
public partial class NodeSpawnerService : NodeSpawnerBase
{
    private readonly NodeManager _nodeManager = NodeManager.Instance;
    private readonly SignalBus _signalBus = SignalBus.Instance;
    private readonly Queue<Tuple<Guid, string>> _queue = new();

    /// <summary>
    /// When a node is spawned on the server,
    /// queue it up to be communicated to clients
    /// </summary>
    private void OnNodeSpawned(Guid nodeNetworkGuid)
    {
        if (!_nodeManager.NetGuidDictionary.TryGetValue(nodeNetworkGuid, out var nodeUpdateInfo))
            return;
        
        _queue.Enqueue(Tuple.Create(nodeNetworkGuid, nodeUpdateInfo.Node.Name.ToString()));
    }
    
    public override async Task NodeSpawnStream(IAsyncStreamReader<RequestNodeSpawnInfo> requestStream, IServerStreamWriter<ReplyNodeSpawnInfo> responseStream, ServerCallContext context)
    {
        if (context.RequestHeaders.Get("Authorization") == null)
            return;

        if (!Guid.TryParse(context.RequestHeaders.Get("Authorization")?.Value, out var guid))
            return;
        
        if (!await ENetServer.Instance.IsPeerVerifiedAsync(guid, context.CancellationToken, 20))
            return;

        _signalBus.NodeSpawnedSignal += OnNodeSpawned;
        
        // Server tells client what to spawn based off of requested network GUID
        var readTask = Task.Run(async () =>
        {
            // Receive client message
            await foreach (var request in requestStream.ReadAllAsync())
            {
                // If it fails, guid was invalid
                if (!Guid.TryParse(request.NodeNetworkGuid, out var nodeNetworkGuid))
                    continue;

                // If it fails, we have no node with that guid on the server
                if (!_nodeManager.NetGuidDictionary.TryGetValue(nodeNetworkGuid, out var nodeUpdateInfo))
                    continue;
                
                // Send client the information requested to spawn the node
                await responseStream.WriteAsync(new ReplyNodeSpawnInfo
                {
                    NodeNetworkGuid = request.NodeNetworkGuid,
                    NodeName = nodeUpdateInfo.Node.Name.ToString()
                });
            }
        });
        
        // On first connection, server tells client to spawn everything available
        foreach (var (netGuid, nodeUpdateInfo) in _nodeManager.NetGuidDictionary)
        {
            // Send client the information needed to spawn the node
            await responseStream.WriteAsync(new ReplyNodeSpawnInfo
            {
                NodeNetworkGuid = netGuid.ToString(),
                NodeName = nodeUpdateInfo.Node.Name.ToString()
            });
        }
        
        // Server tells client to spawn thing without the client asking for it
        while (!context.CancellationToken.IsCancellationRequested || !readTask.IsCompleted)
        {
            if (_queue.TryDequeue(out var tuple))
            {
                var nodeNetworkGuid = tuple.Item1;
                var nodeName = tuple.Item2;
                await responseStream.WriteAsync(new ReplyNodeSpawnInfo
                {
                    NodeNetworkGuid = nodeNetworkGuid.ToString(),
                    NodeName = nodeName
                });
            }
            
            await Task.Delay(TimeSpan.FromMilliseconds(Networking.PhysicsTickLength), context.CancellationToken);
        } 
        
        _signalBus.NodeSpawnedSignal -= OnNodeSpawned;
    }
}