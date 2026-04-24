namespace ReactiveSO;

using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class Node3DRuntimeSet : Resource
{
    [Export] private VoidEventChannel _onItemsChanged;
    [Export] private IntEventChannel _onCountChanged;

    private readonly List<Node3D> _items = new();

    [Signal]
    public delegate void ItemsChangedEventHandler();

    public IReadOnlyList<Node3D> Items => _items;
    public int Count => _items.Count;
    public VoidEventChannel OnItemsChanged => _onItemsChanged;
    public IntEventChannel OnCountChanged => _onCountChanged;

    public void Add(Node3D item)
    {
        if (item == null || _items.Contains(item)) return;
        _items.Add(item);
        NotifyChanged();
    }

    public bool Remove(Node3D item)
    {
        if (!_items.Remove(item)) return false;
        NotifyChanged();
        return true;
    }

    public bool Contains(Node3D item)
    {
        return _items.Contains(item);
    }

    public void Clear()
    {
        if (_items.Count == 0) return;
        _items.Clear();
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        EmitSignal(SignalName.ItemsChanged);
        _onItemsChanged?.Raise();
        _onCountChanged?.Raise(_items.Count);
    }
}
