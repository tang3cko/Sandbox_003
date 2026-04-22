namespace ArenaSurvivor;

using Godot;

public partial class TitleScreen : Control
{
    private Label _promptLabel;
    private float _blinkTimer;
    private bool _promptVisible = true;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
        SetOffsetsPreset(LayoutPreset.FullRect);

        BuildUI();
    }

    public override void _Process(double delta)
    {
        _blinkTimer += (float)delta;
        if (_blinkTimer >= 0.8f)
        {
            _blinkTimer = 0;
            _promptVisible = !_promptVisible;
            _promptLabel.Visible = _promptVisible;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true } or InputEventMouseButton { Pressed: true })
        {
            GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
            return;
        }
    }

    private void BuildUI()
    {
        var bg = new ColorRect();
        bg.Color = new Color(0.03f, 0.03f, 0.08f, 1);
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        bg.SetOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.Center);
        vbox.SetOffsetsPreset(LayoutPreset.Center);
        vbox.CustomMinimumSize = new Vector2(800, 400);
        vbox.OffsetLeft = -400;
        vbox.OffsetRight = 400;
        vbox.OffsetTop = -200;
        vbox.OffsetBottom = 200;
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddThemeConstantOverride("separation", 40);

        var title = CreateLabel("ARENA SURVIVOR", 80);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        var subtitle = CreateLabel("Phase 4: Action Game", 36);
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.7f));
        vbox.AddChild(subtitle);

        _promptLabel = CreateLabel("Press any key to start", 36);
        _promptLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(_promptLabel);

        AddChild(vbox);
    }

    private Label CreateLabel(string text, int fontSize)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }
}
