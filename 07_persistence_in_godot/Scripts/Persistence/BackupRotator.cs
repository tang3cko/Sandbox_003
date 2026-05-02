namespace Persistence;

using Godot;

// Godot-dependent backup rotation. Pulled out of SaveSystem for responsibility split.
// Not unit-testable (depends on DirAccess); covered by manual play-test.
public static class BackupRotator
{
    private const string UserPrefix = "user://";

    // Rotation order (called AFTER a fresh write to TempPath has succeeded):
    //   1. Delete bak1 if present.
    //   2. Rename bak0 -> bak1 if bak0 present.
    //   3. Rename main -> bak0 if main present.
    //
    // Caller is responsible for the subsequent rename of temp -> main.
    public static Error Rotate(string mainPath, string bak0Path, string bak1Path)
    {
        var dir = DirAccess.Open(UserPrefix);
        if (dir == null)
        {
            return DirAccess.GetOpenError();
        }

        if (Godot.FileAccess.FileExists(bak1Path))
        {
            var err = dir.Remove(StripUserPrefix(bak1Path));
            if (err != Error.Ok) return err;
        }

        if (Godot.FileAccess.FileExists(bak0Path))
        {
            var err = dir.Rename(StripUserPrefix(bak0Path), StripUserPrefix(bak1Path));
            if (err != Error.Ok) return err;
        }

        if (Godot.FileAccess.FileExists(mainPath))
        {
            var err = dir.Rename(StripUserPrefix(mainPath), StripUserPrefix(bak0Path));
            if (err != Error.Ok) return err;
        }

        return Error.Ok;
    }

    public static string StripUserPrefix(string path) =>
        path != null && path.StartsWith(UserPrefix) ? path.Substring(UserPrefix.Length) : path;
}
