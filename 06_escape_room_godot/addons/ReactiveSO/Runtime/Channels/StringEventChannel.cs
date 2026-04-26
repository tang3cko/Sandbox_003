namespace ReactiveSO;

using Godot;

[GlobalClass]
public partial class StringEventChannel : Resource
{
    [Signal]
    public delegate void RaisedEventHandler(string value);

    public int ListenerCount => GetSignalConnectionList(SignalName.Raised).Count;

    public void Raise(string value)
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
