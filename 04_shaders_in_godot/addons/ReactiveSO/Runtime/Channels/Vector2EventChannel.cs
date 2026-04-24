namespace ReactiveSO;

using Godot;

[GlobalClass]
public partial class Vector2EventChannel : Resource
{
    [Signal]
    public delegate void RaisedEventHandler(Vector2 value);

    public int ListenerCount => GetSignalConnectionList(SignalName.Raised).Count;

    public void Raise(Vector2 value)
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
