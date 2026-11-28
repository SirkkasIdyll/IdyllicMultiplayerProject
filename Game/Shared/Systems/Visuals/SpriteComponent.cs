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

    [Export]
    public Texture2D? Texture
    {
        get;
        set
        {
            field = value;
            Sprite3D?.Texture = value;
            if (value != null)
                Sprite3D?.Hframes = value.GetWidth() / 48;
        }
    }
    
    public override void _Ready()
    {
        base._Ready();

        if (Sprite3D == null)
        {
            Sprite3D = new Sprite3D();
            AddChild(Sprite3D);
            Sprite3D.Owner = GetTree().EditedSceneRoot;
            Sprite3D.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
            Sprite3D.PixelSize = 0.08f;
            Sprite3D.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
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