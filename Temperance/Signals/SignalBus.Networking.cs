using ENet;

namespace IdyllicMultiplayerProject.Temperance.Signals;

public partial class SignalBus
{
    public delegate void PeerConnectedSignalHandler(Event netEvent);
    public event PeerConnectedSignalHandler? PeerConnectedSignal;
    public void EmitPeerConnectedSignal(Event netEvent)
    {
        PeerConnectedSignal?.Invoke(netEvent);
    }

    public delegate void PeerDisconnectedSignalHandler(Event netEvent);
    public event PeerDisconnectedSignalHandler? PeerDisconnectedSignal;
    public void EmitPeerDisconnectedSignal(Event netEvent)
    {
        PeerDisconnectedSignal?.Invoke(netEvent);
    }
}