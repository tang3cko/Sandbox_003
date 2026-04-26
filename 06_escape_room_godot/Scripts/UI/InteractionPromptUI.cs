namespace EscapeRoom;

using Godot;

[GlobalClass]
public partial class InteractionPromptUI : Control
{
    [Export] private NodePath _labelPath = new();
    [Export] private NodePath _holdBarPath = new();
    [Export] private InteractionContextEventChannel _onContextChanged;

    private Label _label;
    private ProgressBar _holdBar;

    public override void _Ready()
    {
        if (_labelPath == null || _labelPath.IsEmpty)
        {
            GD.PushError($"{nameof(InteractionPromptUI)}: '_labelPath' is empty.");
            return;
        }
        _label = GetNodeOrNull<Label>(_labelPath);
        if (_label == null)
        {
            GD.PushError($"{nameof(InteractionPromptUI)}: '_labelPath' did not resolve to a Label.");
            return;
        }

        if (_holdBarPath != null && !_holdBarPath.IsEmpty)
            _holdBar = GetNodeOrNull<ProgressBar>(_holdBarPath);

        if (_onContextChanged != null)
            _onContextChanged.Raised += OnContext;
        else
            GD.PushWarning($"{nameof(InteractionPromptUI)}: '_onContextChanged' is not assigned.");

        OnContext(null);
    }

    public override void _ExitTree()
    {
        if (_onContextChanged != null)
            _onContextChanged.Raised -= OnContext;
    }

    private void OnContext(InteractionContext ctx)
    {
        if (_label == null) return;

        if (ctx == null || !ctx.IsVisible)
        {
            _label.Visible = false;
            _label.Text = "";
            if (_holdBar != null) _holdBar.Visible = false;
            return;
        }

        var glyph = string.IsNullOrEmpty(ctx.Glyph) ? "" : $"[{ctx.Glyph}] ";
        var body = ctx.IsBlocked
            ? ctx.BlockReason
            : (string.IsNullOrEmpty(ctx.TargetName) ? ctx.Verb : $"{ctx.Verb} {ctx.TargetName}");

        _label.Text = $"{glyph}{body}";
        _label.Modulate = ctx.IsBlocked ? ctx.BlockedColor : ctx.TextColor;
        _label.Visible = true;

        if (_holdBar != null)
        {
            var showBar = ctx.RequiresHold && ctx.HoldProgress > 0f && ctx.HoldProgress < 1f;
            _holdBar.Visible = showBar;
            _holdBar.MaxValue = 1.0;
            _holdBar.Value = ctx.HoldProgress;
        }
    }
}
