namespace Persistence;
using System;
using System.Collections.Generic;

public sealed class SaveData
{
    public int SaveVersion { get; set; } = SaveDataMigrator.CurrentVersion;
    public double SavedAtUnix { get; set; }
    public long SaveSequence { get; set; }
    public int Money { get; set; }
    public int Day { get; set; } = 1;
    public Dictionary<string, int> InventoryCounts { get; set; } = new();
}
