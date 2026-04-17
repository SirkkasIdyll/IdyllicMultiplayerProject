using ENet;
using Godot;
using Google.Protobuf;
using Resources.ProtocolBuffers;

namespace IdyllicMultiplayerProject.Temperance.Networking;

public partial class ENetServer : Node
{
    public const string Ip = "127.0.0.1";
    public const ushort Port = 3802;
    private const ushort MaxDuplicatePeers = 0;
    private readonly Host _server = new();
    
    public override void _Ready()
    {
        base._Ready();
        
        var address = new Address();
        address.SetHost(Ip);
        address.Port = Port;
        _server.Create(address, 2);
        
        if (MaxDuplicatePeers > 0)
            _server.SetMaxDuplicatePeers(MaxDuplicatePeers);
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
                GD.Print("Doing nothing");
                break;

            case EventType.Connect:
                GD.Print("Client connected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                break;

            case EventType.Disconnect:
                GD.Print("Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                break;

            case EventType.Timeout:
                GD.Print("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                break;

            case EventType.Receive:
                GD.Print("Packet received from - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                netEvent.Packet.Dispose();
                break;
        }
        
        _server.Flush();
    }

    private void SendUnreliable(IMessage message)
    {
        var buffer = new byte[message.CalculateSize()];
        message.WriteTo(buffer);

        var packet = new Packet();
        packet.Create(buffer);

        _server.Broadcast((byte)ENetChannels.Unreliable, ref packet);
    }

    private void SendReliable(IMessage message)
    {
        var buffer = new byte[message.CalculateSize()];
        message.WriteTo(buffer);

        var packet = new Packet();
        packet.Create(buffer, PacketFlags.Reliable);
        
        _server.Broadcast((byte)ENetChannels.Reliable, ref packet);
    }
}

public enum ENetChannels : byte
{
    Unreliable = 0,
    Reliable = 1,
}