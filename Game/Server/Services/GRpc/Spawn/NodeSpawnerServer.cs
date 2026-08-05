using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Resources.ProtocolBuffers;
using Grpc.Core;
using Game.Shared.Systems.Metadata;
using Game.Temperance.NCS;
using Game.Temperance.Network;
using Game.Temperance.Signals;

namespace Game.Server.Services.GRpc.Spawn;

/// <summary>
/// Server-side gRPC service that is instantiated each time a new connection with a client is made
/// </summary>
public partial class NodeSpawnerServer : NodeSpawner.NodeSpawnerBase
{
    private readonly ComponentManager _componentManager = ComponentManager.Instance;
    private readonly NodeManager _nodeManager = NodeManager.Instance;
    private readonly SignalBus _signalBus = SignalBus.Instance;
    private readonly Queue<Tuple<Guid, NodeUpdateInfo>> _queue = new();

    /// <summary>
    /// When a node is spawned on the server,
    /// queue it up to be communicated to clients
    /// </summary>
    private void OnNodeSpawned(Guid nodeNetworkGuid, ref NodeSpawnedSignal args)
    {
        if (!_nodeManager.NetGuidDictionary.TryGetValue(nodeNetworkGuid, out var nodeUpdateInfo))
            return;
        
        _queue.Enqueue(Tuple.Create(nodeNetworkGuid, nodeUpdateInfo));
    }
    
    /// <summary>
    /// 1. Send the client everything they need to spawn to catch up to the server
    /// 2. Listen for the client sending network GUIDs they need to spawn and tell them what to spawn
    /// 3. Tell clients to spawn something when a node is spawned on the server
    /// </summary>
    public override async Task NodeSpawnStream(IAsyncStreamReader<RequestNodeSpawnInfo> requestStream, IServerStreamWriter<ReplyNodeSpawnInfo> responseStream, ServerCallContext context)
    {
        if (context.RequestHeaders.Get("Authorization") == null)
            return;

        if (!Guid.TryParse(context.RequestHeaders.Get("Authorization")?.Value, out var guid))
            return;
        
        if (!await ENetServer.Instance.IsPeerVerifiedAsync(guid, context.CancellationToken, 20))
            return;

        _signalBus.NodeSpawnedSignal += OnNodeSpawned;
        
        // On first connection, server tells client to spawn everything available
        foreach (var (netGuid, nodeUpdateInfo) in _nodeManager.NetGuidDictionary)
        {
            if (nodeUpdateInfo.MetadataComponent == null)
                continue;
            
            await ReplyWithSpawnInfo(responseStream, netGuid, nodeUpdateInfo.MetadataComponent);
        }
        
        // Receive network GUIDs from clients, and tell them what to spawn based on the network GUID on the server
        var readTask = Task.Run(async () =>
        {
            // Receive client message
            await foreach (var request in requestStream.ReadAllAsync())
            {
                // If it fails, guid was invalid
                if (!Guid.TryParse(request.NodeNetworkGuid, out var netGuid))
                    continue;

                // If it fails, we have no node with that guid on the server
                if (!_nodeManager.NetGuidDictionary.TryGetValue(netGuid, out var nodeUpdateInfo))
                    continue;
                
                if (nodeUpdateInfo.MetadataComponent == null)
                    continue;
            
                await ReplyWithSpawnInfo(responseStream, netGuid, nodeUpdateInfo.MetadataComponent);
            }
        });
        
        // When the Server tells client to spawn thing without the client asking for it
        while (!context.CancellationToken.IsCancellationRequested || !readTask.IsCompleted)
        {
            // Loop through everything we need to spawn
            while (_queue.TryDequeue(out var tuple))
            {
                var netGuid = tuple.Item1;
                var nodeUpdateInfo = tuple.Item2;
                
                if (nodeUpdateInfo.MetadataComponent == null)
                    continue;
                
                await ReplyWithSpawnInfo(responseStream, netGuid, nodeUpdateInfo.MetadataComponent);
            }
            
            await Task.Delay(Networking.ServerTickSpan, context.CancellationToken);
        } 
        
        _signalBus.NodeSpawnedSignal -= OnNodeSpawned;
    }

    // Send client the information needed to spawn the node
    private static async Task ReplyWithSpawnInfo(IServerStreamWriter<ReplyNodeSpawnInfo> responseStream, Guid netGuid, MetadataComponent metadataComponent)
    {
        await responseStream.WriteAsync(new ReplyNodeSpawnInfo
        {
            NodeNetworkGuid = netGuid.ToString(),
            NodeName = metadataComponent.PrototypeName,
            Components = { metadataComponent.ComponentDictionary.Keys }
        });
    }
}