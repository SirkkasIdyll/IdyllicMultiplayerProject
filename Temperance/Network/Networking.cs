using System.Linq;
using Godot;

namespace IdyllicMultiplayerProject.Temperance.Network;

public static class Networking
{
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
}