namespace Persistence;
using System.Collections.Generic;

public static class SaveDataMigrator
{
    public const int CurrentVersion = 1;

    public const string SaveVersionKey = "save_version";

    public enum MigrationStatus { UpToDate, Migrated, FromFuture, Invalid }

    public static (Dictionary<string, object> Result, MigrationStatus Status) Migrate(
        IReadOnlyDictionary<string, object> input)
    {
        if (input == null)
            return (null, MigrationStatus.Invalid);

        if (!input.TryGetValue(SaveVersionKey, out var versionObj) || versionObj is not int version)
            return (null, MigrationStatus.Invalid);

        if (version > CurrentVersion)
            return (CopyOf(input), MigrationStatus.FromFuture);

        if (version == CurrentVersion)
            return (CopyOf(input), MigrationStatus.UpToDate);

        if (version < 0)
            return (null, MigrationStatus.Invalid);

        var working = CopyOf(input);
        int currentVersion = version;

        while (currentVersion < CurrentVersion)
        {
            switch (currentVersion)
            {
                case 0:
                    MigrateV0ToV1(working);
                    currentVersion = 1;
                    break;
                default:
                    return (null, MigrationStatus.Invalid);
            }
        }

        working[SaveVersionKey] = CurrentVersion;
        return (working, MigrationStatus.Migrated);
    }

    private static void MigrateV0ToV1(Dictionary<string, object> data)
    {
        if (data.TryGetValue("gold", out var goldValue))
        {
            data.Remove("gold");
            data["money"] = goldValue;
        }
    }

    private static Dictionary<string, object> CopyOf(IReadOnlyDictionary<string, object> input)
    {
        var copy = new Dictionary<string, object>(input.Count);
        foreach (var kv in input)
            copy[kv.Key] = kv.Value;
        return copy;
    }
}
