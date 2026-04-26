namespace EscapeRoom;

using Godot;

[GlobalClass]
public abstract partial class InteractionValidator : Resource
{
    public abstract bool Validate(IInteractor interactor, IInteractable target);
    public virtual string GetBlockReason() => "Blocked";
}
