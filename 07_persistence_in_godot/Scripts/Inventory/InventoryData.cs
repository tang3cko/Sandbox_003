namespace Persistence;

using System.Collections.Generic;

// Mutable-state wrapper around the pure InventoryStack helper.
// Owns the canonical Dictionary<string, int> for runtime use,
// delegating actual transitions to the pure layer.
//
// Add/Remove return bool to indicate whether the operation actually
// changed state (so the caller can decide whether to refresh UI / log).
public sealed class InventoryData
{
    private Dictionary<string, int> _counts = new();

    public IReadOnlyDictionary<string, int> Counts => _counts;

    public InventoryData() { }

    public InventoryData(IReadOnlyDictionary<string, int> initial)
    {
        if (initial == null) return;
        foreach (var kv in initial)
            _counts[kv.Key] = kv.Value;
    }

    public bool Add(string itemId, int count = 1)
    {
        if (string.IsNullOrEmpty(itemId) || count <= 0) return false;
        var next = InventoryStack.Add(_counts, itemId, count);
        if (DictsEqual(next, _counts)) return false;
        _counts = ToDict(next);
        return true;
    }

    public bool Remove(string itemId, int count = 1)
    {
        if (string.IsNullOrEmpty(itemId) || count <= 0) return false;
        var next = InventoryStack.Remove(_counts, itemId, count);
        if (DictsEqual(next, _counts)) return false;
        _counts = ToDict(next);
        return true;
    }

    public int GetCount(string itemId) => InventoryStack.GetCount(_counts, itemId);

    public void ReplaceAll(IReadOnlyDictionary<string, int> snapshot)
    {
        _counts = snapshot == null
            ? new Dictionary<string, int>()
            : new Dictionary<string, int>(snapshot);
    }

    public void Clear() => _counts = new Dictionary<string, int>();

    private static Dictionary<string, int> ToDict(IReadOnlyDictionary<string, int> source)
    {
        var dict = new Dictionary<string, int>(source.Count);
        foreach (var kv in source)
            dict[kv.Key] = kv.Value;
        return dict;
    }

    private static bool DictsEqual(IReadOnlyDictionary<string, int> a, IReadOnlyDictionary<string, int> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var kv in a)
        {
            if (!b.TryGetValue(kv.Key, out var bv) || bv != kv.Value) return false;
        }
        return true;
    }
}
