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

        // _signalBus.ReceiveMovementInputSignal += OnReceiveMovementInput;
    }

    // public override void _PhysicsProcess(double delta)
    // {
    //     base._PhysicsProcess(delta);
    //
    //     // TODO: TRACK INPUT AT SPECIFIC NETWORK TICK FOR CLIENT CONTROLLED NODE
    //     // TODO: SOMETHING SOMETHING RECONCILE INCORRECT POSITIONS BUT NOT VISUALS
    //     _componentManager.GetEntitiesWithComponent<MovementComponent>(out var entities);
    //     foreach (var entity in entities)
    //     {
    //         if (entity is not CharacterBody3D characterBody3D)
    //             continue;
    //         
    //         if (!_componentManager.TryGetComponent<MovementComponent>(entity, out var movementComponent))
    //             continue;
    //         
    //         characterBody3D.Velocity = new Vector3
    //         {
    //             X = movementComponent.MovementSpeed * movementComponent.InputDirection.X,
    //             Z = movementComponent.MovementSpeed * movementComponent.InputDirection.Y
    //         };
    //         
    //         if (!characterBody3D.Velocity.IsZeroApprox())
    //             characterBody3D.MoveAndSlide();
    //     }
    //     
    //     if (Networking.IsServer() || ENetClient.Instance.ClientControlledEntity == null)
    //         return;
    //
    //     if (!_componentManager.TryGetComponent<MovementComponent>(ENetClient.Instance.ClientControlledEntity,
    //             out var movementComponent2))
    //         return;
    //     
    //     var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
    //     movementComponent2.InputDirection = input;
    // }

    // private void OnReceiveMovementInput(Guid netGuid, Vector2 input, ref ReceiveMovementInputSignal signal)
    // {
        // if (!_entityManager.NetGuidDictionary.TryGetValue(netGuid, out var entityUpdateInfo))
        //     return;
        //
        // if (!_componentManager.TryGetComponent<MovementComponent>(entityUpdateInfo.Node, out var movementComponent))
        //     return;
        //
        // movementComponent.InputDirection = input;
        // GD.Print("Updated movement input");
    // }

    // private void OnSendEntityStates(ref SendEntityStatesSignal args)
    // {
    //     foreach (var entityState in args.Message.NodeState)
    //     {
    //         if (!Guid.TryParse(entityState.EntityNetworkGuid, out var netGuid))
    //             continue;
    //         
    //         if (!_entityManager.NetGuidDictionary.TryGetValue(netGuid, out var entityUpdateInfo))
    //             continue;
    //         
    //         var entity = nodeUpdateInfo.Node;
    //
    //         if (!_componentManager.TryGetComponent<MovementComponent>(entity, out var movementComponent))
    //             continue;
    //
    //         nodeState.Transform = new GdTransform3
    //         {
    //             Basis = new GdBasis3
    //             {
    //                 X = new GdVector3 { X = entity.Transform.Basis.X.X, Y = entity.Transform.Basis.X.Y, Z = entity.Transform.Basis.X.Z },
    //                 Y = new GdVector3 { X = entity.Transform.Basis.Y.X, Y = entity.Transform.Basis.Y.Y, Z = entity.Transform.Basis.Y.Z },
    //                 Z = new GdVector3 { X = entity.Transform.Basis.Z.X, Y = entity.Transform.Basis.Z.Y, Z = entity.Transform.Basis.Z.Z }
    //             },
    //             Origin = new GdVector3 { X = entity.Transform.Origin.X, Y = entity.Transform.Origin.Y, Z = entity.Transform.Origin.Z }
    //         };
    //
    //         nodeState.MovementComponentState = new MovementComponentState
    //         {
    //             InputDirection = new GdVector2 {  X = movementComponent.InputDirection.X, Y = movementComponent.InputDirection.Y },
    //             MovementSpeed = movementComponent.MovementSpeed
    //         };
    //     }
    // }
    //
    // private void OnReceiveEntityStates(ref ReceiveEntityStatesSignal args)
    // {
    //     foreach (var entityState in args.Message.NodeState)
    //     {
    //         if (entityState.MovementComponentState is null)
    //             continue;
    //         
    //         var movementComponentState =  nodeState.MovementComponentState;
    //         
    //         if (!Guid.TryParse(entityState.EntityNetworkGuid, out var netGuid))
    //             continue;
    //
    //         if (!_entityManager.NetGuidDictionary.TryGetValue(netGuid, out var entityUpdateInfo))
    //             continue;
    //
    //         var entity = nodeUpdateInfo.Node;
    //
    //         if (!_componentManager.TryGetComponent<MovementComponent>(entityUpdateInfo.Node, out var movementComponent))
    //             continue;
    //
    //         var targetTransform3D = new Transform3D
    //         {
    //             Basis = new Basis(
    //                 new Vector3(entityState.Transform.Basis.X.X, nodeState.Transform.Basis.X.Y,
    //                     nodeState.Transform.Basis.X.Z),
    //                 new Vector3(entityState.Transform.Basis.Y.X, nodeState.Transform.Basis.Y.Y,
    //                     nodeState.Transform.Basis.Y.Z),
    //                 new Vector3(entityState.Transform.Basis.Z.X, nodeState.Transform.Basis.Z.Y,
    //                     nodeState.Transform.Basis.Z.Z)
    //             ),
    //             Origin = new Vector3(
    //                 nodeState.Transform.Origin.X,
    //                 nodeState.Transform.Origin.Y,
    //                 nodeState.Transform.Origin.Z
    //             )
    //         };
    //         
    //         var distance = entity.Transform.Origin.DistanceTo(targetTransform3D.Origin);
    //         if (distance != 0f)
    //         {
    //             // This was originally a frame-rate independent interpolation example given by the Godot docs
    //             // Delta is generally around 30 ms
    //             // var udelta = nodeState.Sequence - nodeUpdateInfo.CurrSequence;
    //             // var speed = 90f;
    //             // var interpolationSpeed = 1f - MathF.Exp(-speed * ((float)udelta / 1000));
    //             // GD.Print("For delta: " + fdelta + " interpolation speed is " + interpolationSpeed);
    //             entity.SetTransform(entity.Transform.InterpolateWith(targetTransform3D, .60f));
    //         }
    //         
    //         movementComponent.InputDirection = new Vector2(movementComponentState.InputDirection.X, movementComponentState.InputDirection.Y);
    //         movementComponent.MovementSpeed = movementComponentState.MovementSpeed;
    //     }
    // }
}