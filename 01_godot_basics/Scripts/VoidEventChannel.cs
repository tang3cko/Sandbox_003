namespace Collector;

using Godot;

[GlobalClass]
public partial class VoidEventChannel : Resource
{
    [Signal]
    public delegate void RaisedEventHandler();

    public void Raise()
    {
        EmitSignal(SignalName.Raised);
    }
}
