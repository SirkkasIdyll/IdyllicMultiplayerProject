using System;
using ENet;
using Godot;
using Game.Temperance.Signals;

namespace Game.Temperance.Network;

public static class Networking
{
    public static readonly double PhysicsTickLength = (double)1 / Engine.GetPhysicsTicksPerSecond();
    public static readonly TimeSpan PhysicsTickSpan = TimeSpan.FromSeconds(PhysicsTickLength);
    public static readonly double ServerTickLength = PhysicsTickLength * 2;
    public static readonly TimeSpan ServerTickSpan = TimeSpan.FromSeconds(ServerTickLength);
    public const int MaxPacketTypesPerChannel = 15;
    
    public static bool IsServer()
    {
        if (OS.GetCmdlineUserArgs().Contains("--server"))
            return true;
        
        if (DisplayServer.GetName() == "headless")
            return true;
        
        if (OS.HasFeature("dedicated_server"))
            return true;

        return false;
    }

    public static void ConnectToServer()
    {
        ENetClient.Instance.ToggleConnection(ENetServer.Ip, ENetServer.Port);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="channel">The channel the packet came from</param>
    /// <param name="peer">The peer who sent the packet</param>
    /// <param name="buffer">The packet</param>
    public static void ProcessPacketFromChannel(ENetChannels channel, Peer peer, byte[] buffer)
    {
        var bufferReader = new BufferReader(buffer);
        var packetType = bufferReader.ReadInt(0, MaxPacketTypesPerChannel);
        
        switch (channel, packetType)
        {
            case (ENetChannels.ReliableClient, (int)ReliableClientPacketType.ConnectionVerificationAttempt):
                ENetServer.Instance.TryClientVerification(peer, ref bufferReader);
                break;
            case (ENetChannels.UnreliableClient, (int)UnreliableClientPacketType.UserInput):
                break;
            case (ENetChannels.ReliableServer, (int)ReliableServerPacketType.LateJoinSynchronization):
                break;
            case (ENetChannels.ReliableServer, (int)ReliableServerPacketType.NodeSpawns):
                break;
            case (ENetChannels.ReliableServer, (int)ReliableServerPacketType.NodeDespawns):
                break;
            case (ENetChannels.ReliableServer, (int)ReliableServerPacketType.ComponentAdded):
                break;
            case (ENetChannels.ReliableServer, (int)ReliableServerPacketType.ComponentRemoved):
                break;
            case (ENetChannels.ReliableServer, (int)ReliableServerPacketType.SynchronizeOnChange):
                break;
            case (ENetChannels.UnreliableServer, (int)UnreliableServerPacketType.StateUpdates):
                break;
            default:
                break;
        }
    }
}

/// <summary>
/// 0 is used solely for initially connecting to the server
/// 1 is used solely by clients to communicate their input to the server
/// 2 is used solely by the server to communicate constant updates and last-known input ticks to clients
/// 3 is used solely by the server to communicate infrequent updates like spawn/despawns, component field OnChanges, or full world state to late joining users
/// </summary>
public enum ENetChannels : byte
{
    ReliableClient = 0,
    UnreliableClient = 1, 
    ReliableServer = 2,
    UnreliableServer = 3
}

/// <summary>
/// 
/// </summary>
public enum ReliableClientPacketType : byte
{
    ConnectionVerificationAttempt = 0,
}

public enum UnreliableClientPacketType : byte
{
    UserInput = 0
}

public enum ReliableServerPacketType : byte
{
    LateJoinSynchronization = 0,
    NodeSpawns = 1,
    NodeDespawns = 2,
    ComponentAdded = 3,
    ComponentRemoved = 4,
    SynchronizeOnChange = 5
}

public enum UnreliableServerPacketType : byte
{
    StateUpdates = 0
}

public class PeerConnectedSignal : UserSignalArgs
{
    private Event NetEvent;
    
    public PeerConnectedSignal(Event netEvent)
    {
        NetEvent = netEvent;
    }
}

public class PeerDisconnectedSignal : UserSignalArgs
{
    private Event NetEvent;
    
    public PeerDisconnectedSignal(Event netEvent)
    {
        NetEvent = netEvent;
    }
}

// public abstract class Message
// {
//     /// <summary>
//     /// The channel this message is intended for, don't forget it!
//     /// </summary>
//     public required ENetChannels Channel;
// }
//
// public class ConnectionVerificationMessage : Message
// {
//     public required Guid ClientGuid;
// }
//
// public class UserInputMessage : Message
// {
//     public required uint ClientTick;
//     public required Vector2[] UserInput;
// }
//
// public class TickUpdateMessage : Message
// {
//     public required uint ServerTick; 
//     public required uint ClientTick;
//     public required uint NodesInMessage;
//     public required (uint NodeNetworkId, bool[] ComponentsIncluded, Vector3 NodePosition, float Rotation, bool[]
//         ComponentDetails) Nodes;
// }
//
// public class EventsAndOnChangeMessage : Message
// {
//     public required uint ServerTick; 
//     public required uint ClientTick;
//     public required uint NodesInMessage;
//     public required (uint NodeNetworkId, bool[] ComponentsIncluded, Vector3 NodePosition, float Rotation, bool[]
//         ComponentDetails) Nodes;
// }

// public class NodeUpdateInfo
// {
//     public readonly Node3D Node;
//     // This exists here because we can't TryGetComponent<> in async threads i.e. gRPC services
//     public readonly MetadataComponent? MetadataComponent;
//     public uint CurrSequence;
//     public OrderedDictionary<uint, NodeState> PredictedNodeStates = new();
//
//     public NodeUpdateInfo(Node3D node, MetadataComponent? metadataComponent)
//     {
//         Node = node;
//         MetadataComponent = metadataComponent;
//     }
// }
//
// public class SendNodeStatesSignal : UserSignalArgs
// {
//     public NodeStates Message = new();
// }
//
// public class ReceiveNodeStatesSignal : UserSignalArgs
// {
//     public NodeStates Message = new();
// }