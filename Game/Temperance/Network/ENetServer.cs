using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ENet;
using Godot;
using Game.Temperance.NCS;
using Game.Temperance.Signals;

namespace Game.Temperance.Network;

public partial class ENetServer : Node
{
    private readonly SignalBus _signalBus = SignalBus.Instance;
    public static ENetServer Instance { get; } = new();
    
    public const string Ip = "127.0.0.1";
    public const ushort Port = 3802;
    private const ushort MaxDuplicatePeers = 0;
    private readonly Host _server = new();
    
    // The Guid used won't change between disconnects,
    // but the peer may change as a result of disconnects
    // and the node they control may not even exist yet
    private readonly Dictionary<Guid, Peer?> _verifiedPeers = new();
    
    // A node may be controlled by a peer,
    // but sometimes the peer will disconnect and be reassigned to a different Peer ID
    // So we keep it nullable in case someone else connects
    private readonly Dictionary<Node, Peer?> _nodesControlledByPeers = new();
    
    /// <summary>
    /// When this value exceeds the <see cref="Networking"/> server tick length
    /// We allow the server to send or receive messages in the interest of reducing used bandwidth
    /// </summary>
    private double _serverMessageTimer = 0;
    /// <summary>
    /// Tracks the server physics tick we are on since the server first started so that we can say
    /// something was at this value on this server tick, for client prediction (and replays in the far future).
    /// The server currently runs physics at 60 hz so this is good for 2 years without restarting the value
    /// </summary>
    private uint _serverTick = 0;
    
    /// <summary>
    /// We queue these up so we can process them on the next physics tick
    /// </summary>
    private readonly Queue<(Peer peer, byte[] buffer)> _unreliableBufferQueue = new();
    
    /// <summary>
    /// We queue these up so we can process them on the next physics tick
    /// </summary>
    private readonly Queue<(Peer peer, byte[] buffer)> _reliableBufferQueue = new();
    
    public override void _Ready()
    {
        base._Ready();
        
        var address = new Address();
        address.SetHost(Ip);
        address.Port = Port;
        _server.Create(address, 2);
        
        // if (MaxDuplicatePeers > 0)
        //     _server.SetMaxDuplicatePeers(MaxDuplicatePeers);
        _server.SetChannelLimit(Enum.GetNames<ENetChannels>().Length);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        
        _server.Dispose();
    }
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        _serverTick++;
        _serverMessageTimer += delta;
         if (_serverMessageTimer >= Networking.ServerTickLength)
        {
            var signal = new ServerMessageTimerSignal();
            _signalBus.EmitServerMessageTimerSignal(ref signal);
            _serverMessageTimer = 0;
        }
         
        while (_reliableBufferQueue.Count > 0)
        {
            var (peer, buffer) = _reliableBufferQueue.Dequeue();
            Networking.ProcessPacketFromChannel(ENetChannels.ReliableClient, peer, buffer);
        }

        while (_unreliableBufferQueue.Count > 0)
        {
            var (peer, buffer) = _unreliableBufferQueue.Dequeue();
            Networking.ProcessPacketFromChannel(ENetChannels.UnreliableClient, peer, buffer);
        }

