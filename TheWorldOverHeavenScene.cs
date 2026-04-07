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
