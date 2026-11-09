using System.Text;
using Game.Temperance.NCS;
using Godot;

namespace Game.Temperance.Network;

public partial class ENetClient
{
    public void ProcessNodeSpawnPacket(ref BufferReader bufferReader)
    {
        // Read server tick
        bufferReader.Read(sizeof(uint) * 8, out var serverTick);
        
        // Read count of nodes to spawn
        bufferReader.Read(10, out var nodesToSpawn);

        if (nodesToSpawn == 0)
            return;
        
        var nodeManager = NodeManager.Instance;
        var componentManager = ComponentManager.Instance;

        for (var i = 0; i < nodesToSpawn; i++)
        {
            // Read how long the name of the node is
            var nodeNameByteCount = bufferReader.ReadInt(0, 32);
            
            // Read the node name
            var nodeName = Encoding.UTF8.GetString(bufferReader.Read(nodeNameByteCount));
            
            // Read the node network ID
            bufferReader.Read(sizeof(ushort) * 8, out var nodeNetworkId);
            
            // Spawn the node now that we have the name and ID
            nodeManager.TrySpawnNode(nodeName, (ushort)nodeNetworkId, out var node3D);

            if (node3D is null)
                continue;
            
            node3D.SetGlobalPosition(bufferReader.ReadVector3(-5000, 5000, 0.01f));
            node3D.SetGlobalRotation(new Vector3(node3D.GetGlobalRotation().X,
                bufferReader.ReadFloat(-2 * Mathf.Pi, 2 * Mathf.Pi, 0.01f), node3D.GlobalRotation.Z));
            
            // If a node is communicated to have a component
            // Add it if it's missing, then retrieve the component
            // and then update the component's details
            for (var j = 0; j < componentManager.ComponentIdToComponentNameDictionary.Count; j++)
            {
                var componentId = (ushort)j;
                var isComponentInPacket = bufferReader.ReadBool();

                if (!isComponentInPacket)
                    continue;

                if (!componentManager.HasComponent(node3D, componentId))
                    componentManager.TryAddComponent(node3D, componentId);
                
                if (!componentManager.TryGetComponent(node3D, componentId, out var component))
                    continue;

                component.ReadReliableChannel(ref bufferReader);
            }
        }
    }
}