using Godot;
using IdyllicMultiplayerProject.Temperance.NCS;

namespace IdyllicMultiplayerProject.Client.Systems;

public partial class UserInterfaceSystem : NodeSystem
{
    private CanvasLayer _canvasLayer = new();

    public override void _Ready()
    {
        base._Ready();
        
        AddChild(_canvasLayer);
    }

    public void AddControlToScreen(Control control)
    {
        _canvasLayer.AddChild(control);
    }
}