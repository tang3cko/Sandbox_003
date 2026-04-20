namespace Collector;

using Godot;

[GlobalClass]
public partial class IntEventChannel : Resource
{
    [Signal]
    public delegate void RaisedEventHandler(int value);

    public void Raise(int value)
    {
        EmitSignal(SignalName.Raised, value);
    }
}
