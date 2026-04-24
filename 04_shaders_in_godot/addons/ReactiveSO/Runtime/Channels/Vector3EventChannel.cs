namespace ReactiveSO;

using Godot;

[GlobalClass]
public partial class Vector3EventChannel : Resource
{
    [Signal]
    public delegate void RaisedEventHandler(Vector3 value);

    public int ListenerCount => GetSignalConnectionList(SignalName.Raised).Count;

    public void Raise(Vector3 value)
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
