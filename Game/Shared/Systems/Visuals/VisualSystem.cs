using Game.Temperance.NCS;
using Game.Temperance.Signals;
using Godot;

namespace Game.Shared.Systems.Visuals;

/// <summary>
/// Controls various visual elements, at the moment deals with the desynchronization of
/// the visual location/rotation of a sprite versus the actual underlying position/rotation of the parent node
/// </summary>
[GlobalClass]
public partial class VisualSystem : NodeSystem
{
    [InjectedDependency] private readonly ComponentManager _componentManager = null!;
    [InjectedDependency] private readonly NodeManager _nodeManager = null!;
    [InjectedDependency] private readonly NodeSystemManager _nodeSystemManager = null!;
    [InjectedDependency] private readonly SignalBus _signalBus = null!;
    
    public override void _Ready()
    {
        base._Ready();

        _signalBus.ComponentsRegisteredSignal += OnComponentsRegistered;
    }
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
    }

    private void OnComponentsRegistered(Node node, ref ComponentsRegisteredSignal args)
    {
        SetTopLevelForVisualComponents(node);
    }
}