namespace ReactiveSO;

using Godot;

[GlobalClass]
public partial class FloatEventChannel : Resource
{
    [Signal]
    public delegate void RaisedEventHandler(float value);

    public int ListenerCount => GetSignalConnectionList(SignalName.Raised).Count;

    public void Raise(float value)
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
