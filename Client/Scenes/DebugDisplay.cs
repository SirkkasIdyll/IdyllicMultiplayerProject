using Godot;
using IdyllicMultiplayerProject.Temperance.Networking;

namespace IdyllicMultiplayerProject.Client.Scenes;

public partial class DebugDisplay : Control
{
	[Export] private Label? _fpsCounter;
	[Export] private Label? _pingCounter;
	[Export] private Label? _packetLossCounter;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_fpsCounter?.Text = Engine.GetFramesPerSecond().ToString("N0");
		_pingCounter?.Text = ENetClient.Instance.GetPing().ToString();
		_packetLossCounter?.Text = ENetClient.Instance.GetPacketLoss().ToString("P");
	}
}
