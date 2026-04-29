namespace SwarmSurvivor;

using System.Collections.Generic;
using Godot;

public partial class OtherPlayersUI : Control
{
    private static readonly Color AccentColor = new Color(0.78f, 0.63f, 0.38f);
    private static readonly Color PanelBgColor = new Color(0.08f, 0.07f, 0.05f, 0.78f);
    private static readonly Color PanelBorderColor = new Color(0.30f, 0.24f, 0.16f, 1f);
    private static readonly Color SubTextColor = new Color(0.85f, 0.82f, 0.74f);
    private static readonly Color HpColor = new Color(0.78f, 0.10f, 0.10f);

    public Node3D PlayersContainer { get; set; }

    private VBoxContainer _otherPlayersList;
    private readonly Dictionary<long, OtherPlayerEntry> _otherPlayerEntries = new();

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        BuildOtherPlayersPanel();
    }

    public void RefreshList()
    {
        if (PlayersContainer == null || !IsInstanceValid(PlayersContainer)) return;
        if (_otherPlayersList == null) return;

        var seen = new HashSet<long>();
        foreach (var child in PlayersContainer.GetChildren())
        {
            if (child is not NetworkedPlayer player) continue;
            if (!IsInstanceValid(player)) continue;
            if (player.IsLocalAuthority) continue;

            long peerId = player.OwnerPeerId;
            seen.Add(peerId);

            if (!_otherPlayerEntries.TryGetValue(peerId, out var entry))
            {
                entry = CreateOtherPlayerEntry(peerId);
                _otherPlayerEntries[peerId] = entry;
                _otherPlayersList.AddChild(entry.Root);
            }

            entry.NameLabel.Text = $"P{peerId}";
            entry.HpBar.MaxValue = player.MaxHealth;
            entry.HpBar.Value = Mathf.Clamp(player.CurrentHealth, 0, player.MaxHealth);
            entry.HpBar.Modulate = player.IsDead ? new Color(0.5f, 0.5f, 0.5f) : Colors.White;
            entry.HpLabel.Text = player.IsDead
                ? "死亡"
                : $"{player.CurrentHealth}/{player.MaxHealth}";
        }

        // Remove entries for peers no longer present.
        var stale = new List<long>();
        foreach (var kvp in _otherPlayerEntries)
        {
            if (!seen.Contains(kvp.Key)) stale.Add(kvp.Key);
        }
        foreach (var peerId in stale)
        {
            var entry = _otherPlayerEntries[peerId];
            entry.Root.QueueFree();
            _otherPlayerEntries.Remove(peerId);
        }
    }

    // ========== Other Players Panel ==========
    private void BuildOtherPlayersPanel()
    {
        var panel = CreateStyledPanel();
        panel.SetAnchorsPreset(LayoutPreset.TopRight);
        panel.OffsetLeft = -300f;
        panel.OffsetTop = 172f;
        panel.OffsetRight = -24f;
        panel.OffsetBottom = 360f;
        AddChild(panel);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        panel.AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 6);
        margin.AddChild(vbox);

        var header = CreateLabel("仲間", 18, AccentColor);
        vbox.AddChild(header);

        _otherPlayersList = new VBoxContainer();
        _otherPlayersList.AddThemeConstantOverride("separation", 4);
        vbox.AddChild(_otherPlayersList);
    }

    private OtherPlayerEntry CreateOtherPlayerEntry(long peerId)
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 6);

        var nameLabel = CreateLabel($"P{peerId}", 16, SubTextColor);
        nameLabel.CustomMinimumSize = new Vector2(70f, 0f);
        hbox.AddChild(nameLabel);

        var hpBar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(120f, 14f),
            ShowPercentage = false,
            MinValue = 0,
            MaxValue = 100,
            Value = 100,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        hpBar.AddThemeStyleboxOverride("background", CreateBarBg());
        hpBar.AddThemeStyleboxOverride("fill", CreateBarFill(HpColor));
        hbox.AddChild(hpBar);

        var hpLabel = CreateLabel("100/100", 14, SubTextColor);
        hpLabel.CustomMinimumSize = new Vector2(70f, 0f);
        hpLabel.HorizontalAlignment = HorizontalAlignment.Right;
        hbox.AddChild(hpLabel);

        return new OtherPlayerEntry
        {
            Root = hbox,
            NameLabel = nameLabel,
            HpBar = hpBar,
            HpLabel = hpLabel,
        };
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

    private static StyleBoxFlat CreateBarBg()
    {
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.04f, 0.03f, 0.9f),
            BorderColor = PanelBorderColor,
        };
        sb.SetBorderWidthAll(1);
        sb.SetCornerRadiusAll(2);
        return sb;
    }

    private static StyleBoxFlat CreateBarFill(Color color)
    {
        var sb = new StyleBoxFlat
        {
            BgColor = color,
        };
        sb.SetCornerRadiusAll(2);
        return sb;
    }

    private sealed class OtherPlayerEntry
    {
        public HBoxContainer Root;
        public Label NameLabel;
        public ProgressBar HpBar;
        public Label HpLabel;
    }
}
