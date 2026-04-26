namespace EscapeRoom;

using System.Collections.Generic;
using Godot;

public partial class InventorySystem : Node
{
    public static InventorySystem Instance { get; private set; }

    [Signal] public delegate void ItemAddedEventHandler(InventoryItem item);
    [Signal] public delegate void ItemRemovedEventHandler(InventoryItem item);

    private readonly List<InventoryItem> _items = new();

    public IReadOnlyList<InventoryItem> Items => _items;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public bool AddItem(InventoryItem item)
    {
        if (item == null || _items.Contains(item)) return false;
        _items.Add(item);
        EmitSignal(SignalName.ItemAdded, item);
        return true;
    }

    public bool RemoveItem(InventoryItem item)
    {
        if (item == null || !_items.Remove(item)) return false;
        EmitSignal(SignalName.ItemRemoved, item);
        return true;
    }

    public bool RemoveItemById(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        var index = _items.FindIndex(i => i != null && i.ItemId == itemId);
        if (index < 0) return false;
        var removed = _items[index];
        _items.RemoveAt(index);
        EmitSignal(SignalName.ItemRemoved, removed);
        return true;
    }

    public bool HasItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        return _items.Exists(i => i != null && i.ItemId == itemId);
    }
}
