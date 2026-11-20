using System;
using Godot;
using Game.Temperance.EntityComponentSystem;
using Game.Temperance.Network;
using Game.Temperance.Network.Client;
using Game.Temperance.Network.Server;
using Game.Temperance.Signals;
using Environment = System.Environment;

namespace Game;

public partial class TheWorldOverHeavenScene : Node3D
{
	// Called when the entity enters the scene tree for the first time.
	public override void _EnterTree()
	{
		base._EnterTree();

		// The EntitySystemManager is not a entity but will add NodeSystems as children of this scene
		// whereas the EntityManager is a entity so we will add it as a child
		EntitySystemManager.Instance.InitializeNodeSystems(this);
		AddChild(EntityManager.Instance);
		AddChild(ComponentManager.Instance);
		
		// Add the required server or client instances depending on which we are
		// so that they may initialize themselves upon entry to the SceneTree
		// and delete the unused counterpart to prevent having an orphaned node
		if (Networking.IsServer())
		{
			AddChild(ENetServer.Instance);
			ENetClient.Instance.Free();
		}
		else
		{
			GetWindow().Title = Environment.GetCommandLineArgs().Contains("--verbose") ? "Verbose Client" : "Basic Client";

			AddChild(ENetClient.Instance);
			ENetServer.Instance.Free();
		}

		var entity = new Entity();
	}

	public override void _Ready()
	{
		base._Ready();
		
		if (Networking.IsServer())
			EntityManager.Instance.TrySpawnEntity("TheWorld", null, out _);
		
		var signal = new DependenciesInjectedSignal();
		SignalBus.Instance.EmitDependenciesInjectedSignal(ref signal);
	}
}
