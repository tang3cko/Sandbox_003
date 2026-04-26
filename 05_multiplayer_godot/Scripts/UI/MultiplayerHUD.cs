namespace SwarmSurvivor;

using System.Collections.Generic;
using Godot;

public partial class MultiplayerHUD : CanvasLayer
{
    private static readonly Color AccentColor = new Color(0.78f, 0.63f, 0.38f);
    private static readonly Color PanelBgColor = new Color(0.08f, 0.07f, 0.05f, 0.78f);
    private static readonly Color PanelBorderColor = new Color(0.30f, 0.24f, 0.16f, 1f);
    private static readonly Color HpColor = new Color(0.78f, 0.10f, 0.10f);
    private static readonly Color ManaColor = new Color(0.20f, 0.40f, 0.85f);
    private static readonly Color SubTextColor = new Color(0.85f, 0.82f, 0.74f);
    private static readonly Color DimColor = new Color(0.55f, 0.50f, 0.42f);

    public Node3D PlayersContainer { get; set; }
    public GameStateSync GameState { get; set; }

    private NetworkedPlayer _cachedLocalPlayer;

    private StatOrb _hpOrb;
    private Label _hpLabel;
    private StatOrb _manaOrb;
    private Label _manaLabel;

    private Label _waveNumberLabel;
    private Label _enemiesRemainingLabel;
    private Label _totalKillsLabel;

    private VBoxContainer _otherPlayersList;
    private readonly Dictionary<long, OtherPlayerEntry> _otherPlayerEntries = new();

    private Label _questProgressLabel;
    private Label _questStatusLabel;

    public override void _Ready()
    {
        BuildHpOrb();
        BuildManaOrb();
        BuildWavePanel();
        BuildOtherPlayersPanel();
        BuildQuestPanel();
        BuildSkillBar();
        BuildWeaponBar();
    }

    public override void _Process(double delta)
    {
        UpdateLocalPlayer();
        UpdateWavePanel();
        UpdateOtherPlayers();
        UpdateQuestPanel();
    }

