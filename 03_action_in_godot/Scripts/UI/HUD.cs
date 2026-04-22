namespace ArenaSurvivor;

using Godot;
using ReactiveSO;

public partial class HUD : Control
{
    [Export] private IntVariable _health;
    [Export] private FloatVariable _stamina;
    [Export] private IntVariable _score;
    [Export] private IntVariable _waveNumber;
    [Export] private VoidEventChannel _onGameOver;
    [Export] private VoidEventChannel _onWaveCompleted;

    private Label _healthLabel;
    private ProgressBar _healthBar;
    private ProgressBar _staminaBar;
    private Label _scoreLabel;
    private Label _waveLabel;
    private Control _gameOverPanel;
    private Label _gameOverLabel;
    private Label _waveAnnouncement;
    private float _waveAnnouncementTimer;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
        SetOffsetsPreset(LayoutPreset.FullRect);

        BuildUI();

        _health.ValueChanged += OnHealthChanged;
        _stamina.ValueChanged += OnStaminaChanged;
        _score.ValueChanged += OnScoreChanged;
        _waveNumber.ValueChanged += OnWaveNumberChanged;
        _onGameOver.Raised += ShowGameOver;
        _onWaveCompleted.Raised += OnWaveCompleted;

        _healthLabel.Text = $"{_health.Value}";
        _scoreLabel.Text = $"Score: {_score.Value}";
        _waveLabel.Text = $"Wave {_waveNumber.Value}";
    }

    public override void _Process(double delta)
    {
        if (GetTree().Paused) return;

        if (_waveAnnouncementTimer > 0f)
        {
            _waveAnnouncementTimer -= (float)delta;
            if (_waveAnnouncementTimer <= 0f)
            {
                _waveAnnouncement.Visible = false;
            }
        }
    }

    private void OnHealthChanged(int v)
    {
        _healthLabel.Text = $"{v}";
        _healthBar.Value = v;
    }

    private void OnStaminaChanged(float v)
    {
        _staminaBar.Value = v;
    }

    private void OnScoreChanged(int v)
    {
        _scoreLabel.Text = $"Score: {v}";
    }

    private void OnWaveNumberChanged(int v)
    {
        _waveLabel.Text = $"Wave {v}";
        ShowWaveAnnouncement(v);
    }

    private void OnWaveCompleted()
    {
        ShowWaveAnnouncement(0);
    }

    private void ShowWaveAnnouncement(int wave)
    {
        _waveAnnouncement.Text = wave > 0 ? $"WAVE {wave}" : "WAVE COMPLETE";
        _waveAnnouncement.Visible = true;
        _waveAnnouncementTimer = 2.0f;
    }

    private void ShowGameOver()
    {
        _gameOverLabel.Text = $"GAME OVER\nScore: {_score.Value}\nWave: {_waveNumber.Value}\n\nR: Restart  T: Title";
        _gameOverPanel.Visible = true;
    }

    public void HideGameOver()
    {
        _gameOverPanel.Visible = false;
    }

    private void BuildUI()
    {
        var topBar = new PanelContainer();
        topBar.SetAnchorsPreset(LayoutPreset.TopWide);
        topBar.SetOffsetsPreset(LayoutPreset.TopWide);
        topBar.OffsetBottom = 120;

        var topHBox = new HBoxContainer();
        topHBox.AddThemeConstantOverride("separation", 20);

        var healthContainer = new VBoxContainer();
        healthContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        healthContainer.AddThemeConstantOverride("separation", 4);

        var healthHeader = new HBoxContainer();
        var healthTitle = CreateLabel("HP", 36);
        _healthLabel = CreateLabel("100", 36);
        healthHeader.AddChild(healthTitle);
        healthHeader.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
        healthHeader.AddChild(_healthLabel);
        healthContainer.AddChild(healthHeader);

        _healthBar = new ProgressBar();
        _healthBar.CustomMinimumSize = new Vector2(250, 24);
        _healthBar.MaxValue = 100;
        _healthBar.Value = 100;
        _healthBar.ShowPercentage = false;
        healthContainer.AddChild(_healthBar);

        _staminaBar = new ProgressBar();
        _staminaBar.CustomMinimumSize = new Vector2(250, 14);
        _staminaBar.MaxValue = 100;
        _staminaBar.Value = 100;
        _staminaBar.ShowPercentage = false;
        healthContainer.AddChild(_staminaBar);

        topHBox.AddChild(healthContainer);
        topHBox.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });

        var infoContainer = new VBoxContainer();
        _scoreLabel = CreateLabel("Score: 0", 36);
        _scoreLabel.HorizontalAlignment = HorizontalAlignment.Right;
        _waveLabel = CreateLabel("Wave 0", 36);
        _waveLabel.HorizontalAlignment = HorizontalAlignment.Right;
        infoContainer.AddChild(_scoreLabel);
        infoContainer.AddChild(_waveLabel);
        topHBox.AddChild(infoContainer);

        topBar.AddChild(topHBox);
        AddChild(topBar);

        _waveAnnouncement = CreateLabel("", 48);
        _waveAnnouncement.SetAnchorsPreset(LayoutPreset.CenterTop);
        _waveAnnouncement.SetOffsetsPreset(LayoutPreset.CenterTop);
        _waveAnnouncement.HorizontalAlignment = HorizontalAlignment.Center;
        _waveAnnouncement.VerticalAlignment = VerticalAlignment.Center;
        _waveAnnouncement.Visible = false;
        _waveAnnouncement.OffsetLeft = -200;
        _waveAnnouncement.OffsetRight = 200;
        _waveAnnouncement.OffsetTop = 200;
        _waveAnnouncement.OffsetBottom = 280;
        AddChild(_waveAnnouncement);

        _gameOverPanel = new PanelContainer();
        _gameOverPanel.SetAnchorsPreset(LayoutPreset.Center);
        _gameOverPanel.SetOffsetsPreset(LayoutPreset.Center);
        _gameOverPanel.CustomMinimumSize = new Vector2(600, 350);
        _gameOverPanel.OffsetLeft = -300;
        _gameOverPanel.OffsetRight = 300;
        _gameOverPanel.OffsetTop = -175;
        _gameOverPanel.OffsetBottom = 175;
        _gameOverPanel.Visible = false;

        _gameOverLabel = CreateLabel("", 60);
        _gameOverLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _gameOverLabel.VerticalAlignment = VerticalAlignment.Center;
        _gameOverPanel.AddChild(_gameOverLabel);
        AddChild(_gameOverPanel);
    }

    private Label CreateLabel(string text, int fontSize)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }

    public override void _ExitTree()
    {
        _health.ValueChanged -= OnHealthChanged;
        _stamina.ValueChanged -= OnStaminaChanged;
        _score.ValueChanged -= OnScoreChanged;
        _waveNumber.ValueChanged -= OnWaveNumberChanged;
        _onGameOver.Raised -= ShowGameOver;
        _onWaveCompleted.Raised -= OnWaveCompleted;
    }
}
