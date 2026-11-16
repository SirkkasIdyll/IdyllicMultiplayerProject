using Godot;
using Game.Temperance.NCS;

namespace Game.Shared.Systems.Camera;

[GlobalClass]
public partial class CameraComponent : Component
{
    [Export]
    public Camera3D? Camera;

    [Export]
    public float CameraSize = 20f;

    [Export][SynchronizedField(Min = -2 * Mathf.Pi, Max = 2 * Mathf.Pi, Precision = 0.01f)]
    public float AngleOfCameraPositionFromDefaultPosition;
}