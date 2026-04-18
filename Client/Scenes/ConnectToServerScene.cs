using Godot;
using System;
using IdyllicMultiplayerProject.Temperance.Networking;

namespace IdyllicMultiplayerProject.Client.Scenes;

public partial class ConnectToServerScene : Control
{
	[Export] private Button? _connectButton;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_connectButton?.Pressed += () => ENetClient.Instance.ToggleConnection(ENetServer.Ip, ENetServer.Port);
	}
}
