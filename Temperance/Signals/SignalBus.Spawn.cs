using System;
using Godot;

namespace IdyllicMultiplayerProject.Temperance.Signals;

public partial class SignalBus
{
    public delegate void RequestSpawnNodeSignalHandler(Guid netGuid, string nodeName);
    public event RequestSpawnNodeSignalHandler? RequestSpawnNodeSignal;
    public void EmitRequestSpawnNodeSignal(Guid netGuid, string nodeName)
    {
        RequestSpawnNodeSignal?.Invoke(netGuid, nodeName);
    }
    
    public delegate void NodeSpawnedSignalHandler(Guid netGuid);
    public event NodeSpawnedSignalHandler? NodeSpawnedSignal;
    public void EmitNodeSpawnedSignal(Guid netGuid)
    {
        NodeSpawnedSignal?.Invoke(netGuid);
    }
}