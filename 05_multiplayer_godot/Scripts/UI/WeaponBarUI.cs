namespace SwarmSurvivor;

using Godot;

public partial class WeaponBarUI : Control
{
    private static readonly Color AccentColor = new Color(0.78f, 0.63f, 0.38f);
    private static readonly Color SubTextColor = new Color(0.85f, 0.82f, 0.74f);

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        BuildWeaponBar();
    }

    // ========== Weapon Bar (right bottom, above mana orb) ==========
    private void BuildWeaponBar()
    {
        BuildSlotBar(
            anchor: LayoutPreset.BottomRight,
            offsetX: -424f,
            offsetY: -88f,
            keys: new[] { "1", "2", "3", "4" });
    }

    private void BuildSlotBar(LayoutPreset anchor, float offsetX, float offsetY, string[] keys)
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
            label.SetAnchorsPreset(LayoutPreset.FullRect);
            label.AddThemeFontSizeOverride("font_size", 20);
            label.AddThemeColorOverride("font_color", SubTextColor);
            slot.AddChild(label);
        }
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
}
