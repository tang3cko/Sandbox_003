namespace Persistence.Tests;
using Xunit;

public class SettingsDataTests
{
    [Fact]
    public void CreateDefault_ReturnsValidDefaults()
    {
        var s = SettingsData.CreateDefault();
        Assert.Equal(SettingsData.CurrentVersion, s.SettingsVersion);
        Assert.Equal(0.8f, s.BgmVolume);
        Assert.Equal(1.0f, s.SfxVolume);
        Assert.Equal(SettingsData.DisplayModeWindowed, s.DisplayMode);
        Assert.True(s.IsValid());
    }

    [Theory]
    [InlineData(-0.1f, 1.0f, 0)]
    [InlineData(1.1f, 1.0f, 0)]
    [InlineData(0.5f, -0.1f, 0)]
    [InlineData(0.5f, 1.1f, 0)]
    [InlineData(0.5f, 0.5f, -1)]
    [InlineData(0.5f, 0.5f, 3)]
    public void IsValid_FalseForOutOfRange(float bgm, float sfx, int displayMode)
    {
        var s = new SettingsData
        {
            BgmVolume = bgm,
            SfxVolume = sfx,
            DisplayMode = displayMode,
        };
        Assert.False(s.IsValid());
    }

    [Fact]
    public void IsValid_FalseForWrongVersion()
    {
        var s = SettingsData.CreateDefault();
        s.SettingsVersion = SettingsData.CurrentVersion + 1;
        Assert.False(s.IsValid());
    }

    [Theory]
    [InlineData(-0.5f, 0f)]
    [InlineData(1.5f, 1f)]
    [InlineData(0.3f, 0.3f)]
    public void Clamped_BoundsBgmVolumeTo01(float input, float expected)
    {
        var s = new SettingsData { BgmVolume = input };
        Assert.Equal(expected, s.Clamped().BgmVolume);
    }

    [Theory]
    [InlineData(-0.5f, 0f)]
    [InlineData(1.5f, 1f)]
    [InlineData(0.3f, 0.3f)]
    public void Clamped_BoundsSfxVolumeTo01(float input, float expected)
    {
        var s = new SettingsData { SfxVolume = input };
        Assert.Equal(expected, s.Clamped().SfxVolume);
    }

    [Theory]
    [InlineData(-1, SettingsData.DisplayModeWindowed)]
    [InlineData(99, SettingsData.DisplayModeFullscreen)]
    [InlineData(SettingsData.DisplayModeBorderless, SettingsData.DisplayModeBorderless)]
    public void Clamped_BoundsDisplayMode(int input, int expected)
    {
        var s = new SettingsData { DisplayMode = input };
        Assert.Equal(expected, s.Clamped().DisplayMode);
    }

    [Fact]
    public void Clamped_NaNBgmVolume_ResetsToZero()
    {
        var s = new SettingsData { BgmVolume = float.NaN };
        Assert.Equal(0f, s.Clamped().BgmVolume);
    }

    [Fact]
    public void Clamped_RestoresVersion()
    {
        var s = new SettingsData { SettingsVersion = 999 };
        Assert.Equal(SettingsData.CurrentVersion, s.Clamped().SettingsVersion);
    }

    [Fact]
    public void Clamped_AlreadyValid_PreservesValues()
    {
        var s = new SettingsData
        {
            BgmVolume = 0.5f,
            SfxVolume = 0.7f,
            DisplayMode = SettingsData.DisplayModeFullscreen,
        };
        var c = s.Clamped();
        Assert.True(c.IsValid());
        Assert.Equal(0.5f, c.BgmVolume);
        Assert.Equal(0.7f, c.SfxVolume);
        Assert.Equal(SettingsData.DisplayModeFullscreen, c.DisplayMode);
    }
}
