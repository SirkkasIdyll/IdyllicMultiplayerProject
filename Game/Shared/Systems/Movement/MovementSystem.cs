using Game.Shared.Systems.Visuals;
using Godot;
using Game.Temperance.EntityComponentSystem;
using Game.Temperance.Network;
using Game.Temperance.Network.Client;
using Game.Temperance.Signals;

namespace Game.Shared.Systems.Movement;

[GlobalClass]
public partial class MovementSystem : EntitySystem
{
    [InjectedDependency] private readonly ComponentManager _componentManager = null!;
    [InjectedDependency] private readonly EntityManager _entityManager = null!;
    [InjectedDependency] private readonly EntitySystemManager _entitySystemManager = null!;
    [InjectedDependency] private readonly SignalBus _signalBus = null!;
    
    public override void _Ready()
    {
        base._Ready();

    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
    
        // TODO: TRACK INPUT AT SPECIFIC NETWORK TICK FOR CLIENT CONTROLLED NODE
        // TODO: SOMETHING SOMETHING RECONCILE INCORRECT POSITIONS BUT NOT VISUALS
        _componentManager.GetEntitiesWithComponent<MovementComponent>(out var entities);
        foreach (var entity in entities)
        {
            if (!_componentManager.TryGetComponent<MovementComponent>(entity, out var movementComponent))
                continue;

            if (movementComponent.CharacterBody == null)
                continue;

            // Movement should always originate from the entity's position
            var characterBody = movementComponent.CharacterBody;
            characterBody.GlobalPosition = entity.GlobalPosition;
            
            characterBody.Velocity = new Vector3
            {
                X = movementComponent.MovementSpeed * movementComponent.InputDirection.X,
                Z = movementComponent.MovementSpeed * movementComponent.InputDirection.Y
            };

            if (characterBody.Velocity.IsZeroApprox())
                continue;
            
            // After movement, we need to sync the parent entity's position with the character body's
            // since the entity's position is what gets networked
            characterBody.MoveAndSlide();
            entity.GlobalPosition = characterBody.GlobalPosition;
        }
        
        if (Networking.IsServer() || ENetClient.Instance.ClientControlledEntity == null)
            return;
    
        if (!_componentManager.TryGetComponent<MovementComponent>(ENetClient.Instance.ClientControlledEntity,
                out var clientMovementComponent))
            return;
        
        var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        clientMovementComponent.InputDirection = input;
    }
}