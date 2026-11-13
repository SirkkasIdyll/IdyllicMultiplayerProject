namespace Game.Temperance.Network.Client;

public partial class ENetClient
{
    public void ProcessChangeControlledNode(ref BufferReader bufferReader)
    {
        // Read server tick
        bufferReader.Read(sizeof(uint) * 8, out var serverTick);
        
        // Read the node network ID
        bufferReader.Read(sizeof(ushort) * 8, out var nodeNetworkId);

        if (!_nodeManager.NetworkIdNodeDictionary.TryGetValue((ushort)nodeNetworkId, out var node))
            return;

        ClientControlledNode = node;
    }
}