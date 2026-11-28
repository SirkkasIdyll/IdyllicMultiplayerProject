using Godot;
using Game.Temperance.EntityComponentSystem;

namespace Game.Shared.Systems.Camera;

[GlobalClass][Tool]
public partial class CameraComponent : Component
{
    [Export]
    public Camera3D? Camera;

    /// <summary>
    /// Camera is rotated face down on the Y axis so we get a top-down view
    /// </summary>
    [Export]
    public Quaternion CameraRotation
    {
        get;
        set
        {
            field = value;
            Camera?.Quaternion = value;
        }
    } = new(-0.707107f, 0f, 0f, 0.707107f);

    /// <summary>
    /// Camera size controls 
    /// </summary>
    [Export]
    public float CameraSize
    {
        get;
        set
        {
            field = value;
            Camera?.Size = value;
        }
    } = 20f;

    [Export]
    public float CameraHeight
    {
        get;
        set
        {
            field = value;
            Camera?.GlobalPosition = new Vector3(Camera.GlobalPosition.X, value, Camera.GlobalPosition.Z);
        }
    } = 20f;

    /// <summary>
    /// The normalized vector that indicates where the mouse is on the screen
    /// Take the angle of this and use it to set the rotation the entity is facing towards
    /// </summary>
    [SynchronizedField(Min = -1, Max = 1, Precision = 0.01f)]
    public Vector2 CameraAimInput;
    
    public override void _Ready()
    {
        base._Ready();

        // Adds the camera to the editor tree scene
        if (Camera == null)
        {
            Camera = new Camera3D();
            AddChild(Camera);
            Camera.Owner = GetTree().EditedSceneRoot;
        }

        Camera.Projection = Camera3D.ProjectionType.Orthogonal;
    }
}