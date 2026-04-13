using System;
using ENet;
using Godot;

namespace IdyllicMultiplayerProject.Temperance.Networking;

public partial class Server : Node3D
{
    public const string Ip = "127.0.0.1";
    public const ushort Port = 3802;

    private readonly Host _server = new();

    public override void _Ready()
    {
        base._Ready();
        
        var address = new Address();
        address.SetHost(Ip);
        address.Port = Port;

        _server.Create(address, 2);
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
}