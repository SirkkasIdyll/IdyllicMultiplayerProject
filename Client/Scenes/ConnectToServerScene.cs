using Godot;
using IdyllicMultiplayerProject.Temperance.Networking;

namespace IdyllicMultiplayerProject.Client.Scenes;

public partial class ConnectToServerScene : Control
{
	[Export] private Button? _connectButton;
	[Export] private Button? _closeButton;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_connectButton?.Pressed += () =>
		{
			ENetClient.Instance.ToggleConnection(ENetServer.Ip, ENetServer.Port);
			GRpcClient.Instance.ToggleConnection(GRpcServer.Ip, GRpcServer.Port);
		};
		_closeButton?.Pressed += QueueFree;
	}
}
