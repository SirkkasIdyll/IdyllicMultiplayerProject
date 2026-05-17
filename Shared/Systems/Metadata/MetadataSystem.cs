using System;
using Godot;
using IdyllicMultiplayerProject.Temperance.NCS;
using IdyllicMultiplayerProject.Temperance.Signals;

namespace IdyllicMultiplayerProject.Shared.Systems.Metadata;

[GlobalClass]
public partial class MetadataSystem : NodeSystem
{
    [InjectedDependency] private readonly SignalBus _signalBus = null!;
    [InjectedDependency] private readonly ComponentManager _componentManager = null!;
    [InjectedDependency] private readonly NodeManager _nodeManager = null!;
    
    public override void _Ready()
    {
        base._Ready();
        
        _signalBus.NodeSpawnedSignal += OnNodeSpawned;
        _signalBus.ComponentAddedSignal += OnComponentAdded;
        _signalBus.ComponentRemovedSignal += OnComponentRemoved;
    }

    private void OnComponentAdded(Node<Component> node)
    {
        if (!_componentManager.TryGetComponent<MetadataComponent>(node, out var metadataComponent))
            return;

        metadataComponent.ComponentDictionary.TryAdd(node.Comp.GetType().Name, node.Comp);
    }

    private void OnComponentRemoved(Node<Component> node)
    {
        if (!_componentManager.TryGetComponent<MetadataComponent>(node, out var metadataComponent))
            return;

        metadataComponent.ComponentDictionary.Remove(node.Comp.GetType().Name);
    }
    
    private void OnNodeSpawned(Guid netGuid)
    {
        if (!_nodeManager.NetGuidDictionary.TryGetValue(netGuid, out var nodeUpdateInfo))
            return;

        if (!_componentManager.TryGetComponent<MetadataComponent>(nodeUpdateInfo.Node, out var metadataComponent))
            return;

        foreach (var child in nodeUpdateInfo.Node.GetChildren())
        {
            if (child is Component component)
                metadataComponent.ComponentDictionary.TryAdd(child.GetType().Name, component);
        }
    }
}