using Game.Temperance.EntityComponentSystem;
using Godot;

namespace Game.Shared.Systems.Visuals;

/// <summary>
/// Maintains a reference to the files used for the sprite
/// </summary>
[GlobalClass][Tool]
public partial class SpriteComponent : Component
{
    [Export]
    public Sprite3D? Sprite3D;
    
    public override void _Ready()
    {
        base._Ready();

        if (Sprite3D == null)
        {
            Sprite3D = new Sprite3D();
            AddChild(Sprite3D);
            Sprite3D.Owner = GetTree().EditedSceneRoot;
        }
    }
}

/// <summary>
/// These are in order of how they get rendered, bottom-most layer to top-most layer
/// </summary>
public enum ClothingSlot
{
    Shoes,
    Bottom,
    Gloves,
    Top,
    Belt,
    Outerwear,
    Bag,
    Neck,
    Inhand,
    Face,
    Hat
}