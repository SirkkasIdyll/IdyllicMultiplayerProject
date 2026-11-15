using Game.Shared.Systems.Camera;
using Godot;

namespace Game.Shared.Systems.Visuals;

/// <summary>
/// Responsible for reconciling the positional difference between a sprite's position
/// and the position of the actual parent physics object which is immediately snapped to corrections
/// </summary>
public partial class VisualSystem
{
    /// <summary>
    /// The components that have a visual element to them,
    /// and need to have their transforms separated from the parent node's physics body
    /// </summary>
    private string[] _visualComponentNames =
    [
        "CameraComponent",
        "PersonalVisionComponent",
        "SpriteComponent"
    ];
    
    /// <summary>
    /// Setting top level on a node's transform separates its own position from its parent
    /// Visual components should be slowly lerped to the actual position of the parent
    /// Rather than immediately, and jarringly, jumping to the position communicated by the server
    /// </summary>
    private void SetTopLevelForVisualComponents(Node node)
    {
        foreach (var componentName in _visualComponentNames)
        {
            if (!_componentManager.ComponentNameToComponentIdDictionary.TryGetValue(componentName, out var componentId))
                continue;
            
            if (!_componentManager.TryGetComponent(node, componentId, out var component))
                continue;

            component.TopLevel = true;
            GD.Print("Set top level for " + componentName);
        }
    }
    
    public void LerpVisualComponents(Node node)
    {
        if (node is not Node3D node3D)
            return;
        
        foreach (var componentName in _visualComponentNames)
        {
            if (!_componentManager.ComponentNameToComponentIdDictionary.TryGetValue(componentName, out var componentId))
                continue;

            if (!_componentManager.TryGetComponent(node, componentId, out var component))
                continue;
            
            var startingPosition = new Vector2(component.GlobalPosition.X, component.GlobalPosition.Z);
            var targetPosition = new Vector2(node3D.GlobalPosition.X, node3D.GlobalPosition.Z);
            GD.Print( componentName + " Starting position:" + startingPosition + " Target position:" + targetPosition);
            var (newPositionX, newPositionZ) = startingPosition.DistanceTo(targetPosition) < 1f
                ? startingPosition.Lerp(targetPosition, 0.95f)
                : startingPosition.Lerp(targetPosition, 0.85f);
            
            component.SetGlobalPosition(new Vector3(newPositionX, component.GlobalPosition.Y, newPositionZ));
        }
    }
}