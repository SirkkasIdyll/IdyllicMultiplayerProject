using Godot;
using IdyllicMultiplayerProject.Temperance.NCS;

namespace IdyllicMultiplayerProject.Client.Systems;

[GlobalClass]
public partial class UserInterfaceSystem : NodeSystem
{
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
        AddControlToUiLayer(GD.Load<PackedScene>("res://Client/Scenes/DebugDisplay.tscn").Instantiate<Control>());
    }

    public void AddControlToUiLayer(Control control)
    {
        _canvasLayer.AddChild(control);
    }
}