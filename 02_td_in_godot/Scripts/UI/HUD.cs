namespace TowerDefense;

using Godot;
using ReactiveSO;

public partial class HUD : Control
{
    [Export] private IntVariable _gold;
    [Export] private IntVariable _lives;
    [Export] private IntVariable _waveNumber;
    [Export] private IntVariable _score;
    [Export] private VoidEventChannel _onGameOver;

    private Label _goldLabel;
    private Label _livesLabel;
    private Label _waveLabel;
    private Label _scoreLabel;
    private Control _gameOverPanel;
    private Label _gameOverLabel;
    private VBoxContainer _towerButtons;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;

        // Build UI
        BuildUI();

        // Subscribe to variable changes
        _gold.ValueChanged += (v) => _goldLabel.Text = $"Gold: {v}";
        _lives.ValueChanged += (v) => _livesLabel.Text = $"Lives: {v}";
        _waveNumber.ValueChanged += (v) => _waveLabel.Text = $"Wave: {v}";
        _score.ValueChanged += (v) => _scoreLabel.Text = $"Score: {v}";
        _onGameOver.Raised += ShowGameOver;

        // Set initial values
        _goldLabel.Text = $"Gold: {_gold.Value}";
        _livesLabel.Text = $"Lives: {_lives.Value}";
        _waveLabel.Text = $"Wave: {_waveNumber.Value}";
        _scoreLabel.Text = $"Score: {_score.Value}";
    }

    private void BuildUI()
    {
        // Top bar
        var topBar = new HBoxContainer();
        topBar.SetAnchorsPreset(LayoutPreset.TopWide);
        topBar.OffsetBottom = 40;
        topBar.AddThemeConstantOverride("separation", 30);

        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        topBar.AddChild(panel);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 30);
        panel.AddChild(hbox);

        _goldLabel = CreateLabel("Gold: 0");
        _livesLabel = CreateLabel("Lives: 0");
        _waveLabel = CreateLabel("Wave: 0");
        _scoreLabel = CreateLabel("Score: 0");

        hbox.AddChild(_goldLabel);
        hbox.AddChild(_livesLabel);
        hbox.AddChild(_waveLabel);
        hbox.AddChild(_scoreLabel);
        AddChild(topBar);

        // Tower buttons (right side)
        _towerButtons = new VBoxContainer();
        _towerButtons.SetAnchorsPreset(LayoutPreset.RightWide);
        _towerButtons.OffsetLeft = -160;
        _towerButtons.OffsetTop = 50;
        _towerButtons.AddThemeConstantOverride("separation", 10);
        AddChild(_towerButtons);

        // Game over panel (hidden initially)
        _gameOverPanel = new PanelContainer();
        _gameOverPanel.SetAnchorsPreset(LayoutPreset.Center);
        _gameOverPanel.CustomMinimumSize = new Vector2(300, 150);
        _gameOverPanel.Visible = false;

        _gameOverLabel = new Label();
        _gameOverLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _gameOverLabel.VerticalAlignment = VerticalAlignment.Center;
        _gameOverPanel.AddChild(_gameOverLabel);
        AddChild(_gameOverPanel);
    }

    public void AddTowerButton(TowerConfig config, TowerPlacement placement)
    {
        var button = new Button();
        button.Text = $"{config.DisplayName}\n${config.Cost}";
        button.CustomMinimumSize = new Vector2(150, 50);
        button.Pressed += () => placement.SelectTower(config);
        _towerButtons.AddChild(button);
    }

    private void ShowGameOver()
    {
        bool isVictory = _lives.Value > 0;
        _gameOverLabel.Text = isVictory
            ? $"VICTORY!\nScore: {_score.Value}\nPress R to restart"
            : $"GAME OVER\nScore: {_score.Value}\nPress R to restart";
        _gameOverPanel.Visible = true;
    }

    public void HideGameOver()
    {
        _gameOverPanel.Visible = false;
    }

    private Label CreateLabel(string text)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeFontSizeOverride("font_size", 18);
        return label;
    }
}
