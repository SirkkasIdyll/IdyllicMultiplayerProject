using Godot;
using IdyllicMultiplayerProject.Temperance;

namespace IdyllicMultiplayerProject.Shared.Systems.Camera;

[GlobalClass]
public partial class CameraComponent : Component
{
    public Camera3D? Camera3D;

    public override void _EnterTree()
    {
        base._EnterTree();

        if (OS.HasFeature("dedicated_server") || DisplayServer.GetName() == "headless" || Multiplayer.IsServer())
            return;

        if (GetViewport().GetCamera3D() != null)
            return;
        
        Camera3D = new Camera3D();
        AddChild(Camera3D);
        GD.Print("Camera spawned for " + Multiplayer.GetUniqueId());
    }
}