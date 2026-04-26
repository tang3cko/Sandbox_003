namespace SwarmSurvivor;

using Godot;
using ReactiveSO;

public partial class UpgradePanel : Control
{
    [Export] private IntEventChannel _onLevelUp;
    [Export] private IntEventChannel _onUpgradeSelected;
    [Export] public UpgradeConfig[] AvailableUpgrades { get; set; }

    private PanelContainer _panel;
    private VBoxContainer _buttonContainer;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
        SetOffsetsPreset(LayoutPreset.FullRect);

        BuildUI();

        if (_onLevelUp != null)
        {
            _onLevelUp.Raised += HandleLevelUp;
        }

        ProcessMode = ProcessModeEnum.Always;
    }

    private void BuildUI()
    {
        _panel = new PanelContainer();
        _panel.SetAnchorsPreset(LayoutPreset.Center);
        _panel.SetOffsetsPreset(LayoutPreset.Center);
        _panel.CustomMinimumSize = new Vector2(700, 500);
        _panel.OffsetLeft = -350;
        _panel.OffsetRight = 350;
        _panel.OffsetTop = -250;
        _panel.OffsetBottom = 250;
        _panel.Visible = false;

        var vbox = new VBoxContainer();
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddThemeConstantOverride("separation", 20);

        var title = CreateLabel("LEVEL UP!", 60);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        var separator = new HSeparator();
        vbox.AddChild(separator);

        _buttonContainer = new VBoxContainer();
        _buttonContainer.AddThemeConstantOverride("separation", 15);
        vbox.AddChild(_buttonContainer);

        _panel.AddChild(vbox);
        AddChild(_panel);
    }

    private void HandleLevelUp(int level)
    {
        if (AvailableUpgrades == null || AvailableUpgrades.Length == 0) return;

        var choices = UpgradeCalculator.GetRandomChoices(
            BuildChoiceArray(), 3, (int)(Time.GetTicksMsec()));

        ShowChoices(choices);
    }

    private UpgradeChoice[] BuildChoiceArray()
    {
        var arr = new UpgradeChoice[AvailableUpgrades.Length];
        for (int i = 0; i < AvailableUpgrades.Length; i++)
        {
            arr[i] = new UpgradeChoice(
                AvailableUpgrades[i].Id,
                AvailableUpgrades[i].UpgradeName,
                AvailableUpgrades[i].Description);
        }
        return arr;
    }

    private void ShowChoices(UpgradeChoice[] choices)
    {
        foreach (var child in _buttonContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var choice in choices)
        {
            var button = new Button
            {
                CustomMinimumSize = new Vector2(0, 80),
            };
            button.AddThemeFontSizeOverride("font_size", 32);
            button.Text = $"{choice.Name} - {choice.Description}";
            int id = choice.Id;
            button.Pressed += () => SelectUpgrade(id);
            _buttonContainer.AddChild(button);
        }

        _panel.Visible = true;
        GetTree().Paused = true;
    }

    private void SelectUpgrade(int id)
    {
        _panel.Visible = false;
        GetTree().Paused = false;
        _onUpgradeSelected?.Raise(id);
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
        if (_onLevelUp != null)
        {
            _onLevelUp.Raised -= HandleLevelUp;
        }
    }
}
