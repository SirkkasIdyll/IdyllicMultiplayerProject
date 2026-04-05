using Godot;
using IdyllicMultiplayerProject.Temperance;

namespace IdyllicMultiplayerProject.Shared.Systems;

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
}