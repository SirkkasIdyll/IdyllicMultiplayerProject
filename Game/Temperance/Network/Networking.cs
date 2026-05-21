using System;
using System.Linq;
using ENet;
using Godot;
using IdyllicMultiplayerProject.Temperance.Signals;

namespace IdyllicMultiplayerProject.Temperance.Network;

public static class Networking
{
    public static readonly double PhysicsTickLength = (double)1 / Engine.GetPhysicsTicksPerSecond() * 1000;
    public static readonly TimeSpan PhysicsTickSpan = TimeSpan.FromMilliseconds(PhysicsTickLength);
    
    public static bool IsServer()
    {
        if (OS.GetCmdlineUserArgs().Contains("--server"))
            return true;
        
        if (DisplayServer.GetName() == "headless")
            return true;
        
        if (OS.HasFeature("dedicated_server"))
            return true;

        return false;
    }

    public static void ConnectToServer()
    {
        ENetClient.Instance.ToggleConnection(ENetServer.Ip, ENetServer.Port);
        GRpcClient.Instance.ToggleConnection(GRpcServer.Ip, GRpcServer.Port);
    }
}

public class PeerConnectedSignal : UserSignalArgs
{
    private Event NetEvent;
    
    public PeerConnectedSignal(Event netEvent)
    {
        NetEvent = netEvent;
    }
}

public class PeerDisconnectedSignal : UserSignalArgs
{
    private Event NetEvent;
    
    public PeerDisconnectedSignal(Event netEvent)
    {
        NetEvent = netEvent;
    }
}