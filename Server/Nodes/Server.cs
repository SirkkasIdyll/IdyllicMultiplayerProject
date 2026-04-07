using Godot;
using System;

namespace IdyllicMultiplayerProject.Server.Nodes;

public partial class Server : Node3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
		
		// Create server.
		var serverPeer = new ENetMultiplayerPeer();
		serverPeer.SetBindIP("192.168.1.14");
		var error = serverPeer.CreateServer(3087, 2);
		if (error == Error.Ok)
			GD.Print("Created server");
		else
			GD.Print(error);
		Multiplayer.SetMultiplayerPeer(serverPeer);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	[Rpc]
	private void OnPeerConnected(long peerId)
	{
		GD.Print("Connected to peer with ID: " + peerId);
	}

	private void OnPeerDisconnected(long peerId)
	{
		GD.Print("Disconnected from peer with ID: " + peerId);
	}
}
