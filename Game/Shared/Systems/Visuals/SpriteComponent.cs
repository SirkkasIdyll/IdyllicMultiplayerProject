using Game.Temperance.EntityComponentSystem;
using Godot;

namespace Game.Shared.Systems.Visuals;

/// <summary>
/// Maintains a reference to the files used for the sprite
/// </summary>
[GlobalClass]
public partial class SpriteComponent : Component
{
    [Export]
    public Sprite3D? Sprite3D;
}
