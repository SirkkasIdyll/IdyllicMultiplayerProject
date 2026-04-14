using System;
using ENet;
using Godot;

namespace IdyllicMultiplayerProject.Temperance.Networking;

public partial class Client : Node3D
{
    private readonly Host _client = new();
    private Peer _peer;
    
    public override void _Ready()
    {
        base._Ready();
        
        _client.Create();
        
        var address = new Address();
        address.Port = Server.Port;
        address.SetHost(Server.Ip);

        _peer = _client.Connect(address);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        
        _peer.DisconnectNow(0);
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
                var buffer = new byte[64];
                BitBuffer data = new BitBuffer(1024);
                netEvent.Packet.CopyTo(buffer);
                data.FromArray(buffer, netEvent.Packet.Length);
                GD.Print(data.ReadString());
                GD.Print(data.ReadBool());
                GD.Print(data.ReadString());
                netEvent.Packet.Dispose();
                break;
        }
        
        _client.Flush();
    }
}