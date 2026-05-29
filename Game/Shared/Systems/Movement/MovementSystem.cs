using System;
using System.Collections.Generic;
using Game.Resources.ProtocolBuffers;
using Godot;
using Game.Temperance.NCS;
using Game.Temperance.Network;
using Game.Temperance.Signals;
using Games.Resources.ProtocolBuffers;

namespace Game.Shared.Systems.Movement;

[GlobalClass]
public partial class MovementSystem : NodeSystem
{
    [InjectedDependency] private readonly ComponentManager _componentManager = null!;
    [InjectedDependency] private readonly NodeManager _nodeManager = null!;
    // [InjectedDependency] private readonly NodeSystemManager _nodeSystemManager = null!;
    [InjectedDependency] private readonly SignalBus _signalBus = null!;

    private Dictionary<Node, Tuple<uint, MovementComponentState>> _movementHistory = new();  
    

    public override void _Ready()
    {
        base._Ready();

        _signalBus.ReceiveMovementInputSignal += OnReceiveMovementInput;
        _signalBus.SendNodeStatesSignal += OnSendNodeStates;
        _signalBus.ReceiveNodeStatesSignal += OnReceiveNodeStates;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        _componentManager.GetNodesWithComponent<MovementComponent>(out var nodes);
        foreach (var node in nodes)
        {
            if (node is not CharacterBody3D characterBody3D)
                continue;
            
            if (!_componentManager.TryGetComponent<MovementComponent>(node, out var movementComponent))
                continue;

            characterBody3D.Velocity = new Vector3
            {
                X = movementComponent.MovementSpeed * movementComponent.InputDirection.X,
                Z = movementComponent.MovementSpeed * movementComponent.InputDirection.Y
            };
            
            if (!characterBody3D.Velocity.IsZeroApprox())
                characterBody3D.MoveAndSlide();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);

        if (Networking.IsServer())
            return;

        if (!@event.IsAction("move_left") && !@event.IsAction("move_right") && !@event.IsAction("move_up") && !@event.IsAction("move_down"))
            return;

        if (!_nodeManager.NetGuidDictionary.TryGetValue(ENetClient.Instance.EnetGuid, out var nodeUpdateInfo))
            return;

        if (!_componentManager.TryGetComponent<MovementComponent>(nodeUpdateInfo.Node, out var movementComponent))
            return;
        
        // Set the movement locally for prediction
        var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        movementComponent.InputDirection = input;
        
        // Send the movement to the server so that it can simulate it on its side
        var userInputMessage = new UserInput { Movement = new GdVector2 { X = input.X, Y = input.Y } };
        ENetClient.Instance.Send(ENetChannels.UserInput, userInputMessage);
    }

    private void OnReceiveMovementInput(Guid netGuid, Vector2 input, ref ReceiveMovementInputSignal signal)
    {
        if (!_nodeManager.NetGuidDictionary.TryGetValue(netGuid, out var nodeUpdateInfo))
            return;

        if (!_componentManager.TryGetComponent<MovementComponent>(nodeUpdateInfo.Node, out var movementComponent))
            return;

        movementComponent.InputDirection = input;
        GD.Print("Updated movement input");
        
    }

    private void OnSendNodeStates(ref SendNodeStatesSignal args)
    {
        foreach (var nodeState in args.Message.NodeState)
        {
            if (!Guid.TryParse(nodeState.NodeNetworkGuid, out var netGuid))
                continue;
            
            if (!_nodeManager.NetGuidDictionary.TryGetValue(netGuid, out var nodeUpdateInfo))
                continue;
            
            var node = nodeUpdateInfo.Node;

            if (!_componentManager.TryGetComponent<MovementComponent>(node, out var movementComponent))
                continue;

            nodeState.GlobalPosition = new GdVector3 { X = node.GlobalPosition.X, Y = node.GlobalPosition.Y, Z = node.GlobalPosition.Z };
            nodeState.GlobalRotation = new GdVector3 { X = node.GlobalRotation.X, Y = node.GlobalRotation.Y, Z = node.GlobalRotation.Z };
            nodeState.GlobalScale = new GdVector3 { X = node.Scale.X, Y = node.Scale.Y, Z = node.Scale.Z };
            nodeState.MovementComponentState = new MovementComponentState
            {
                InputDirection = new GdVector2 {  X = movementComponent.InputDirection.X, Y = movementComponent.InputDirection.Y },
                MovementSpeed = movementComponent.MovementSpeed
            };
        }
    }

    private void OnReceiveNodeStates(ref ReceiveNodeStatesSignal args)
    {
        foreach (var nodeState in args.Message.NodeState)
        {
            if (nodeState.MovementComponentState is null)
                continue;
            
            var movementComponentState =  nodeState.MovementComponentState;
            
            if (!Guid.TryParse(nodeState.NodeNetworkGuid, out var netGuid))
                continue;

            if (!_nodeManager.NetGuidDictionary.TryGetValue(netGuid, out var nodeUpdateInfo))
                continue;

            var node = nodeUpdateInfo.Node;

            if (!_componentManager.TryGetComponent<MovementComponent>(nodeUpdateInfo.Node, out var movementComponent))
                continue;
            
            node.GlobalPosition = new Vector3(nodeState.GlobalPosition.X, nodeState.GlobalPosition.Y, nodeState.GlobalPosition.Z);
            node.GlobalRotation = new Vector3(nodeState.GlobalRotation.X, nodeState.GlobalRotation.Y, nodeState.GlobalRotation.Z);
            node.Scale = new Vector3(nodeState.GlobalScale.X, nodeState.GlobalScale.Y, nodeState.GlobalScale.Z);
            movementComponent.InputDirection = new Vector2(movementComponentState.InputDirection.X, movementComponentState.InputDirection.Y);
            movementComponent.MovementSpeed = movementComponentState.MovementSpeed;
        }
    }
}