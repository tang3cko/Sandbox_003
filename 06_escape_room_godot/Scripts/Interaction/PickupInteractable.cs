namespace EscapeRoom;

using Godot;

[GlobalClass]
public partial class PickupInteractable : InteractableComponent
{
    [Export] public InventoryItem Item { get; set; }

    public override void _Ready()
    {
        if (Item != null)
        {
            if (string.IsNullOrEmpty(Verb) || Verb == "Use") Verb = "Take";
            if (string.IsNullOrEmpty(TargetName)) TargetName = Item.DisplayName;
        }
    }

    public override InteractionContext BuildContext(IInteractor interactor, string glyph, float holdProgress)
    {
        var ctx = base.BuildContext(interactor, glyph, holdProgress);
        if (!ctx.IsBlocked && Item?.Icon != null)
            ctx.Icon = Item.Icon;
        return ctx;
    }

    protected override InteractionResult OnInteract(IInteractor interactor)
    {
        if (Item == null)
        {
            GD.PushError($"{nameof(PickupInteractable)} '{Name}': Item is null.");
            return InteractionResult.Rejected;
        }
        var inv = InventorySystem.Instance;
        if (inv == null)
        {
            GD.PushError($"{nameof(PickupInteractable)}: InventorySystem autoload missing.");
            return InteractionResult.Rejected;
        }
        if (!inv.AddItem(Item)) return InteractionResult.Rejected;

        Enabled = false;
        QueueFree();
        return InteractionResult.Success;
    }
}
