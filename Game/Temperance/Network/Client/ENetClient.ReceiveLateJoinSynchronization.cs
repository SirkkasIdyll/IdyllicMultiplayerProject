namespace Game.Temperance.Network.Client;

public partial class ENetClient
{
    public void ProcessLateJoinSynchronizationPacket(ref BufferReader bufferReader)
    {
        // Read server tick
        bufferReader.Read(sizeof(uint) * 8, out var serverTick);
        ReceiveNodeComponentUpdate(ref bufferReader, true);
    }
}