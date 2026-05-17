using System;
using Godot;
using Google.Protobuf.Collections;

namespace IdyllicMultiplayerProject.Temperance.Signals;

public partial class SignalBus
{
    public delegate void RequestSpawnNodeSignalHandler(Guid netGuid, string nodeName, RepeatedField<string> components);
    public event RequestSpawnNodeSignalHandler? RequestSpawnNodeSignal;
    public void EmitRequestSpawnNodeSignal(Guid netGuid, string nodeName, RepeatedField<string> components)
    {
        RequestSpawnNodeSignal?.Invoke(netGuid, nodeName, components);
    }
    
    public delegate void NodeSpawnedSignalHandler(Guid netGuid);
    public event NodeSpawnedSignalHandler? NodeSpawnedSignal;
    public void EmitNodeSpawnedSignal(Guid netGuid)
    {
        NodeSpawnedSignal?.Invoke(netGuid);
    }
}