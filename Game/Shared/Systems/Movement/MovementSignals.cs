using System;
using Game.Temperance.Signals;
using Godot;

namespace Game.Shared.Systems.Movement;

public class ReceiveMovementInputSignal : UserSignalArgs
{
    public Guid NetGuid;
    public Vector2 Input;

    public ReceiveMovementInputSignal(Guid netGuid, Vector2 input)
    {
        NetGuid = netGuid;
        Input = input;
    }
}