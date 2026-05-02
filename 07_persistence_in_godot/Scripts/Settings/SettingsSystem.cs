namespace Persistence;

using Godot;

// Godot-dependent settings system.
//
// Stores SettingsData in user://settings.cfg via Godot's ConfigFile (INI-like).
// Three sections:
//   [meta]    version
//   [audio]   bgm_volume, sfx_volume
//   [display] mode
//
// Load applies SettingsData.Clamped() to keep values in spec ranges
// even if the file was tampered with.
public sealed class SettingsSystem
{
    public const string SettingsPath = "user://settings.cfg";

    private const string SectionMeta    = "meta";
    private const string SectionAudio   = "audio";
    private const string SectionDisplay = "display";

    private const string KeyVersion    = "version";
    private const string KeyBgmVolume  = "bgm_volume";
    private const string KeySfxVolume  = "sfx_volume";
    private const string KeyDisplayMode = "mode";

    public Error Save(SettingsData data)
    {
        if (data == null)
        {
            GD.PushError("SettingsSystem.Save: data is null");
            return Error.InvalidParameter;
        }

        var clamped = data.Clamped();

        var cfg = new ConfigFile();
        cfg.SetValue(SectionMeta,    KeyVersion,     clamped.SettingsVersion);
        cfg.SetValue(SectionAudio,   KeyBgmVolume,   clamped.BgmVolume);
        cfg.SetValue(SectionAudio,   KeySfxVolume,   clamped.SfxVolume);
        cfg.SetValue(SectionDisplay, KeyDisplayMode, clamped.DisplayMode);

        // Save (no encryption key) - ConfigFile.Save returns Error.Ok on success.
        var err = cfg.Save(SettingsPath);
        if (err != Error.Ok)
            GD.PushError($"SettingsSystem.Save: write failed ({err})");
        return err;
    }

    public (SettingsData Data, Error Error) Load()
    {
        if (!Godot.FileAccess.FileExists(SettingsPath))
            return (SettingsData.CreateDefault(), Error.FileNotFound);

        var cfg = new ConfigFile();
        var err = cfg.Load(SettingsPath);
        if (err != Error.Ok)
        {
            GD.PushWarning($"SettingsSystem.Load: load failed ({err}), using defaults");
            return (SettingsData.CreateDefault(), err);
        }

        var def = SettingsData.CreateDefault();

        var data = new SettingsData
        {
            SettingsVersion = (int)  cfg.GetValue(SectionMeta,    KeyVersion,     def.SettingsVersion),
            BgmVolume       = (float)(double)cfg.GetValue(SectionAudio, KeyBgmVolume, (double)def.BgmVolume),
            SfxVolume       = (float)(double)cfg.GetValue(SectionAudio, KeySfxVolume, (double)def.SfxVolume),
            DisplayMode     = (int)  cfg.GetValue(SectionDisplay, KeyDisplayMode, def.DisplayMode),
        };

        // Fail-soft: out-of-range values are pulled back into spec rather than rejected.
        return (data.Clamped(), Error.Ok);
    }

    public bool DeleteSettingsFile()
    {
        if (!Godot.FileAccess.FileExists(SettingsPath))
            return true;

        var dir = DirAccess.Open("user://");
        if (dir == null)
        {
            GD.PushError($"SettingsSystem.DeleteSettingsFile: open user:// failed ({DirAccess.GetOpenError()})");
            return false;
        }

        var err = dir.Remove(BackupRotator.StripUserPrefix(SettingsPath));
        if (err != Error.Ok)
        {
            GD.PushWarning($"SettingsSystem.DeleteSettingsFile: remove failed ({err})");
            return false;
        }
        return true;
    }
}
