namespace SwarmSurvivor;

using Godot;
using ReactiveSO;

public partial class PauseMenu : Control
{
    [Export] private VoidEventChannel _onGameOver;

    private bool _isPaused;
    private bool _isGameOver;
    private PanelContainer _pausePanel;
    private Label _titleLabel;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
        SetOffsetsPreset(LayoutPreset.FullRect);

        BuildUI();

        if (_onGameOver != null)
            _onGameOver.Raised += HandleGameOver;

        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("pause"))
        {
            if (_isGameOver) return;
            TogglePause();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey { Pressed: true } key)
        {
            if (key.Keycode == Key.R && (_isPaused || _isGameOver))
            {
                Restart();
                return;
            }

            if (key.Keycode == Key.T && (_isPaused || _isGameOver))
            {
                GoToTitle();
                return;
            }
        }
    }

    private void TogglePause()
    {
        _isPaused = !_isPaused;
        GetTree().Paused = _isPaused;
        _pausePanel.Visible = _isPaused;
    }

    public void ShowVictory()
    {
        _isGameOver = true;
        _titleLabel.Text = "VICTORY!";
        _pausePanel.Visible = true;
        GetTree().Paused = true;
    }

    private void HandleGameOver()
    {
        _isGameOver = true;
        _titleLabel.Text = "GAME OVER";
        _pausePanel.Visible = true;
    }

    private void Restart()
    {
        Engine.TimeScale = 1.0;
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }

    private void GoToTitle()
    {
        Engine.TimeScale = 1.0;
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://Scenes/Title.tscn");
    }

    private void BuildUI()
    {
        _pausePanel = new PanelContainer();
        _pausePanel.SetAnchorsPreset(LayoutPreset.Center);
        _pausePanel.SetOffsetsPreset(LayoutPreset.Center);
        _pausePanel.CustomMinimumSize = new Vector2(500, 300);
        _pausePanel.OffsetLeft = -250;
        _pausePanel.OffsetRight = 250;
        _pausePanel.OffsetTop = -150;
        _pausePanel.OffsetBottom = 150;
        _pausePanel.Visible = false;

        var vbox = new VBoxContainer();
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddThemeConstantOverride("separation", 20);

        _titleLabel = CreateLabel("PAUSED", 60);
        _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_titleLabel);

        var separator = new HSeparator();
        vbox.AddChild(separator);

        var resumeHint = CreateLabel("Resume: ESC", 36);
        resumeHint.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(resumeHint);

        var restartHint = CreateLabel("Restart: R", 36);
        restartHint.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(restartHint);

        var titleHint = CreateLabel("Title: T", 36);
        titleHint.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(titleHint);

        _pausePanel.AddChild(vbox);
        AddChild(_pausePanel);
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
        if (_onGameOver != null)
            _onGameOver.Raised -= HandleGameOver;
    }
}
