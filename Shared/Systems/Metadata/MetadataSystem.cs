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
        _signalBus.NodeDespawningSignal += OnNodeDespawning;
        _signalBus.ComponentAddedSignal += OnComponentAdded;
        _signalBus.ComponentRemovedSignal += OnComponentRemoved;
    }

    private void OnComponentAdded(Node<Component> node)
    {
        // Add node to list of nodes with specific component type
        if (_componentManager.NodeDictionary.TryGetValue(node.Comp.GetType().Name, out var list))
            list.Add(node);
        else
            _componentManager.NodeDictionary[node.Comp.GetType().Name] = [node];
        
        if (!_componentManager.TryGetComponent<MetadataComponent>(node, out var metadataComponent))
            return;

        metadataComponent.ComponentDictionary.TryAdd(node.Comp.GetType().Name, node.Comp);
    }

    private void OnComponentRemoved(Node<Component> node)
    {
        if (_componentManager.NodeDictionary.TryGetValue(node.Comp.GetType().Name, out var list))
            list.Remove(node.Owner);
        
        if (!_componentManager.TryGetComponent<MetadataComponent>(node, out var metadataComponent))
            return;

        metadataComponent.ComponentDictionary.Remove(node.Comp.GetType().Name);
    }
    
    private void OnNodeSpawned(Guid netGuid, ref NodeSpawnedSignal args)
    {
        if (!_nodeManager.NetGuidDictionary.TryGetValue(netGuid, out var nodeUpdateInfo))
            return;

        var node = nodeUpdateInfo.Node;

        _componentManager.TryGetComponent<MetadataComponent>(node, out var metadataComponent);
        foreach (var child in node.GetChildren())
        {
            if (child is not Component component)
                continue;
            
            metadataComponent?.ComponentDictionary.TryAdd(component.GetType().Name, component);
                
            // Add node to list of nodes with specific component type
            if (_componentManager.NodeDictionary.TryGetValue(component.GetType().Name, out var list))
                list.Add(node);
            else
                _componentManager.NodeDictionary[component.GetType().Name] = [node];
        }
    }

    private void OnNodeDespawning(Node3D node, ref NodeDespawningSignal args)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is not Component component)
                continue;
            
            if (_componentManager.NodeDictionary.TryGetValue(component.GetType().Name, out var list))
                list.Remove(node);
        }
    }
}