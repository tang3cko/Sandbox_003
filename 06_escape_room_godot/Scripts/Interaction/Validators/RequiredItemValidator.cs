namespace EscapeRoom;

using Godot;

[GlobalClass]
public partial class RequiredItemValidator : InteractionValidator
{
    [Export] public string ItemId { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";

    public override bool Validate(IInteractor interactor, IInteractable target)
    {
        if (string.IsNullOrEmpty(ItemId)) return true;
        var inv = InventorySystem.Instance;
        return inv != null && inv.HasItem(ItemId);
    }

    public override string GetBlockReason()
    {
        var name = string.IsNullOrEmpty(DisplayName) ? ItemId : DisplayName;
        return $"Requires {name}";
    }
}
