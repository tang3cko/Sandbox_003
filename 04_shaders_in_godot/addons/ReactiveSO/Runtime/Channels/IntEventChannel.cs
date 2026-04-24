namespace ReactiveSO;

using Godot;

[GlobalClass]
public partial class IntEventChannel : Resource
{
    [Signal]
    public delegate void RaisedEventHandler(int value);

    public int ListenerCount => GetSignalConnectionList(SignalName.Raised).Count;

    public void Raise(int value)
    {
        EmitSignal(SignalName.Raised, value);
    }

    public void ClearListeners()
    {
        foreach (var connection in GetSignalConnectionList(SignalName.Raised))
        {
            var callable = (Callable)connection["callable"];
            Disconnect(SignalName.Raised, callable);
        }
    }
}
