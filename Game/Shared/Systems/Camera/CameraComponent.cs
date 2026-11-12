using Godot;
using Game.Temperance.NCS;

namespace Game.Shared.Systems.Camera;

[GlobalClass]
public partial class CameraComponent : Component
{
    [Export]
    public Camera3D? Camera;
}