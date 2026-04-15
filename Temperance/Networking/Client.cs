using System;
using ENet;
using Godot;
using Grpc.Net.Client;
using Resources.ProtocolBuffers;

namespace IdyllicMultiplayerProject.Temperance.Networking;

public partial class Client : Node3D
{
    private readonly Host _client = new();
    private Peer _peer;
    private GrpcChannel? _grpcChannel;
    
    public override void _Ready()
    {
        base._Ready();
        
        _client.Create();
        
        var address = new Address();
        address.Port = Server.Port;
        address.SetHost(Server.Ip);

        _peer = _client.Connect(address);
        _grpcChannel = GrpcChannel.ForAddress("https://" + Server.Ip + ":" + Server.GrpcPort);
        var client = new Greeter.GreeterClient(_grpcChannel);
        var reply = client.SayHello(new HelloRequest { Name = "JOJOOO" });
        GD.Print(reply.Message);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        
        _peer.DisconnectNow(0);
        _client.Dispose();
        _grpcChannel?.Dispose();
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
                var buffer = new byte[netEvent.Packet.Length];
                netEvent.Packet.CopyTo(buffer);
                netEvent.Packet.Dispose();

                GD.Print(_grpcChannel?.State.ToString());
                // var something = test.Parser.ParseFrom(buffer);
                // GD.Print(something.ExDouble);
                // GD.Print(something.ThisThing);
                // GD.Print(something.GreatMindsThinkLikeThis);
                break;
        }
        
        _client.Flush();
    }
}