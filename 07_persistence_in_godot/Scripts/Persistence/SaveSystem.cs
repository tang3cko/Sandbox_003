namespace Persistence;

using Godot;
using System.Collections.Generic;
using System.IO;

// Godot-dependent save system.
//
// Layout (atomic write + 2-generation backup rotation):
//   user://save.dat       <- main (loaded first)
//   user://save.dat.tmp   <- write-target before rename
//   user://save.dat.bak0  <- previous main
//   user://save.dat.bak1  <- one before that
//
// Save flow:
//   1. SaveData -> Dictionary<string, object>
//   2. Serialize to MemoryStream (header + payload + CRC32)
//   3. Open temp via FileAccess.Open(Write), StoreBuffer, Close
//   4. Rotate: bak1 deleted, bak0 -> bak1, main -> bak0
//   5. Rename temp -> main
//
// Load flow (3-stage fallback, fail-soft):
//   main -> bak0 -> bak1 -> default (caller substitutes new SaveData())
//
// Notes:
//   - Encryption intentionally omitted (indie consensus 2024-2026).
//   - OS.SetUseFileAccessSaveAndSwap does not exist on Godot 4.6 .NET; rolling our own.
//   - Godot Issue #64575: ModeFlags.ReadWrite truncates on open in Godot 4.x; we never use it.
//     We always open Write or Read separately.
public sealed class SaveSystem
{
    public const string SavePath    = "user://save.dat";
    public const string TempPath    = "user://save.dat.tmp";
    public const string Backup0Path = "user://save.dat.bak0";
    public const string Backup1Path = "user://save.dat.bak1";

    // Dictionary keys (stable, must match SaveDataMigrator expectations).
    private const string KeySaveVersion = "save_version";
    private const string KeySavedAt     = "saved_at";
    private const string KeyMoney       = "money";
    private const string KeyDay         = "day";
    private const string KeyInventory   = "inventory";

    public enum SaveError
    {
        Ok,
        OpenFailed,
        WriteFailed,
        RenameFailed,
    }

    public enum LoadError
    {
        Ok,
        NoSaveFound,
        AllSourcesCorrupt,
    }

    public enum LoadSource
    {
        Primary,
        Backup0,
        Backup1,
        Default,
    }

    private readonly SaveSerializer _serializer = new();

    public SaveError Save(SaveData data)
    {
        if (data == null)
        {
            GD.PushError("SaveSystem.Save: data is null");
            return SaveError.WriteFailed;
        }

        var dict = ToDict(data);

        // 1. Serialize to memory first so we never write a partial header on disk.
        byte[] bytes;
        try
        {
            using var ms = new MemoryStream();
            _serializer.Serialize(ms, dict);
            bytes = ms.ToArray();
        }
        catch (System.Exception e)
        {
            GD.PushError($"SaveSystem.Save: serialize failed: {e.Message}");
            return SaveError.WriteFailed;
        }

        // 2. Write the temp file in full.
        {
            using var file = Godot.FileAccess.Open(TempPath, Godot.FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PushError($"SaveSystem.Save: open temp failed ({Godot.FileAccess.GetOpenError()})");
                return SaveError.OpenFailed;
            }

            file.StoreBuffer(bytes);
            // Probe the file error before Dispose to surface write failures.
            var err = file.GetError();
            if (err != Error.Ok)
            {
                GD.PushError($"SaveSystem.Save: write temp failed ({err})");
                return SaveError.WriteFailed;
            }
        }

        // 3. Rotate existing main/bak0/bak1 BEFORE we rename temp into place.
        var rotateErr = BackupRotator.Rotate(SavePath, Backup0Path, Backup1Path);
        if (rotateErr != Error.Ok)
        {
            GD.PushError($"SaveSystem.Save: rotate failed ({rotateErr})");
            return SaveError.RenameFailed;
        }

        // 4. Promote temp -> main.
        var dir = DirAccess.Open("user://");
        if (dir == null)
        {
            GD.PushError($"SaveSystem.Save: open user:// failed ({DirAccess.GetOpenError()})");
            return SaveError.RenameFailed;
        }

        var renameErr = dir.Rename(
            BackupRotator.StripUserPrefix(TempPath),
            BackupRotator.StripUserPrefix(SavePath));
        if (renameErr != Error.Ok)
        {
            GD.PushError($"SaveSystem.Save: promote temp failed ({renameErr})");
            return SaveError.RenameFailed;
        }

        return SaveError.Ok;
    }

    public (SaveData Data, LoadError Error, LoadSource Source) Load()
    {
        var sources = new (string Path, LoadSource Source)[]
        {
            (SavePath,    LoadSource.Primary),
            (Backup0Path, LoadSource.Backup0),
            (Backup1Path, LoadSource.Backup1),
        };

        bool anyExisted = false;
        foreach (var (path, source) in sources)
        {
            if (!Godot.FileAccess.FileExists(path)) continue;
            anyExisted = true;

            var (data, status) = TryLoadFrom(path);
            if (status == SaveSerializer.DeserializeStatus.Ok && data != null)
            {
                if (source != LoadSource.Primary)
                    GD.Print($"SaveSystem.Load: recovered from {source}");
                return (data, LoadError.Ok, source);
            }

            GD.PushWarning($"SaveSystem.Load: {path} unusable ({status})");
        }

        if (anyExisted)
            return (null, LoadError.AllSourcesCorrupt, LoadSource.Default);
        return (null, LoadError.NoSaveFound, LoadSource.Default);
    }

