using System;
using System.Linq;
using Godot;

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