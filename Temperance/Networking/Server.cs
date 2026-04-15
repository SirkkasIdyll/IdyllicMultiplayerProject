using System;
using ENet;
using Godot;
using Google.Protobuf;

namespace IdyllicMultiplayerProject.Temperance.Networking;

public partial class Server : Node3D
{
    public enum ChannelIDs : byte
    {
        Unreliable = 0,
        Reliable = 1,
    }
    
    /// <summary>
    /// Maximum amount of duplicate connections from the same host,
    /// set to a non-zero value if you want to limit it
    /// </summary>
    private const ushort MaxDuplicatePeers = 0;
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
        
        if (MaxDuplicatePeers > 0)
            _server.SetMaxDuplicatePeers(MaxDuplicatePeers);

        var timer = new Timer();
        timer.WaitTime = 4;
        timer.Autostart = true;
        timer.OneShot = true;
        timer.Timeout += () =>
        {
            var thing = new test
            {
                ExDouble = 23.423f,
                ThisThing = true,
                GreatMindsThinkLikeThis = "I'm really the most smartest and beautifulest person in the world :3"
            };
            
            var buffer = new byte[thing.CalculateSize()];
            thing.WriteTo(buffer);

            var packet = new Packet();
            packet.Create(buffer);
            _server.Broadcast((byte)ChannelIDs.Unreliable, ref packet);
        };
        AddChild(timer);
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