        while (_server.CheckEvents(out var netEvent) > 0 || _server.Service(0, out netEvent) > 0)
        {
            switch (netEvent.Type) {
                case EventType.None:
                    break;
                case EventType.Connect:
                    OnPeerConnected(netEvent);
                    break;
                case EventType.Disconnect:
                    OnPeerDisconnected(netEvent);
                    break;
                case EventType.Timeout:
                    OnPeerTimeout(netEvent);
                    break;
                case EventType.Receive:
                    OnPeerReceivedPacket(netEvent);
                    netEvent.Packet.Dispose();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        _server.Flush();
    }
    
    private void OnPeerConnected(Event netEvent)
    {
        GD.Print("ENet Client connected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
        var signal = new PeerConnectedSignal(netEvent);
        _signalBus.EmitPeerConnectedSignal(netEvent, ref signal);
    }

    private void OnPeerDisconnected(Event netEvent)
    {
        GD.Print("ENet Disconnected from peer id: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
    }

    private void OnPeerTimeout(Event netEvent)
    {
        GD.Print("ENet Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
        DisconnectPeer(netEvent.Peer);
    }

    private void OnPeerReceivedPacket(Event netEvent)
    {
        GD.Print("ENet Packet received from - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
        var buffer = new byte[netEvent.Packet.Length];
        netEvent.Packet.CopyTo(buffer);
        
        switch ((ENetChannels)netEvent.ChannelID)
        {
            case ENetChannels.ReliableClient:
                _reliableBufferQueue.Enqueue((netEvent.Peer, buffer));
                break;
            case ENetChannels.UnreliableClient:
                _unreliableBufferQueue.Enqueue((netEvent.Peer, buffer));
                break;
            // We should never be getting messages from another server so ignore it lol
            case ENetChannels.ReliableServer:
            case ENetChannels.UnreliableServer:
            default:
                break;
        }
    }
    
    public bool TryClientVerification(Peer peer, ref BufferReader bufferReader)
    {
        // // Clients are not allowed to send messages to our other channels while they are unverified
        // // SERVER KILL THAT PERSON
        // if (!_verifiedPeers.ContainsValue(peer) &&
        //     (ENetChannels)netEvent.ChannelID != ENetChannels.ReliableClient)
        // {
        //     DisconnectPeer(netEvent.Peer);
        //     return;
        // }
        //
        // // For connection verifications, we don't want to pre-check if the peer is verified
        // if (packetType != (int)ReliableClientPacketType.ConnectionVerificationAttempt)
        // {
        //     DisconnectPeer(netEvent.Peer);
        //     return;
        // }
        
        var guid = new Guid(bufferReader.Read(16));
        // Check for a valid guid
        if (guid.Version != 7)
        {
            GD.Print("ENet Peer id: " + peer.ID + " attempted to verify with invalid guid: " + guid + ". Disconnecting.");
            DisconnectPeer(peer);
            return false;
        }
            
        // Check if they're trying to verify themselves with a guid that belongs to another peer
        if (_verifiedPeers.ContainsKey(guid) && _verifiedPeers[guid].HasValue)
        {
            GD.Print("ENet Peer id: " + peer.ID + " attempted to connect with already verified guid: " + guid + " belonging to a connected peer. Disconnecting.");
            DisconnectPeer(peer);
            return false;
        }
            
        // Not really sure if I want to disconnect just the weird actor, or potentially both people in this case
        if (_verifiedPeers.ContainsKey(guid) && _verifiedPeers[guid].HasValue && _verifiedPeers.ContainsValue(peer) && !Equals(_verifiedPeers[guid]!.Value, peer))
        {
            GD.Print("ENet Peer id: " + peer.ID + " is already verified but attempting to connect with different guid. Disconnecting.");
            DisconnectPeer(peer);
            return false;
        }
            
        GD.Print("ENet Registered peer id: " + peer.ID + ", Guid: " + guid);
        _verifiedPeers.Add(guid, peer);
        
        // TODO: Replace this later with a proper join -> spawn flow
        if (NodeManager.Instance.TrySpawnNode("TestCharacter", new Vector3(0, 2, 0), null, out var node3D))
            _nodesControlledByPeers.Add(node3D, peer);
        return true;
    }

    /// <summary>
    /// Broadcast a message to everyone on this channel
    /// </summary>
    /// <param name="channel">Refer to <see cref="ENetChannels"/></param>
    /// <param name="buffer">A message to be put into the packet</param>
    /// <param name="flag">Use reliable for time sequential info or if you need acknowledgement</param>
    public void Broadcast(ENetChannels channel, byte[] buffer, PacketFlags flag = PacketFlags.None)
    {
        var packet = new Packet();
        packet.Create(buffer, flag);

        // _dataSentKb += message.CalculateSize();
        _server.Broadcast((byte)channel, ref packet);
    }

    /// <summary>
    /// Broadcast a message to a set of specific peers on this channel
    /// </summary>
    /// <param name="channel">Refer to <see cref="ENetChannels"/></param>
    /// <param name="buffer">A message to be put into the packet</param>
    /// <param name="peers">List of peers you want the message to go to</param>
    /// <param name="flag">Use reliable for time sequential info or if you need acknowledgement</param>
    public void Broadcast(ENetChannels channel, byte[] buffer, Peer[] peers, PacketFlags flag = PacketFlags.None)
    {
        var packet = new Packet();
        packet.Create(buffer, flag);

        _server.Broadcast((byte)channel, ref packet, peers);
    }
    
    /// <summary>
    /// Broadcast a message to everyone except for this peer on this channel
    /// </summary>
    /// <param name="channel">Refer to <see cref="ENetChannels"/></param>
    /// <param name="buffer">A message to be put into the packet</param>
    /// <param name="peer">The unlucky peer who doesn't get the message</param>
    /// <param name="flag">Use reliable for time sequential info or if you need acknowledgement</param>
    public void Broadcast(ENetChannels channel, byte[] buffer, Peer peer, PacketFlags flag = PacketFlags.None)
    {
        var packet = new Packet();
        packet.Create(buffer, flag);

        _server.Broadcast((byte)channel, ref packet, peer);
    }

    /// <summary>
    /// Disconnects the peer, and if we have a guid or node correlated with that peer, sets the peer it knows to null
    /// </summary>
    /// <param name="peer"></param>
    private void DisconnectPeer(Peer peer)
    {
        var verifiedPeer = _verifiedPeers.FirstOrDefault(x => x.Value != null && Equals(x.Value.Value, peer));
        if (verifiedPeer.Value != null)
            _verifiedPeers[verifiedPeer.Key] = null;
        
        var nodeControlledByPeer =
            _nodesControlledByPeers.FirstOrDefault(x => x.Value != null && Equals(x.Value.Value, peer));
        if (nodeControlledByPeer.Value != null)
            _nodesControlledByPeers[nodeControlledByPeer.Key] = null;
        
        peer.DisconnectNow(0);
    }
    
    // TODO: REMOVE THIS
    // private void OnServerMessageTimer(ref ServerMessageTimerSignal args)
    // {
    //     if (!Networking.IsServer())
    //         return;
    //     
    //     var signal = new SendNodeStatesSignal();
    //     foreach (var (netGuid, _) in NetGuidDictionary)
    //         signal.Message.NodeState.Add(new NodeState
    //         {
    //             Sequence = Library.Time,
    //             NodeNetworkGuid = netGuid.ToString()
    //         });
    //     
    //     _signalBus.EmitSendNodeStatesSignal(ref signal);
    //     Broadcast(ENetChannels.TickUpdateUnreliable, signal.Message);
    // }
    //
    // public NodeState GetLatestPredictedNodeState(NodeUpdateInfo nodeUpdateInfo)
    // {
    //     if (nodeUpdateInfo.PredictedNodeStates.Count == 0)
    //     {
    //         var nodeState = new NodeState { Sequence = nodeUpdateInfo.CurrSequence + (uint)(Networking.PhysicsTickLength * 1000) };
    //         nodeUpdateInfo.PredictedNodeStates.Add(nodeState.Sequence, nodeState);
    //         return nodeUpdateInfo.PredictedNodeStates[0];
    //     }
    //
    //     if (nodeUpdateInfo.PredictedNodeStates.GetAt(nodeUpdateInfo.PredictedNodeStates.Count - 1).Value
    //             .Transform is null)
    //         return new NodeState();
    //         
    //     return new NodeState();
    // }
}

public class ServerMessageTimerSignal : UserSignalArgs;
