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

		// The NodeSystemManager is not a node but will add NodeSystems as children of this scene
		// whereas the NodeManager is a node so we will add it as a child
		NodeSystemManager.Instance.InitializeNodeSystems(this);
		AddChild(NodeManager.Instance);

		// Add the required server or client instances depending on which we are
		// so that they may initialize themselves upon entry to the SceneTree
		// and delete the unused counterpart to prevent having an orphaned node
		if (OS.HasFeature("dedicated_server") || DisplayServer.GetName() == "headless")
		{
			AddChild(ENetServer.Instance);
			AddChild(GRpcServer.Instance);
			ENetClient.Instance.Free();
			GRpcClient.Instance.Free();
		}
		else
		{
			AddChild(ENetClient.Instance);
			AddChild(GRpcClient.Instance);
			ENetServer.Instance.Free();
			GRpcServer.Instance.Free();
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		
		// Kill the orphans
		ComponentManager.Instance.PurgeDictionary();
	}
}
