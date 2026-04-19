using System;
using ENet;
using Godot;
using Google.Protobuf;
using Resources.ProtocolBuffers;

namespace IdyllicMultiplayerProject.Temperance.Networking;

public partial class ENetClient : Node
{
    public static ENetClient Instance { get; } = new();
    
    private Guid _enetGuid = Guid.CreateVersion7();
    private readonly Host _client = new();
    private Peer? _peer;

    public override void _Ready()
    {
        base._Ready();

        _client.Create();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        _peer?.DisconnectNow(0);
        _client.Dispose();
    }
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (_peer == null)
            return;
        
        if (_client.CheckEvents(out var netEvent) <= 0)
            if (_client.Service(0, out netEvent) <= 0)
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
                break;
        }
        
        _client.Flush();
    }
    
    private void OnPeerConnected(Event netEvent)
    {
        GD.Print("Client connected to server. " + _enetGuid);
        
        // Send connection verification so that other packets sent
        // do not get rejected causing an immediate disconnection
        var connectionVerificationRequest = new ConnectionVerificationRequest { Guid = _enetGuid.ToString() };
        Send(ENetChannels.ConnectionVerification, connectionVerificationRequest, PacketFlags.Reliable);
    }

    private void OnPeerDisconnected(Event netEvent)
    {
        GD.Print("Client disconnected from server");
    }

    private void OnPeerTimeout(Event netEvent)
    {
        GD.Print("Client connection timeout");
    }

    private void OnPeerReceivedPacket(Event netEvent)
    {
        GD.Print("Packet received from server - Channel ID: " + netEvent.ChannelID + ", Data length: " +
                 netEvent.Packet.Length);
    }

    /// <summary>
    /// Returns percent of packets lost compared to packets sent
    /// </summary>
    /// <returns>Also returns 0 if not connected</returns>
    public ulong GetPacketLoss()
    {
        if (_peer != null && _peer.Value.PacketsSent != 0)
            return _peer.Value.PacketsLost / _peer.Value.PacketsSent;
        
        return 0;
    }
    
    /// <summary>
    /// Returns last-known round trip time if connected to server
    /// </summary>
    /// <returns>0 if not connected</returns>
    public uint GetPing()
    {
        return _peer?.LastRoundTripTime ?? 0;
    }

    /// <summary>
    /// Check if ENet client is connected to server
    /// </summary>
    public bool IsConnected()
    {
        return _peer?.State == PeerState.Connected;
    }

    /// <summary>
    /// Connects to a server if not connected
    /// If already connected, disconnect from server
    /// </summary>
    /// <param name="host"></param>
    /// <param name="port"></param>
    public void ToggleConnection(string host, ushort port)
    {
        if (_peer?.State == PeerState.Connected)
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
    /// <param name="message">A protobuf message</param>
    /// <param name="flag">Use reliable for time sequential info or if you need acknowledgement</param>
    private void Send(ENetChannels channel, IMessage message, PacketFlags flag = PacketFlags.None)
    {
        var buffer = new byte[message.CalculateSize()];
        message.WriteTo(buffer);

        var packet = new Packet();
        packet.Create(buffer, flag);

        _peer?.Send((byte)channel, ref packet);
    }

    /// <summary>
    /// Attempts to connect to a given hostname and port
    /// Check <see cref="_peer"/> to see how the connection went
    /// </summary>
    /// <param name="host">Can be a host name or IP address</param>
    /// <param name="port"></param>
    private void TryConnect(string host, ushort port)
    {
        var address = new Address();
        address.Port = port;
        address.SetHost(host);

        // TODO: Does not gracefully handle rejections when SetMaxDuplicatePeers is 1/hit
        _peer = _client.Connect(address, Enum.GetNames<ENetChannels>().Length);
    }

    /// <summary>
    /// Attempts to disconnect from a server after all queued outgoing packets have been sent
    /// </summary>
    private void TryDisconnect()
    {
        _peer?.DisconnectLater(0);
    }
}