namespace EscapeRoom;

using Godot;

[GlobalClass]
public partial class InteractionContext : Resource
{
    [Export] public string Verb { get; set; } = "";
    [Export] public string TargetName { get; set; } = "";
    [Export] public string Glyph { get; set; } = "";
    [Export] public string BlockReason { get; set; } = "";
    [Export] public Color TextColor { get; set; } = Colors.White;
    [Export] public Color BlockedColor { get; set; } = new Color(1f, 0.5f, 0.5f, 1f);
    [Export] public bool RequiresHold { get; set; } = false;
    [Export] public float HoldProgress { get; set; } = 0f;
    [Export] public Texture2D Icon { get; set; }

    public bool IsBlocked => !string.IsNullOrEmpty(BlockReason);
    public bool IsVisible => !string.IsNullOrEmpty(Verb) || IsBlocked;
}
