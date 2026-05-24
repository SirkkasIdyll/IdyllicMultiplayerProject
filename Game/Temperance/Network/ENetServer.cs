using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ENet;
using Godot;
using Google.Protobuf;
using Game.Resources.ProtocolBuffers;
using Game.Shared.Systems.Movement;
using Game.Temperance.NCS;
using Game.Temperance.Signals;
using Games.Resources.ProtocolBuffers;

namespace Game.Temperance.Network;

public partial class ENetServer : Node
{
    private readonly SignalBus _signalBus = SignalBus.Instance;
    public static ENetServer Instance { get; } = new();
    
    public const string Ip = "127.0.0.1";
    public const ushort Port = 3802;
    private const ushort MaxDuplicatePeers = 0;
    private readonly Host _server = new();
    private readonly Dictionary<Peer, Guid> _verifiedPeers = new();
    
    public override void _Ready()
    {
        base._Ready();
        
        var address = new Address();
        address.SetHost(Ip);
        address.Port = Port;
        _server.Create(address, 2);
        
        _server.SetChannelLimit(Enum.GetNames<ENetChannels>().Length);
        // if (MaxDuplicatePeers > 0)
        //     _server.SetMaxDuplicatePeers(MaxDuplicatePeers);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        
        _server.Dispose();
    }
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        
        if (_server.CheckEvents(out var netEvent) <= 0)
            if (_server.Service(0, out netEvent) <= 0)
                return;
        
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
        GD.Print("ENet Disconnected from peer id: " + netEvent.Peer.ID + ", Guid: " + _verifiedPeers[netEvent.Peer]);
        _verifiedPeers.Remove(netEvent.Peer);
    }

    private void OnPeerTimeout(Event netEvent)
    {
        GD.Print("ENet Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);

    }

    private void OnPeerReceivedPacket(Event netEvent)
    {
        GD.Print("ENet Packet received from - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
        var buffer = new byte[netEvent.Packet.Length];
        
        // For connection verifications, we don't want to pre-check if the peer is verified
        if ((ENetChannels)netEvent.ChannelID == ENetChannels.ConnectionVerification)
        {
            netEvent.Packet.CopyTo(buffer);
            var clientGuidMessage = ConnectionVerificationRequest.Parser.ParseFrom(buffer);
            
            // Check for a valid guid
            if (!Guid.TryParse(clientGuidMessage.Guid, out var guid))
            {
                GD.Print("ENet Peer id: " + netEvent.Peer.ID + " attempted to verify with invalid guid: " + guid + ". Disconnecting.");
                netEvent.Peer.DisconnectNow(0);
                return;
            }
            
            // Check if they're trying to verify themselves with a guid that belongs to another peer
            if (_verifiedPeers.ContainsValue(guid))
            {
                GD.Print("ENet Peer id: " + netEvent.Peer.ID + " attempted to connect with already verified guid: " + guid + ". Disconnecting.");
                netEvent.Peer.DisconnectNow(0);
                return;
            }
            
            // Not really sure if I want to disconnect just the weird actor, or potentially both people in this case
            if (!_verifiedPeers.TryAdd(netEvent.Peer, guid) && _verifiedPeers[netEvent.Peer] != guid)
            {
                GD.Print("ENet Peer id: " + netEvent.Peer.ID + " is already verified but attempting to connect with different guid. Disconnecting.");
                netEvent.Peer.DisconnectNow(0);
                return;
            }
            
            GD.Print("ENet Registered peer id: " + netEvent.Peer.ID + ", Guid: " + guid);
            NodeManager.Instance.TrySpawnNode("TestCharacter", new Vector3(0, 2, 0), guid, out _);
            return;
        }
        
        // For every other message received, we want to know if the peer is guid verified
        // otherwise we want to disconnect from them immediately
        if (!IsPeerVerified(netEvent.Peer))
            return;
        
        netEvent.Packet.CopyTo(buffer);
        switch ((ENetChannels)netEvent.ChannelID)
        {
            default:
            case ENetChannels.ConnectionVerification:
                break;
            
            case ENetChannels.UserInput:
                var userInputMessage = UserInput.Parser.ParseFrom(buffer);
                if (userInputMessage.Movement is not null)
                {
                    var signal = new ReceiveMovementInputSignal(_verifiedPeers[netEvent.Peer], new Vector2(userInputMessage.Movement.X, userInputMessage.Movement.Y));
                    _signalBus.EmitReceiveMovementInputSignal(_verifiedPeers[netEvent.Peer], new Vector2(userInputMessage.Movement.X, userInputMessage.Movement.Y), ref signal);
                    GD.Print("User input received: (" + userInputMessage.Movement.X + ", " + userInputMessage.Movement.Y + ")");
                }
                
                break;
        }
    }

    /// <summary>
    /// Broadcast a message to everyone on this channel
    /// </summary>
    /// <param name="channel">Refer to <see cref="ENetChannels"/></param>
    /// <param name="message">A protobuf message</param>
    /// <param name="flag">Use reliable for time sequential info or if you need acknowledgement</param>
    private void Broadcast(ENetChannels channel, IMessage message, PacketFlags flag = PacketFlags.None)
    {
        var buffer = new byte[message.CalculateSize()];
        message.WriteTo(buffer);

        var packet = new Packet();
        packet.Create(buffer, flag);

        _server.Broadcast((byte)channel, ref packet);
    }

    /// <summary>
    /// Broadcast a message to a set of specific peers on this channel
    /// </summary>
    /// <param name="channel">Refer to <see cref="ENetChannels"/></param>
    /// <param name="message">A protobuf message</param>
    /// <param name="peers">List of peers you want the message to go to</param>
    /// <param name="flag">Use reliable for time sequential info or if you need acknowledgement</param>
    private void Broadcast(ENetChannels channel, IMessage message, Peer[] peers, PacketFlags flag = PacketFlags.None)
    {
        var buffer = new byte[message.CalculateSize()];
        message.WriteTo(buffer);

        var packet = new Packet();
        packet.Create(buffer, flag);

        _server.Broadcast((byte)channel, ref packet, peers);
    }
    
    /// <summary>
    /// Broadcast a message to everyone except for this peer on this channel
    /// </summary>
    /// <param name="channel">Refer to <see cref="ENetChannels"/></param>
    /// <param name="message">A protobuf message</param>
    /// <param name="peer">The unlucky peer who doesn't get the message</param>
    /// <param name="flag">Use reliable for time sequential info or if you need acknowledgement</param>
    private void Broadcast(ENetChannels channel, IMessage message, Peer peer, PacketFlags flag = PacketFlags.None)
    {
        var buffer = new byte[message.CalculateSize()];
        message.WriteTo(buffer);

        var packet = new Packet();
        packet.Create(buffer, flag);

        _server.Broadcast((byte)channel, ref packet, peer);
    }

    /// <summary>
    /// Returns true if peer is verified,
    /// otherwise returns false and disconnects the peer
    /// for attempting to send a packet without being verified
    /// </summary>
    private bool IsPeerVerified(Peer peer)
    {
        if (_verifiedPeers.ContainsKey(peer))
            return true;
        
        peer.DisconnectNow(0);
        return false;
    }

    public bool IsPeerVerified(Guid guid)
    {
        if (_verifiedPeers.ContainsValue(guid))
            return true;

        return false;
    }

    /// <summary>
    /// Waits for an amount of time until peer is verified
    /// </summary>
    /// <param name="guid">ENet Peer Guid</param>
    /// <param name="cancellationToken"></param>
    /// <param name="timeout">Time to wait in seconds before giving up</param>
    /// <returns></returns>
    public async Task<bool> IsPeerVerifiedAsync(Guid guid, CancellationToken cancellationToken, long timeout = 5)
    {
        long i = 0;
        var retryTimeMs = 200;

        while (i < timeout * 1000 || !cancellationToken.IsCancellationRequested)
        {
            if (_verifiedPeers.ContainsValue(guid))
                return true;

            await Task.Delay(TimeSpan.FromMilliseconds(retryTimeMs), cancellationToken);
            i += retryTimeMs;
            GD.Print("Waiting for peer verification: " + i);
        }

        return false;
    }
}

public enum ENetChannels : byte
{
    ConnectionVerification = 0, // Used for notifying ENetServer of connections
    UserInput = 1,
}