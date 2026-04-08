using Godot;
using System;

namespace IdyllicMultiplayerProject.Shared.Scenes;

public partial class TheWorldScene : Node3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("ZA WARUDO!");
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		GD.Print("TOKI WO TOMARE");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
