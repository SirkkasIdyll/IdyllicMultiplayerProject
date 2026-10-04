using Game.Temperance.NCS;
using Godot;

namespace Game.Shared.Systems.Camera;

/// <summary>
/// Personal vision is the player's bare amount of light so that they can technically navigate in darkness
/// This can be disabled for species that don't have night vision, or just in general if we want pure darkness
/// </summary>
[GlobalClass]
public partial class PersonalVisionComponent : Component
{
    /// <summary>
    /// Vision cone is the spotlight cone that the player can spin around
    /// </summary>
    [Export]
    public SpotLight3D? VisionCone;

    [Export]
    public float VisionConeAngle = 55.0f;
    
    [Export]
    public float VisionConeAttenuation = 2.0f;

    [Export]
    public Color VisionConeColor = new Color(1f, 1f, 1f, 1f);
    
    [Export]
    public float VisionConeEnergy = 1.0f;
    
    [Export]
    public float VisionConeRange = 20.0f;
    
    /// <summary>
    /// Vision circle is the basic light around the player's own body so they're aware of what their body is touching
    /// </summary>
    [Export]
    public OmniLight3D? VisionCircle;

    [Export]
    public float VisionCircleAttenuation = 2.0f;

    [Export]
    public Color VisionCircleColor = new Color(1f, 1f, 1f, 1f);
    
    [Export]
    public float VisionCircleEnergy = 0.15f;
    
    [Export]
    public float VisionCircleRange = 5f;
}