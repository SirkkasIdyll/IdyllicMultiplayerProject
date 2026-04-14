using System;
using ENet;
using Godot;

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
        timer.WaitTime = 2;
        timer.Autostart = true;
        timer.Timeout += () =>
        {
            var packet = new Packet();
            var buffer = new byte[64];
            BitBuffer data = new BitBuffer(1024);
            data.AddString("Wow, this is some pretty cool data.")
                .AddBool(true)
                .AddString(1.23452341f.ToString("R"))
                .ToArray(buffer);
            data.Clear();
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