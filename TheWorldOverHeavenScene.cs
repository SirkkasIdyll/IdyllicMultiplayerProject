using Godot;
using IdyllicMultiplayerProject.Temperance.NCS;
using IdyllicMultiplayerProject.Temperance.Networking;

namespace IdyllicMultiplayerProject;

public partial class TheWorldOverHeavenScene : Node3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _EnterTree()
	{
		base._EnterTree();

		NodeSystemManager.Instance.InitializeNodeSystems(this);
		AddChild(NodeManager.Instance);

		if (OS.HasFeature("dedicated_server") || DisplayServer.GetName() == "headless")
		{
			AddChild(new ENetServer());
			AddChild(new GRpcServer());
		}
		else
		{
			AddChild(new ENetClient());
			AddChild(new GRpcClient());
		}
		
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		
		ComponentManager.Instance.PurgeDictionary();
	}
}
