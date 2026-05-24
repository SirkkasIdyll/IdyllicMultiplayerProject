using Godot;
using Game.Temperance.NCS;
using Game.Temperance.Network;

namespace Game.Shared.Systems.Camera;

[GlobalClass]
public partial class CameraComponent : Component
{
    public Camera3D? Camera3D;

    public override void _EnterTree()
    {
        base._EnterTree();

        if (Networking.IsServer())
            return;

        if (GetViewport().GetCamera3D() != null)
            return;
        
        Camera3D = new Camera3D();
        AddChild(Camera3D);
        GD.Print("Camera spawned for " + Multiplayer.GetUniqueId());
    }
}