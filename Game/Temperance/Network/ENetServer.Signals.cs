using System;
using System.Collections.Generic;
using System.Text;
using ENet;
using Game.Temperance.NCS;
using Godot;

namespace Game.Temperance.Network;

// In the interest of hiding all the non-network related stuff from the main class
// Here's a file with all the packet formations
public partial class ENetServer
{
    private List<Node> _nodesSpawnsToBroadcast = [];
    
    private void OnNodeSpawned(Node node, ref NodeSpawnedSignal args)
    {
        _nodesSpawnsToBroadcast.Add(node);
    }

    private void OnServerMessageTimer(ref ServerMessageTimerSignal args)
    {
        BroadcastNodeSpawns();
    }

    private void BroadcastNodeSpawns()
    {
        if (_nodesSpawnsToBroadcast.Count == 0)
            return;
        
        var nodeManager = NodeManager.Instance;
        var componentManager = ComponentManager.Instance;
        var buffer = new byte[10000];
        var bufferWriter = new BufferWriter(buffer);

        // Write packet type
        bufferWriter.WriteInt((byte)ReliableServerPacketType.NodeSpawns, 0, Networking.MaxPacketTypesPerChannel);
        
        // Write server tick
        bufferWriter.StoreOrWrite(_serverTick, sizeof(uint) * 8);
        
        // Write count of nodes
        bufferWriter.StoreOrWrite((uint)_nodesSpawnsToBroadcast.Count, 10);
        
        foreach (var node in _nodesSpawnsToBroadcast)
        {
            if (!nodeManager.NodeNetworkIdDictionary.TryGetValue(node, out var networkId))
                continue;
            
            if (node is not Node3D node3D)
                continue;

            var nodeName = nodeManager.GetNodeName(node);
            
            // Write length of node name
            // (I'm just gonna assume we're not gonna need more than 32 bytes to write a simple name man)
            bufferWriter.WriteInt(Encoding.UTF8.GetByteCount(nodeName), 0, 32);
            
            // Write node name for spawning, shorter than the full file path
            bufferWriter.StoreOrWrite(Encoding.UTF8.GetBytes(nodeName));
            
            // Write node network ID
            bufferWriter.StoreOrWrite(networkId, sizeof(ushort) * 8);
            
            // Write node position
            bufferWriter.WriteVector3(node3D.GlobalPosition, -5000, 5000, 0.01f);
            
            // Write node rotation (the only rotation that matters in our top down game)
            bufferWriter.WriteFloat(node3D.GlobalRotation.Y, -2 * Mathf.Pi, 2 * Mathf.Pi, 0.01f);
            
            // Write what components a node has in order so that the upcoming details can be read in order
            for (ushort i = 0; i < componentManager.ComponentIdToComponentNameDictionary.Count; i++)
            {
                bufferWriter.WriteBool(componentManager.HasComponent(node, i));
            }
            
            // Write out the details for each component in order
            // We only need to write out the reliable channel because the unreliable-side gets communicated regularly,
            // often enough that we don't need to care
            var components = componentManager.ComponentDictionaryByNode[node];
            for (ushort i = 0; i < componentManager.ComponentIdToComponentNameDictionary.Count; i++)
            {
                var componentName = componentManager.ComponentIdToComponentNameDictionary[i];

                if (!components.TryGetValue(componentName, out var component))
                    continue;
                
                component.WriteReliableChannel(ref bufferWriter);
            }
        }
        
        bufferWriter.FinalWrite(out var byteIndex);
        Array.Resize(ref buffer, byteIndex);
        Broadcast(ENetChannels.ReliableServer, buffer, PacketFlags.Reliable);
        _nodesSpawnsToBroadcast.Clear();
    }
}