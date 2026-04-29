namespace SwarmSurvivor;

using Godot;

public partial class WavePanelUI : Control
{
    private static readonly Color AccentColor = new Color(0.78f, 0.63f, 0.38f);
    private static readonly Color PanelBgColor = new Color(0.08f, 0.07f, 0.05f, 0.78f);
    private static readonly Color PanelBorderColor = new Color(0.30f, 0.24f, 0.16f, 1f);
    private static readonly Color SubTextColor = new Color(0.85f, 0.82f, 0.74f);
    private static readonly Color HpColor = new Color(0.78f, 0.10f, 0.10f);

    private Label _waveNumberLabel;
    private Label _enemiesRemainingLabel;
    private Label _totalKillsLabel;

    private Label _questProgressLabel;
    private Label _questStatusLabel;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        BuildWavePanel();
        BuildQuestPanel();
    }

    public void UpdateWave(int currentWave, int enemiesRemaining, int totalKills)
    {
        if (_waveNumberLabel == null) return;
        _waveNumberLabel.Text = $"ウェーブ {currentWave}";
        _enemiesRemainingLabel.Text = $"残りの敵: {enemiesRemaining}";
        _totalKillsLabel.Text = $"撃破: {totalKills}";
    }

    public void UpdateQuest(int currentWave, bool isVictory, bool isGameOver)
    {
        if (_questProgressLabel == null) return;
        _questProgressLabel.Text = $"・ウェーブを生き延びる ({currentWave}/{WaveCalculator.MaxWaves})";

        if (isVictory)
        {
            _questStatusLabel.Text = "勝利！";
            _questStatusLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.3f));
        }
        else if (isGameOver)
        {
            _questStatusLabel.Text = "ゲームオーバー";
            _questStatusLabel.AddThemeColorOverride("font_color", HpColor);
        }
        else
        {
            _questStatusLabel.Text = string.Empty;
        }
    }

    // ========== Wave Panel (top right) ==========
    private void BuildWavePanel()
    {
        var panel = CreateStyledPanel();
        panel.SetAnchorsPreset(LayoutPreset.TopRight);
        panel.OffsetLeft = -300f;
        panel.OffsetTop = 24f;
        panel.OffsetRight = -24f;
        panel.OffsetBottom = 156f;
        AddChild(panel);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        margin.AddChild(vbox);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(header);

        var skull = CreateLabel("\u2620", 36, AccentColor);
        header.AddChild(skull);

        _waveNumberLabel = CreateLabel("ウェーブ 0", 32, AccentColor);
        header.AddChild(_waveNumberLabel);

        _enemiesRemainingLabel = CreateLabel("残りの敵: 0", 18, SubTextColor);
        vbox.AddChild(_enemiesRemainingLabel);

        _totalKillsLabel = CreateLabel("撃破: 0", 18, SubTextColor);
        vbox.AddChild(_totalKillsLabel);
    }

    // ========== Quest Panel (left middle) ==========
    private void BuildQuestPanel()
    {
        var panel = CreateStyledPanel();
        panel.SetAnchorsPreset(LayoutPreset.CenterLeft);
        panel.OffsetLeft = 24f;
        panel.OffsetTop = -50f;
        panel.OffsetRight = 280f;
        panel.OffsetBottom = 50f;
        AddChild(panel);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 4);
        margin.AddChild(vbox);

        var title = CreateLabel("終わらぬ侵食", 20, AccentColor);
        vbox.AddChild(title);

        _questProgressLabel = CreateLabel("・ウェーブを生き延びる (0/15)", 16, SubTextColor);
        vbox.AddChild(_questProgressLabel);

        _questStatusLabel = CreateLabel(string.Empty, 16, AccentColor);
        vbox.AddChild(_questStatusLabel);
    }

    private static Label CreateLabel(string text, int fontSize, Color color)
    {
        var label = new Label
        {
            Text = text,
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.85f));
        label.AddThemeConstantOverride("outline_size", 3);
        return label;
    }

    private static Panel CreateStyledPanel()
    {
        var panel = new Panel();
        var sb = new StyleBoxFlat
        {
            BgColor = PanelBgColor,
            BorderColor = PanelBorderColor,
        };
        sb.SetBorderWidthAll(2);
        sb.SetCornerRadiusAll(4);
        sb.ContentMarginLeft = 6f;
        sb.ContentMarginTop = 6f;
        sb.ContentMarginRight = 6f;
        sb.ContentMarginBottom = 6f;
        panel.AddThemeStyleboxOverride("panel", sb);
        return panel;
    }
}