    // ========== HP Orb ==========
    private void BuildHpOrb()
    {
        const float orbSize = 120f;
        var container = new Control
        {
            CustomMinimumSize = new Vector2(orbSize, orbSize),
        };
        container.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        container.OffsetLeft = 28f;
        container.OffsetTop = -orbSize - 28f;
        container.OffsetRight = orbSize + 28f;
        container.OffsetBottom = -28f;
        AddChild(container);

        _hpOrb = new StatOrb
        {
            OrbColor = HpColor,
            MaxValue = 100,
            Value = 100,
        };
        _hpOrb.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        container.AddChild(_hpOrb);

        _hpLabel = new Label
        {
            Text = "100",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _hpLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _hpLabel.AddThemeFontSizeOverride("font_size", 36);
        _hpLabel.AddThemeColorOverride("font_color", Colors.White);
        _hpLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.85f));
        _hpLabel.AddThemeConstantOverride("outline_size", 4);
        container.AddChild(_hpLabel);
    }

    // ========== Mana Orb (placeholder) ==========
    private void BuildManaOrb()
    {
        const float orbSize = 120f;
        var container = new Control
        {
            CustomMinimumSize = new Vector2(orbSize, orbSize),
        };
        container.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        container.OffsetLeft = -orbSize - 28f;
        container.OffsetTop = -orbSize - 28f;
        container.OffsetRight = -28f;
        container.OffsetBottom = -28f;
        AddChild(container);

        _manaOrb = new StatOrb
        {
            OrbColor = ManaColor,
            MaxValue = 100,
            Value = 0,
            ShowFill = false,
        };
        _manaOrb.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        container.AddChild(_manaOrb);

        _manaLabel = new Label
        {
            Text = "—",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _manaLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _manaLabel.AddThemeFontSizeOverride("font_size", 36);
        _manaLabel.AddThemeColorOverride("font_color", SubTextColor);
        _manaLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.85f));
        _manaLabel.AddThemeConstantOverride("outline_size", 4);
        container.AddChild(_manaLabel);
    }

    // ========== Wave Panel (top right) ==========
    private void BuildWavePanel()
    {
        var panel = CreateStyledPanel();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        panel.OffsetLeft = -300f;
        panel.OffsetTop = 24f;
        panel.OffsetRight = -24f;
        panel.OffsetBottom = 156f;
        AddChild(panel);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
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

    private void UpdateWavePanel()
    {
        if (GameState == null) return;
        _waveNumberLabel.Text = $"ウェーブ {GameState.CurrentWave}";
        _enemiesRemainingLabel.Text = $"残りの敵: {GameState.EnemiesRemaining}";
        _totalKillsLabel.Text = $"撃破: {GameState.TotalKills}";
    }

    // ========== Other Players Panel ==========
    private void BuildOtherPlayersPanel()
    {
        var panel = CreateStyledPanel();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        panel.OffsetLeft = -300f;
        panel.OffsetTop = 172f;
        panel.OffsetRight = -24f;
        panel.OffsetBottom = 360f;
        AddChild(panel);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
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

    private void UpdateOtherPlayers()
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
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
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

    // ========== Quest Panel (left middle) ==========
    private void BuildQuestPanel()
    {
        var panel = CreateStyledPanel();
        panel.SetAnchorsPreset(Control.LayoutPreset.CenterLeft);
        panel.OffsetLeft = 24f;
        panel.OffsetTop = -50f;
        panel.OffsetRight = 280f;
        panel.OffsetBottom = 50f;
        AddChild(panel);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
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

    private void UpdateQuestPanel()
    {
        if (GameState == null) return;
        int wave = GameState.CurrentWave;
        _questProgressLabel.Text = $"・ウェーブを生き延びる ({wave}/{WaveCalculator.MaxWaves})";

        if (GameState.IsVictory)
        {
            _questStatusLabel.Text = "勝利！";
            _questStatusLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.3f));
        }
        else if (GameState.IsGameOver)
        {
            _questStatusLabel.Text = "ゲームオーバー";
            _questStatusLabel.AddThemeColorOverride("font_color", HpColor);
        }
        else
        {
            _questStatusLabel.Text = string.Empty;
        }
    }

    // ========== Skill Bar (center bottom) ==========
    private void BuildSkillBar()
    {
        BuildSlotBar(
            anchor: Control.LayoutPreset.CenterBottom,
            offsetX: -132f,
            offsetY: -88f,
            keys: new[] { "Q", "E", "R", "F" });
    }

    // ========== Weapon Bar (right bottom, above mana orb) ==========
    private void BuildWeaponBar()
    {
        BuildSlotBar(
            anchor: Control.LayoutPreset.BottomRight,
            offsetX: -424f,
            offsetY: -88f,
            keys: new[] { "1", "2", "3", "4" });
    }

    private void BuildSlotBar(Control.LayoutPreset anchor, float offsetX, float offsetY, string[] keys)
    {
        const float slotSize = 56f;
        const float separation = 8f;
        float totalWidth = (slotSize * keys.Length) + (separation * (keys.Length - 1));

        var container = new HBoxContainer();
        container.SetAnchorsPreset(anchor);
        container.OffsetLeft = offsetX;
        container.OffsetTop = offsetY;
        container.OffsetRight = offsetX + totalWidth;
        container.OffsetBottom = offsetY + slotSize;
        container.AddThemeConstantOverride("separation", (int)separation);
        AddChild(container);

        foreach (var key in keys)
        {
            var slot = new Panel
            {
                CustomMinimumSize = new Vector2(slotSize, slotSize),
            };
            slot.AddThemeStyleboxOverride("panel", CreateSlotStylebox());
            container.AddChild(slot);

            var label = new Label
            {
                Text = key,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            label.AddThemeFontSizeOverride("font_size", 20);
            label.AddThemeColorOverride("font_color", SubTextColor);
            slot.AddChild(label);
        }
    }

    // ========== Local Player Caching ==========
    private void UpdateLocalPlayer()
    {
        var player = GetLocalPlayer();
        if (player == null || !IsInstanceValid(player))
        {
            _hpLabel.Text = "—";
            _hpOrb.Value = 0;
            _hpOrb.MaxValue = 100;
            _hpOrb.QueueRedraw();
            return;
        }

        int hp = Mathf.Clamp(player.CurrentHealth, 0, player.MaxHealth);
        _hpLabel.Text = player.IsDead ? "DEAD" : hp.ToString();
        _hpOrb.MaxValue = player.MaxHealth;
        _hpOrb.Value = hp;
        _hpOrb.QueueRedraw();
    }

    private NetworkedPlayer GetLocalPlayer()
    {
        if (_cachedLocalPlayer != null && IsInstanceValid(_cachedLocalPlayer))
        {
            return _cachedLocalPlayer;
        }
        if (PlayersContainer == null || !IsInstanceValid(PlayersContainer)) return null;

        foreach (var child in PlayersContainer.GetChildren())
        {
            if (child is NetworkedPlayer p && p.IsLocalAuthority)
            {
                _cachedLocalPlayer = p;
                return p;
            }
        }
        return null;
    }

    // ========== Helpers ==========
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
        panel.AddThemeStyleboxOverride("panel", CreatePanelStylebox());
        return panel;
    }

    private static StyleBoxFlat CreatePanelStylebox()
    {
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
        return sb;
    }

    private static StyleBoxFlat CreateSlotStylebox()
    {
        var sb = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.05f, 0.85f),
            BorderColor = AccentColor,
        };
        sb.SetBorderWidthAll(2);
        sb.SetCornerRadiusAll(2);
        return sb;
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

public partial class StatOrb : Control
{
    public Color OrbColor { get; set; } = new Color(0.7f, 0.1f, 0.1f);
    public int Value { get; set; }
    public int MaxValue { get; set; } = 100;
    public bool ShowFill { get; set; } = true;

    public override void _Draw()
    {
        var size = Size;
        var radius = Mathf.Min(size.X, size.Y) * 0.45f;
        var center = size * 0.5f;

        // Outer frame
        DrawCircle(center, radius + 4f, new Color(0.15f, 0.12f, 0.08f));
        // Base (darkened orb color)
        DrawCircle(center, radius, OrbColor.Darkened(0.55f));
        // Fill (HP %)
        if (ShowFill && MaxValue > 0)
        {
            float fillRatio = Mathf.Clamp((float)Value / MaxValue, 0f, 1f);
            float fillRadius = radius * fillRatio;
            if (fillRadius > 0.01f)
            {
                DrawCircle(center, fillRadius, OrbColor);
            }
        }
        // Highlight glint (upper left)
        DrawCircle(
            center + new Vector2(-radius * 0.32f, -radius * 0.32f),
            radius * 0.22f,
            new Color(1f, 1f, 1f, 0.22f));
    }
}
