namespace Persistence;
using System.Collections.Generic;

public static class InventoryStack
{
    public static IReadOnlyDictionary<string, int> Add(
        IReadOnlyDictionary<string, int> source, string itemId, int amount)
    {
        if (source == null) source = new Dictionary<string, int>();
        if (string.IsNullOrEmpty(itemId)) return Copy(source);
        if (amount <= 0) return Copy(source);

        var next = Copy(source);
        next.TryGetValue(itemId, out int current);
        next[itemId] = current + amount;
        return next;
    }

    public static IReadOnlyDictionary<string, int> Remove(
        IReadOnlyDictionary<string, int> source, string itemId, int amount)
    {
        if (source == null) source = new Dictionary<string, int>();
        if (string.IsNullOrEmpty(itemId)) return Copy(source);
        if (amount <= 0) return Copy(source);

        var next = Copy(source);
        if (!next.TryGetValue(itemId, out int current)) return next;

        int updated = current - amount;
        if (updated <= 0)
            next.Remove(itemId);
        else
            next[itemId] = updated;
        return next;
    }

    public static int GetCount(IReadOnlyDictionary<string, int> source, string itemId)
    {
        if (source == null) return 0;
        if (string.IsNullOrEmpty(itemId)) return 0;
        return source.TryGetValue(itemId, out int count) ? count : 0;
    }

    private static Dictionary<string, int> Copy(IReadOnlyDictionary<string, int> source)
    {
        var dict = new Dictionary<string, int>(source.Count);
        foreach (var kv in source)
            dict[kv.Key] = kv.Value;
        return dict;
    }
}
