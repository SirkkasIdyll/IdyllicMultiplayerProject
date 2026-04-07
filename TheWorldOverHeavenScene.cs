using Godot;
using System;
using IdyllicMultiplayerProject.Temperance;

namespace IdyllicMultiplayerProject;

public partial class TheWorldOverHeavenScene : Node3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _EnterTree()
	{
		base._EnterTree();

		NodeSystemManager.Instance.InitializeNodeSystems(this);
		NodeManager.Instance.InitializeNodeSpawner(this);

		if (OS.HasFeature("dedicated_server") || DisplayServer.GetName() == "headless")
		{
			// Create server.
			var peer = new ENetMultiplayerPeer();
			peer.CreateServer(3802, 2);
			Multiplayer.MultiplayerPeer = peer;
			Multiplayer.PeerConnected += (peerId) => GD.Print("Connected to peer: " + peerId);
		}
		else
		{
			// Create client.
			var peer = new ENetMultiplayerPeer();
			peer.CreateClient("127.0.0.1", 3802);
			Multiplayer.MultiplayerPeer = peer;
			Multiplayer.ConnectedToServer += () => GD.Print("Connected to server");
		}
		
		NodeManager.Instance.MainSpawner.AddSpawnableScene("res://Shared/Scenes/TheWorldScene.tscn");
		GD.Print(NodeManager.Instance.MainSpawner.GetSpawnableScene(0));
		if (OS.HasFeature("dedicated_server") || DisplayServer.GetName() == "headless")
		{
			AddChild(GD.Load<PackedScene>("res://Shared/Scenes/TheWorldScene.tscn").Instantiate<Node3D>());
		}
		// if (OS.HasFeature("dedicated_server") || DisplayServer.GetName() == "headless")
		// 	AddChild(new Server.Nodes.Server());
		// else
		// 	AddChild(new Client.Nodes.Client());
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		
		ComponentManager.Instance.PurgeDictionary();
	}
}
