using ENet;
using Godot;

namespace IdyllicMultiplayerProject.Temperance.Networking;

public partial class ENetClient : Node
{
    private readonly Host _client = new();
    private Peer? _peer;

    public override void _Ready()
    {
        base._Ready();

        _client.Create();

        var address = new Address();
        address.Port = ENetServer.Port;
        address.SetHost(ENetServer.Ip);

        _peer = _client.Connect(address);
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
        
        if (_client.CheckEvents(out var netEvent) <= 0)
            if (_client.Service(0, out netEvent) <= 0)
                return;

        switch (netEvent.Type) {
            case EventType.None:
                break;

            case EventType.Connect:
                GD.Print("Client connected to server");
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
}