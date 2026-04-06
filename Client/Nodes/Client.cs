using Godot;
using System;

namespace IdyllicMultiplayerProject.Client.Nodes;

public partial class Client : Node3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
		Multiplayer.ConnectedToServer += OnConnectedToServer;
		Multiplayer.ServerDisconnected += OnServerDisconnected;
		Multiplayer.ConnectionFailed += OnConnectionFailed;
		
		// Create client.
		var clientPeer = new ENetMultiplayerPeer();
		var error = clientPeer.CreateClient("192.168.1.14", 3087);
		if (error == Error.Ok)
			GD.Print("Created client");
		else
			GD.Print(error);
		Multiplayer.SetMultiplayerPeer(clientPeer);
		GD.Print(Multiplayer.IsServer() + " " + Multiplayer.GetUniqueId() + " " + Multiplayer.GetRemoteSenderId());
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void OnConnectionFailed()
	{
		GD.Print("Failed to connect to server");
		Multiplayer.SetMultiplayerPeer(null);

		GD.Print("Retrying connection");
		// Create client.
		var clientPeer = new ENetMultiplayerPeer();
		var error = clientPeer.CreateClient("192.168.1.14", 3087);
		if (error == Error.Ok)
			GD.Print("Created client");
		else
			GD.Print(error);
		Multiplayer.SetMultiplayerPeer(clientPeer);
		GD.Print(Multiplayer.IsServer() + " " + Multiplayer.GetUniqueId() + " " + Multiplayer.GetRemoteSenderId());
	}

	private void OnConnectedToServer()
	{
		GD.Print("Connected to server");
	}
	
	private void OnPeerConnected(long peerId)
	{
		GD.Print("Connected to peer with ID: " + peerId);
	}

	private void OnPeerDisconnected(long peerId)
	{
		GD.Print("Disconnected from peer with ID: " + peerId);
	}

	private void OnServerDisconnected()
	{
		GD.Print("Disconnected from server");
	}
}
