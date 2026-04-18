using ENet;
using Godot;

namespace IdyllicMultiplayerProject.Temperance.Networking;

public partial class ENetClient : Node
{
    public static ENetClient Instance { get; } = new();
    
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
                GD.Print("Client connected to server.");
                break;

            case EventType.Disconnect:
                GD.Print("Client disconnected from server");
                break;

            case EventType.Timeout:
                GD.Print("Client connection timeout");
                break;

            case EventType.Receive:
                GD.Print("Packet received from server - Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                break;
        }
        
        _client.Flush();
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

        _peer = _client.Connect(address);
    }

    /// <summary>
    /// Attempts to disconnect from a server after all queued outgoing packets have been sent
    /// </summary>
    private void TryDisconnect()
    {
        _peer?.DisconnectLater(0);
    }
}