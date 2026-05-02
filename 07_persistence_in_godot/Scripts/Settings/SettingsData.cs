namespace Persistence;

public sealed class SettingsData
{
    public const int CurrentVersion = 1;

    public const int DisplayModeWindowed = 0;
    public const int DisplayModeBorderless = 1;
    public const int DisplayModeFullscreen = 2;

    public int SettingsVersion { get; set; } = CurrentVersion;
    public float BgmVolume { get; set; } = 0.8f;
    public float SfxVolume { get; set; } = 1.0f;
    public int DisplayMode { get; set; } = DisplayModeWindowed;

    public static SettingsData CreateDefault() => new();

    public bool IsValid()
    {
        if (SettingsVersion != CurrentVersion) return false;
        if (BgmVolume < 0f || BgmVolume > 1f) return false;
        if (SfxVolume < 0f || SfxVolume > 1f) return false;
        if (DisplayMode < DisplayModeWindowed || DisplayMode > DisplayModeFullscreen) return false;
        return true;
    }

    public SettingsData Clamped()
    {
        return new SettingsData
        {
            SettingsVersion = CurrentVersion,
            BgmVolume = Clamp01(BgmVolume),
            SfxVolume = Clamp01(SfxVolume),
            DisplayMode = ClampDisplayMode(DisplayMode),
        };
    }

    private static float Clamp01(float v)
    {
        if (float.IsNaN(v)) return 0f;
        if (v < 0f) return 0f;
        if (v > 1f) return 1f;
        return v;
    }

    private static int ClampDisplayMode(int mode)
    {
        if (mode < DisplayModeWindowed) return DisplayModeWindowed;
        if (mode > DisplayModeFullscreen) return DisplayModeFullscreen;
        return mode;
    }
}
