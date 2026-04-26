namespace EscapeRoom;

using Godot;

[GlobalClass]
public partial class InteractableComponent : Area3D, IInteractable
{
    [Signal] public delegate void FocusEnteredEventHandler(Node interactor);
    [Signal] public delegate void FocusExitedEventHandler(Node interactor);
    [Signal] public delegate void InteractedEventHandler(Node interactor);

    [Export] public string Verb { get; set; } = "Use";
    [Export] public string TargetName { get; set; } = "";
    [Export] public bool Enabled { get; set; } = true;
    [Export] public int InteractionPriority { get; set; } = 0;
    [Export] public bool RequiresHold { get; set; } = false;
    [Export] public float HoldDuration { get; set; } = 0.6f;
    [Export] public InteractionValidator[] Validators { get; set; }

    public virtual bool CanBeInteractedBy(IInteractor interactor)
    {
        if (!Enabled) return false;
        return FailingValidator(interactor) == null;
    }

    public virtual InteractionContext BuildContext(IInteractor interactor, string glyph, float holdProgress)
    {
        var ctx = new InteractionContext
        {
            Glyph = glyph,
            RequiresHold = RequiresHold,
            HoldProgress = holdProgress,
        };

        if (!Enabled)
        {
            ctx.BlockReason = "Disabled";
            return ctx;
        }

        var failing = FailingValidator(interactor);
        if (failing != null)
        {
            ctx.BlockReason = failing.GetBlockReason();
            return ctx;
        }

        ctx.Verb = Verb;
        ctx.TargetName = TargetName;
        return ctx;
    }

    public virtual void OnFocusEnter(IInteractor interactor)
    {
        EmitSignal(SignalName.FocusEntered, interactor?.Body);
    }

    public virtual void OnFocusExit(IInteractor interactor)
    {
        EmitSignal(SignalName.FocusExited, interactor?.Body);
    }

    public InteractionResult Interact(IInteractor interactor)
    {
        if (!CanBeInteractedBy(interactor)) return InteractionResult.Rejected;
        var result = OnInteract(interactor);
        if (result == InteractionResult.Success)
            EmitSignal(SignalName.Interacted, interactor?.Body);
        return result;
    }

    protected virtual InteractionResult OnInteract(IInteractor interactor) => InteractionResult.Success;

    private InteractionValidator FailingValidator(IInteractor interactor)
    {
        if (Validators == null) return null;
        for (int i = 0; i < Validators.Length; i++)
        {
            var v = Validators[i];
            if (v != null && !v.Validate(interactor, this))
                return v;
        }
        return null;
    }
}
