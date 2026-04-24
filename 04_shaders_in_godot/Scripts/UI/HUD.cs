namespace SwarmSurvivor;

using Godot;
using ReactiveSO;

public partial class HUD : CanvasLayer
{
    [Export] private IntVariable _health;
    [Export] private IntVariable _score;
    [Export] private IntVariable _waveNumber;
    [Export] private IntVariable _level;
    [Export] private IntVariable _xp;
    [Export] private IntVariable _killCount;
    [Export] private IntVariable _xpToNext;

    private Label _healthLabel;
    private Label _scoreLabel;
    private Label _waveLabel;
    private Label _levelLabel;
    private ProgressBar _xpBar;
    private Label _killLabel;
    private int _maxHealth = 100;

    public override void _Ready()
    {
        var margin = new MarginContainer();
        margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        margin.SetOffsetsPreset(Control.LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 40);
        margin.AddThemeConstantOverride("margin_top", 30);
        margin.AddThemeConstantOverride("margin_right", 40);
        AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(vbox);

        _healthLabel = CreateLabel("Health: 100", 36);
        vbox.AddChild(_healthLabel);

        _levelLabel = CreateLabel("Level: 1", 36);
        vbox.AddChild(_levelLabel);

        _xpBar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(300, 24),
            MinValue = 0,
            MaxValue = 1,
            Value = 0,
        };
        vbox.AddChild(_xpBar);

        _waveLabel = CreateLabel("Wave: 0", 36);
        vbox.AddChild(_waveLabel);

        _scoreLabel = CreateLabel("Score: 0", 36);
        vbox.AddChild(_scoreLabel);

        _killLabel = CreateLabel("Kills: 0", 36);
        vbox.AddChild(_killLabel);

        ConnectVariables();
    }

    private Label CreateLabel(string text, int fontSize)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", Colors.White);
        return label;
    }

    private void ConnectVariables()
    {
        if (_health != null) _health.ValueChanged += OnHealthChanged;
        if (_score != null) _score.ValueChanged += OnScoreChanged;
        if (_waveNumber != null) _waveNumber.ValueChanged += OnWaveChanged;
        if (_level != null) _level.ValueChanged += OnLevelChanged;
        if (_xp != null) _xp.ValueChanged += OnXPChanged;
        if (_killCount != null) _killCount.ValueChanged += OnKillChanged;
        if (_xpToNext != null) _xpToNext.ValueChanged += OnXPToNextChanged;

        if (_health != null) OnHealthChanged(_health.Value);
        if (_score != null) OnScoreChanged(_score.Value);
        if (_waveNumber != null) OnWaveChanged(_waveNumber.Value);
        if (_level != null) OnLevelChanged(_level.Value);
        if (_xp != null) OnXPChanged(_xp.Value);
        if (_killCount != null) OnKillChanged(_killCount.Value);
        if (_xpToNext != null) OnXPToNextChanged(_xpToNext.Value);
    }

    public void SetMaxHealth(int max) { _maxHealth = max; }
    public void SetXPToNextLevel(int xp) { _xpBar.MaxValue = xp; }

    private void OnHealthChanged(int value) => _healthLabel.Text = $"Health: {value}/{_maxHealth}";
    private void OnScoreChanged(int value) => _scoreLabel.Text = $"Score: {value}";
    private void OnWaveChanged(int value) => _waveLabel.Text = $"Wave: {value}/{WaveCalculator.MaxWaves}";
    private void OnLevelChanged(int value) => _levelLabel.Text = $"Level: {value}";
    private void OnXPChanged(int value) => _xpBar.Value = value;
    private void OnKillChanged(int value) => _killLabel.Text = $"Kills: {value}";
    private void OnXPToNextChanged(int value) => _xpBar.MaxValue = value;

    public override void _ExitTree()
    {
        if (_health != null) _health.ValueChanged -= OnHealthChanged;
        if (_score != null) _score.ValueChanged -= OnScoreChanged;
        if (_waveNumber != null) _waveNumber.ValueChanged -= OnWaveChanged;
        if (_level != null) _level.ValueChanged -= OnLevelChanged;
        if (_xp != null) _xp.ValueChanged -= OnXPChanged;
        if (_killCount != null) _killCount.ValueChanged -= OnKillChanged;
        if (_xpToNext != null) _xpToNext.ValueChanged -= OnXPToNextChanged;
    }
}
