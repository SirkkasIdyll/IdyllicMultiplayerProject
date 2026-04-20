namespace IdyllicMultiplayerProject.Temperance.Signals;

/// <summary>
/// Signals are what Godot calls messages, events that get triggered and can be subscribed to
/// 
/// The SignalBus acts as a C# message bus for custom user-created signals,
/// to serve as a container for the event - delegate - eventhandler implementation pattern,
/// and to subscribe/unsubscribe from these custom user-created signals to prevent a memory leak
/// </summary>
public partial class SignalBus
{
    public static SignalBus Instance { get; } = new();
    private SignalBus() { }
}

/// <summary>
/// I'm implementing a custom user signals class because I hate how clunky it is having to use
/// Godot's Signal attribute/EventHandler stuff. It's difficult having to connect each node in the editor and
/// then connecting each node to the exact signals coming from each node.
/// <see cref="Signals.SignalBus"/> to the rescue!
/// </summary>
public abstract partial class UserSignalArgs
{
    // public string SignalName => GetType().Name;
}

public abstract partial class HandledSignalArgs : UserSignalArgs
{
    /// <summary>
    /// If the signal is marked as handled, don't process the signal by any other systems
    /// </summary>
    public bool Handled;
}

public abstract partial class CancellableSignalArgs : UserSignalArgs
{
    /// <summary>
    /// If the signal is marked as Canceled, don't process the signal by any other systems
    /// </summary>
    public bool Canceled;
}