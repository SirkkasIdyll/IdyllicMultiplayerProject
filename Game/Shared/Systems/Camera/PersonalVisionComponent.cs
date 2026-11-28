using Game.Temperance.EntityComponentSystem;
using Godot;

namespace Game.Shared.Systems.Camera;

/// <summary>
/// Personal vision is the player's bare amount of light so that they can technically navigate in darkness
/// This can be disabled for species that don't have night vision, or just in general if we want pure darkness
/// </summary>
[GlobalClass][Tool]
public partial class PersonalVisionComponent : Component
{
    /// <summary>
    /// Vision cone is the spotlight cone that the player can spin around
    /// </summary>
    [Export]
    public SpotLight3D? VisionCone;

    [Export]
    public float VisionConeAngle
    {
        get;
        set
        {
            field = value;
            VisionCone?.SpotAngle = value;
        }
    } = 75.0f;
    
    [Export]
    public float VisionConeAttenuation
    {
        get;
        set
        {
            field = value;
            VisionCone?.SpotAttenuation = value;
        }
    } = 2.0f;

    [Export]
    public Color VisionConeColor
    {
        get;
        set
        {
            field = value;
            VisionCone?.LightColor = value;
        }
    } = new(1f, 1f, 1f, 1f);
    
    [Export]
    public float VisionConeEnergy
    {
        get;
        set
        {
            field = value;
            VisionCone?.LightEnergy = value;
        }
    } = 1.0f;
    
    [Export]
    public float VisionConeRange
    {
        get;
        set
        {
            field = value;
            VisionCone?.SpotRange = value;
        }
    } = 20.0f;
    
    /// <summary>
    /// Vision circle is the basic light around the player's own body so they're aware of what their body is touching
    /// </summary>
    [Export]
    public OmniLight3D? VisionCircle;

    [Export]
    public float VisionCircleAttenuation
    {
        get;
        set
        {
            field = value;
            VisionCircle?.OmniAttenuation = value;
        }
    } = 2.0f;

    [Export]
    public Color VisionCircleColor
    {
        get;
        set
        {
            field = value;
            VisionCircle?.LightColor = value;
        }
    } = new(1f, 1f, 1f, 1f);
    
    [Export]
    public float VisionCircleEnergy
    {
        get;
        set
        {
            field = value;
            VisionCircle?.LightEnergy = value;
        }
    } = 0.15f;
    
    [Export]
    public float VisionCircleRange
    {
        get;
        set
        {
            field = value;
            VisionCircle?.OmniRange = value;
        }
    } = 5f;

    public override void _Ready()
    {
        base._Ready();

        if (VisionCone == null)
        {
            VisionCone = new SpotLight3D();
            AddChild(VisionCone);
            VisionCone.Owner = GetTree().EditedSceneRoot;
        }

        if (VisionCircle == null)
        {
            VisionCircle = new OmniLight3D();
            AddChild(VisionCircle);
            VisionCircle.Owner = GetTree().EditedSceneRoot;
        }
    }
}