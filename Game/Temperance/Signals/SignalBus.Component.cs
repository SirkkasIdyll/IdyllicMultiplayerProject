using IdyllicMultiplayerProject.Temperance.NCS;

namespace IdyllicMultiplayerProject.Temperance.Signals;

public partial class SignalBus
{
    public delegate void ComponentAddedSignalHandler(Node<Component> node);
    public event ComponentAddedSignalHandler? ComponentAddedSignal;
    public void EmitComponentAddedSignal(Node<Component> node)
    {
        ComponentAddedSignal?.Invoke(node);
    }
    
    public delegate void ComponentRemovedSignalHandler(Node<Component> node);
    public event ComponentRemovedSignalHandler? ComponentRemovedSignal;
    public void EmitComponentRemovedSignal(Node<Component> node)
    {
        ComponentRemovedSignal?.Invoke(node);
    }
}