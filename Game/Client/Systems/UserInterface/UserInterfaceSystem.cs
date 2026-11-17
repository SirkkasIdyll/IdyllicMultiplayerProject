using Godot;
using Game.Temperance.NCS;
using Game.Temperance.Signals;

namespace Game.Client.Systems.UserInterface;

[GlobalClass]
public partial class UserInterfaceSystem : NodeSystem
{
    [InjectedDependency] private readonly ComponentManager _componentManager = null!;
    [InjectedDependency] private readonly NodeManager _nodeManager = null!;
    [InjectedDependency] private readonly NodeSystemManager _nodeSystemManager = null!;
    [InjectedDependency] private readonly SignalBus _signalBus = null!;
    private CanvasLayer _canvasLayer = new();

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        base._UnhandledKeyInput(@event);
        
        if (@event.IsActionPressed("ui_cancel"))
            AddControlToUiLayer(GD.Load<PackedScene>("res://Client/Scenes/ConnectToServerScene.tscn").Instantiate<Control>());
    }

    public override void _Ready()
    {
        base._Ready();
        
        AddChild(_canvasLayer);
        _signalBus.DependenciesInjectedSignal += OnDependenciesInjected;
    }

    public void AddControlToUiLayer(Control control)
    {
        _canvasLayer.AddChild(control);
    }

    private void OnDependenciesInjected(ref DependenciesInjectedSignal args)
    {
        AddControlToUiLayer(GD.Load<PackedScene>("res://Client/Scenes/DebugDisplay.tscn").Instantiate<Control>());
    }
}