namespace SwarmSurvivor;

using Godot;

public partial class HpOrbUI : Control
{
    private static readonly Color HpColor = new Color(0.78f, 0.10f, 0.10f);
    private static readonly Color ManaColor = new Color(0.20f, 0.40f, 0.85f);
    private static readonly Color SubTextColor = new Color(0.85f, 0.82f, 0.74f);

    private StatOrb _hpOrb;
    private Label _hpLabel;
    private StatOrb _manaOrb;
    private Label _manaLabel;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        BuildHpOrb();
        BuildManaOrb();
    }

    public void UpdateHp(int currentHealth, int maxHealth, bool isDead)
    {
        if (_hpOrb == null || _hpLabel == null) return;

        if (maxHealth <= 0)
        {
            _hpLabel.Text = "—";
            _hpOrb.Value = 0;
            _hpOrb.MaxValue = 100;
            _hpOrb.QueueRedraw();
            return;
        }

        int hp = Mathf.Clamp(currentHealth, 0, maxHealth);
        _hpLabel.Text = isDead ? "DEAD" : hp.ToString();
        _hpOrb.MaxValue = maxHealth;
        _hpOrb.Value = hp;
        _hpOrb.QueueRedraw();
    }

    public void ClearHp()
    {
        if (_hpOrb == null || _hpLabel == null) return;
        _hpLabel.Text = "—";
        _hpOrb.Value = 0;
        _hpOrb.MaxValue = 100;
        _hpOrb.QueueRedraw();
    }

    // ========== HP Orb ==========
    private void BuildHpOrb()
    {
        const float orbSize = 120f;
        var container = new Control
        {
            CustomMinimumSize = new Vector2(orbSize, orbSize),
        };
        container.SetAnchorsPreset(LayoutPreset.BottomLeft);
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
        _hpOrb.SetAnchorsPreset(LayoutPreset.FullRect);
        container.AddChild(_hpOrb);

        _hpLabel = new Label
        {
            Text = "100",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _hpLabel.SetAnchorsPreset(LayoutPreset.FullRect);
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
        container.SetAnchorsPreset(LayoutPreset.BottomRight);
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
        _manaOrb.SetAnchorsPreset(LayoutPreset.FullRect);
        container.AddChild(_manaOrb);

        _manaLabel = new Label
        {
            Text = "—",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _manaLabel.SetAnchorsPreset(LayoutPreset.FullRect);
        _manaLabel.AddThemeFontSizeOverride("font_size", 36);
        _manaLabel.AddThemeColorOverride("font_color", SubTextColor);
        _manaLabel.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.85f));
        _manaLabel.AddThemeConstantOverride("outline_size", 4);
        container.AddChild(_manaLabel);
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
