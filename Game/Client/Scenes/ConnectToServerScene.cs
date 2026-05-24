using Godot;
using Game.Temperance.Network;

namespace Game.Client.Scenes;

public partial class ConnectToServerScene : Control
{
	[Export] private Button? _connectButton;
	[Export] private Button? _closeButton;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_connectButton?.Pressed += Networking.ConnectToServer;
		_closeButton?.Pressed += QueueFree;
	}
}
