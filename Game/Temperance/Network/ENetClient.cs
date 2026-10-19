using System;
using ENet;
using Godot;
using Game.Temperance.NCS;
using Game.Temperance.Signals;

namespace Game.Temperance.Network;

public partial class ENetClient : Node
{
    private readonly SignalBus _signalBus = SignalBus.Instance;
    public static ENetClient Instance { get; } = new();

    /// <summary>
    /// This is a temporary form of "authentication" that identifies a client before
    /// I move on to the real stuff
    /// </summary>
    public Guid EnetGuid = Guid.CreateVersion7();
    
    /// <summary>
    /// Sends and receives packets
    /// </summary>
    private readonly Host _client = new();
    
    /// <summary>
    /// Client's only connect to the server, never with each other, so they only have one peer :(
    /// </summary>
    private Peer? _serverPeer;
    
    /// <summary>
    /// Kind of like the <see cref="ENetServer"/>'s server tick, except the only real reason we track it on the client
    /// is so we can tell the server when a specific input happened, then it can tell us when the last client tick
    /// it received was, so we can rollback to the CLIENT input tick specifically to see if what we predicted was good
    /// </summary>
    private uint _clientTick = 0;

    public override void _Ready()
    {
        base._Ready();

        _client.Create();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        _serverPeer?.DisconnectNow(0);
        _client.Dispose();
    }
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        
        if (_serverPeer == null)
            return;

        _clientTick++;
        while (_client.CheckEvents(out var netEvent) > 0 || _client.Service(0, out netEvent) > 0)
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
        
        _client.Flush();
    }
    
    private void OnPeerConnected(Event netEvent)
    {
        GD.Print("ENet Client connected to server. " + EnetGuid);
        
        // Send connection verification so that other packets sent
        // do not get rejected causing an immediate disconnection
        var connectionVerificationRequest = new ConnectionVerificationRequest { Guid = EnetGuid.ToString() };
        Send(ENetChannels.ConnectionVerificationReliable, connectionVerificationRequest, PacketFlags.Reliable);
    }

    private void OnPeerDisconnected(Event netEvent)
    {
        GD.Print("ENet Client disconnected from server");
        var signal = new PeerDisconnectedSignal(netEvent);
        _signalBus.EmitPeerDisconnectedSignal(netEvent, ref signal);
    }

    private void OnPeerTimeout(Event netEvent)
    {
        GD.Print("ENet Client connection timeout");
    }

    private void OnPeerReceivedPacket(Event netEvent)
    {
        // GD.Print("ENet Packet received from server - Channel ID: " + netEvent.ChannelID + ", Data length: " +
        //          netEvent.Packet.Length);
        
        var buffer = new byte[netEvent.Packet.Length];
        netEvent.Packet.CopyTo(buffer);

        var bufferReader = new BufferReader(buffer);
        var channel = bufferReader.ReadInt(0, Networking.ChannelMax);
        
        // The message was sent via the wrong channel and you should feel bad about it
        if (netEvent.ChannelID != channel)
            return;
        
        switch ((ENetChannels)channel)
        {
            case ENetChannels.ConnectionVerificationReliable:
                break;
            case ENetChannels.UserInputUnreliable:
                break;
            case ENetChannels.TickUpdateUnreliable:
                var message = NodeStates.Parser.ParseFrom(buffer);
                var signal = new ReceiveNodeStatesSignal() { Message = message };
                _signalBus.EmitReceiveNodeStatesSignal(ref signal);
                break;
            case ENetChannels.EventsAndOnChangeReliable:
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Returns percent of packets lost compared to packets sent
    /// </summary>
    /// <returns>Also returns 0 if not connected</returns>
    public ulong GetPacketLoss()
    {
        if (_serverPeer != null && _serverPeer.Value.PacketsSent != 0)
            return _serverPeer.Value.PacketsLost / _serverPeer.Value.PacketsSent;
        
        return 0;
    }
    
    /// <summary>
    /// Returns last-known round trip time if connected to server
    /// </summary>
    /// <returns>0 if not connected</returns>
    public uint GetPing()
    {
        return _serverPeer?.LastRoundTripTime ?? 0;
    }

    /// <summary>
    /// Check if ENet client is connected to server
    /// </summary>
    public bool IsConnected()
    {
        return _serverPeer?.State == PeerState.Connected;
    }

    /// <summary>
    /// Connects to a server if not connected
    /// If already connected, disconnect from server
    /// </summary>
    /// <param name="host"></param>
    /// <param name="port"></param>
    public void ToggleConnection(string host, ushort port)
    {
        if (_serverPeer?.State == PeerState.Connected)
        {
            TryDisconnect();
            return;
        }
        
        TryConnect(host, port);
    }

    /// <summary>
    /// Send a message to the server on this channel
    /// </summary>
    /// <param name="channel">Refer to <see cref="ENetChannels"/></param>
    /// <param name="buffer">A message to be put into the packet</param>
    /// <param name="flag">Use reliable for time sequential info or if you need acknowledgement</param>
    public void Send(ENetChannels channel, byte[] buffer, PacketFlags flag = PacketFlags.None)
    {
        var packet = new Packet();
        packet.Create(buffer, flag);

        _serverPeer?.Send((byte)channel, ref packet);
    }

    /// <summary>
    /// Attempts to connect to a given hostname and port
    /// Check <see cref="_serverPeer"/> to see how the connection went
    /// </summary>
    /// <param name="host">Can be a host name or IP address</param>
    /// <param name="port"></param>
    private void TryConnect(string host, ushort port)
    {
        var address = new Address();
        address.Port = port;
        address.SetHost(host);

        // TODO: Does not gracefully handle rejections when SetMaxDuplicatePeers is 1/hit
        _serverPeer = _client.Connect(address, Enum.GetNames<ENetChannels>().Length);
    }

    /// <summary>
    /// Attempts to disconnect from a server after all queued outgoing packets have been sent
    /// </summary>
    private void TryDisconnect()
    {
        _serverPeer?.DisconnectLater(0);
    }
}