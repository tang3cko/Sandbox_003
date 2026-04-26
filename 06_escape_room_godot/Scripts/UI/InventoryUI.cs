namespace EscapeRoom;

using Godot;

[GlobalClass]
public partial class InventoryUI : Control
{
    [Export] public Vector2I SlotSize { get; set; } = new Vector2I(64, 64);
    [Export] private NodePath _slotContainerPath = new();

    private Container _slotContainer;

    public override void _Ready()
    {
        if (_slotContainerPath == null || _slotContainerPath.IsEmpty)
        {
            GD.PushError($"{nameof(InventoryUI)}: '_slotContainerPath' is empty.");
            return;
        }
        _slotContainer = GetNodeOrNull<Container>(_slotContainerPath);
        if (_slotContainer == null)
        {
            GD.PushError($"{nameof(InventoryUI)}: '_slotContainerPath' could not be resolved as Container.");
            return;
        }

        var inventory = InventorySystem.Instance;
        if (inventory != null)
        {
            inventory.ItemAdded += OnItemChanged;
            inventory.ItemRemoved += OnItemChanged;
        }
        else
        {
            GD.PushWarning($"{nameof(InventoryUI)}: InventorySystem autoload missing at _Ready.");
        }

        Refresh();
    }

    public override void _ExitTree()
    {
        var inventory = InventorySystem.Instance;
        if (inventory != null)
        {
            inventory.ItemAdded -= OnItemChanged;
            inventory.ItemRemoved -= OnItemChanged;
        }
    }

    private void OnItemChanged(InventoryItem _) => Refresh();

    private void Refresh()
    {
        if (_slotContainer == null) return;

        foreach (var child in _slotContainer.GetChildren())
            child.QueueFree();

        var inventory = InventorySystem.Instance;
        if (inventory == null) return;

        foreach (var item in inventory.Items)
        {
            if (item == null) continue;
            _slotContainer.AddChild(BuildSlot(item));
        }
    }

    private Control BuildSlot(InventoryItem item)
    {
        var slot = new PanelContainer
        {
            CustomMinimumSize = SlotSize,
            TooltipText = string.IsNullOrEmpty(item.Description) ? item.DisplayName : item.Description,
        };

        if (item.Icon != null)
        {
            slot.AddChild(new TextureRect
            {
                Texture = item.Icon,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            });
        }
        else
        {
            slot.AddChild(new Label
            {
                Text = item.DisplayName,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            });
        }

        return slot;
    }
}
