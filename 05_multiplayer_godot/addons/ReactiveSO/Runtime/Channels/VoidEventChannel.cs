namespace ReactiveSO;

using Godot;

[GlobalClass]
public partial class VoidEventChannel : Resource
{
    [Signal]
    public delegate void RaisedEventHandler();

    public int ListenerCount => GetSignalConnectionList(SignalName.Raised).Count;

    public void Raise()
    {
        EmitSignal(SignalName.Raised);
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
