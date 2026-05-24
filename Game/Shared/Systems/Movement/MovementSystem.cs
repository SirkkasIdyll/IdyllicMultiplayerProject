using Game.Resources.ProtocolBuffers;
using Godot;
using Game.Temperance.NCS;
using Game.Temperance.Network;
using Game.Temperance.Signals;
using Games.Resources.ProtocolBuffers;

namespace Game.Shared.Systems.Movement;

[GlobalClass]
public partial class MovementSystem : NodeSystem
{
    [InjectedDependency] private readonly SignalBus _signalBus = null!;
    [InjectedDependency] private readonly ComponentManager _componentManager = null!;
    [InjectedDependency] private readonly NodeSystemManager _nodeSystemManager = null!;

    public override void _Ready()
    {
        base._Ready();
        
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);

        if (Networking.IsServer())
            return;
        
        var velocity = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        var userInputMessage = new UserInput
        {
            Movement = new GdVector2 { X = velocity.X, Y = velocity.Y },
        };
        ENetClient.Instance.Send(ENetChannels.UserInput, userInputMessage);
    }
}