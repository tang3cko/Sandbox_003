namespace EscapeRoom;

using Godot;

[GlobalClass]
public partial class InteractionContextEventChannel : Resource
{
    [Signal] public delegate void RaisedEventHandler(InteractionContext context);

    public void Raise(InteractionContext context)
    {
        EmitSignal(SignalName.Raised, context);
    }
}