    public bool DeleteAllSaveFiles()
    {
        var dir = DirAccess.Open("user://");
        if (dir == null)
        {
            GD.PushError($"SaveSystem.DeleteAllSaveFiles: open user:// failed ({DirAccess.GetOpenError()})");
            return false;
        }

        bool ok = true;
        foreach (var path in new[] { SavePath, TempPath, Backup0Path, Backup1Path })
        {
            if (!Godot.FileAccess.FileExists(path)) continue;
            var err = dir.Remove(BackupRotator.StripUserPrefix(path));
            if (err != Error.Ok)
            {
                GD.PushWarning($"SaveSystem.DeleteAllSaveFiles: remove {path} failed ({err})");
                ok = false;
            }
        }
        return ok;
    }

    // -------- internals --------

    private (SaveData Data, SaveSerializer.DeserializeStatus Status) TryLoadFrom(string path)
    {
        byte[] bytes;
        using (var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read))
        {
            if (file == null)
            {
                GD.PushWarning($"SaveSystem.TryLoadFrom: open {path} failed ({Godot.FileAccess.GetOpenError()})");
                // Treat unreadable as truncated for fallback purposes.
                return (null, SaveSerializer.DeserializeStatus.Truncated);
            }

            var len = (long)file.GetLength();
            bytes = file.GetBuffer(len);
        }

        Dictionary<string, object> dict;
        SaveSerializer.DeserializeStatus status;
        using (var ms = new MemoryStream(bytes, writable: false))
        {
            (dict, status) = _serializer.Deserialize(ms);
        }

        if (status != SaveSerializer.DeserializeStatus.Ok || dict == null)
            return (null, status);

        // Run migration; treat Invalid / FromFuture as load-time failure for this source.
        var (migrated, migrationStatus) = SaveDataMigrator.Migrate(dict);
        switch (migrationStatus)
        {
            case SaveDataMigrator.MigrationStatus.UpToDate:
            case SaveDataMigrator.MigrationStatus.Migrated:
                break;
            case SaveDataMigrator.MigrationStatus.FromFuture:
                return (null, SaveSerializer.DeserializeStatus.FromFutureVersion);
            case SaveDataMigrator.MigrationStatus.Invalid:
            default:
                return (null, SaveSerializer.DeserializeStatus.InvalidFormat);
        }

        SaveData data;
        try
        {
            data = FromDict(migrated);
        }
        catch (System.Exception e)
        {
            GD.PushWarning($"SaveSystem.TryLoadFrom: rebuild SaveData failed: {e.Message}");
            return (null, SaveSerializer.DeserializeStatus.InvalidFormat);
        }

        return (data, SaveSerializer.DeserializeStatus.Ok);
    }

    private static Dictionary<string, object> ToDict(SaveData data)
    {
        var inventory = new Dictionary<string, object>();
        if (data.InventoryCounts != null)
        {
            foreach (var kv in data.InventoryCounts)
                inventory[kv.Key] = kv.Value;
        }

        return new Dictionary<string, object>
        {
            [KeySaveVersion] = data.SaveVersion,
            [KeySavedAt]     = data.SavedAtUnix,
            [KeyMoney]       = data.Money,
            [KeyDay]         = data.Day,
            [KeyInventory]   = inventory,
        };
    }

    private static SaveData FromDict(IReadOnlyDictionary<string, object> dict)
    {
        var data = new SaveData();

        if (dict.TryGetValue(KeySaveVersion, out var v) && v is int version)
            data.SaveVersion = version;

        if (dict.TryGetValue(KeySavedAt, out var t))
        {
            switch (t)
            {
                case double td: data.SavedAtUnix = td; break;
                case long tl:   data.SavedAtUnix = tl; break;
                case int ti:    data.SavedAtUnix = ti; break;
            }
        }

        if (dict.TryGetValue(KeyMoney, out var m))
        {
            switch (m)
            {
                case int mi:  data.Money = mi; break;
                case long ml: data.Money = (int)ml; break;
            }
        }

        if (dict.TryGetValue(KeyDay, out var d))
        {
            switch (d)
            {
                case int di:  data.Day = di; break;
                case long dl: data.Day = (int)dl; break;
            }
        }

        if (dict.TryGetValue(KeyInventory, out var inv) && inv is IDictionary<string, object> invDict)
        {
            var counts = new Dictionary<string, int>(invDict.Count);
            foreach (var kv in invDict)
            {
                switch (kv.Value)
                {
                    case int ci:  counts[kv.Key] = ci; break;
                    case long cl: counts[kv.Key] = (int)cl; break;
                }
            }
            data.InventoryCounts = counts;
        }

        return data;
    }
}
