namespace EscapeRoom;

using Godot;

[GlobalClass]
public partial class PlayerInteractor : Node, IInteractor
{
    [Export] public string InteractAction { get; set; } = "interact";
    [Export] private NodePath _rayPath = new();
    [Export] private NodePath _bodyPath = new();
    [Export] private InteractionContextEventChannel _onContextChanged;

    private RayCast3D _ray;
    private Node3D _body;
    private IInteractable _currentTarget;
    private string _glyph = "E";
    private float _holdProgress;
    private bool _isHolding;
    private InteractionContext _lastContext;

    public Node3D Body => _body;
    public Vector3 GlobalPosition => _body != null ? _body.GlobalPosition : Vector3.Zero;

    public override void _Ready()
    {
        if (_rayPath == null || _rayPath.IsEmpty)
        {
            GD.PushError($"{nameof(PlayerInteractor)}: '_rayPath' is empty.");
            return;
        }
        _ray = GetNodeOrNull<RayCast3D>(_rayPath);
        if (_ray == null)
            GD.PushError($"{nameof(PlayerInteractor)}: '_rayPath' could not be resolved as RayCast3D.");

        _body = (_bodyPath != null && !_bodyPath.IsEmpty)
            ? GetNodeOrNull<Node3D>(_bodyPath)
            : GetParent<Node3D>();
        if (_body == null)
            GD.PushWarning($"{nameof(PlayerInteractor)}: body not resolved; range/spatial validators will degrade.");

        _glyph = InputGlyphResolver.Resolve(InteractAction);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_ray == null) return;
        _ray.ForceRaycastUpdate();

        var next = ResolveTarget();
        SwapFocus(next);

        if (_currentTarget != null && _currentTarget.CanBeInteractedBy(this) && _currentTarget is InteractableComponent comp && comp.RequiresHold && _isHolding)
        {
            _holdProgress = Mathf.Clamp(_holdProgress + (float)delta / Mathf.Max(0.0001f, comp.HoldDuration), 0f, 1f);
            EmitContext();
            if (_holdProgress >= 1f)
            {
                _holdProgress = 0f;
                _isHolding = false;
                _currentTarget.Interact(this);
                _ray.ForceRaycastUpdate();
                SwapFocus(ResolveTarget());
                EmitContext();
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsAction(InteractAction)) return;

        if (@event.IsActionPressed(InteractAction))
        {
            if (_currentTarget == null || !_currentTarget.CanBeInteractedBy(this)) return;

            if (_currentTarget is InteractableComponent comp && comp.RequiresHold)
            {
                _isHolding = true;
                _holdProgress = 0f;
                EmitContext();
                return;
            }

            _currentTarget.Interact(this);
            if (_ray != null)
            {
                _ray.ForceRaycastUpdate();
                SwapFocus(ResolveTarget());
            }
            EmitContext();
            return;
        }

        if (@event.IsActionReleased(InteractAction) && _isHolding)
        {
            _isHolding = false;
            _holdProgress = 0f;
            EmitContext();
        }
    }

    private IInteractable ResolveTarget()
    {
        if (_ray == null || !_ray.IsColliding()) return null;
        var collider = _ray.GetCollider();
        if (collider is not IInteractable candidate) return null;
        if (collider is GodotObject go && !GodotObject.IsInstanceValid(go)) return null;
        if (collider is Node node && node.IsQueuedForDeletion()) return null;
        return candidate;
    }

    private void SwapFocus(IInteractable next)
    {
        if (next == _currentTarget) return;

        if (_currentTarget != null)
        {
            try { _currentTarget.OnFocusExit(this); }
            catch (System.Exception e) { GD.PushError($"OnFocusExit threw: {e.Message}"); }
        }

        _currentTarget = next;
        _holdProgress = 0f;
        _isHolding = false;

        if (_currentTarget != null)
        {
            try { _currentTarget.OnFocusEnter(this); }
            catch (System.Exception e) { GD.PushError($"OnFocusEnter threw: {e.Message}"); }
        }

        EmitContext();
    }

    private void EmitContext()
    {
        if (_onContextChanged == null) return;

        InteractionContext ctx;
        if (_currentTarget == null)
        {
            ctx = new InteractionContext { Glyph = _glyph };
        }
        else
        {
            ctx = _currentTarget.BuildContext(this, _glyph, _holdProgress);
        }

        if (ContextEquals(ctx, _lastContext)) return;
        _lastContext = ctx;
        _onContextChanged.Raise(ctx);
    }

    private static bool ContextEquals(InteractionContext a, InteractionContext b)
    {
        if (a == null || b == null) return false;
        return a.Verb == b.Verb
            && a.TargetName == b.TargetName
            && a.BlockReason == b.BlockReason
            && a.Glyph == b.Glyph
            && a.RequiresHold == b.RequiresHold
            && Mathf.IsEqualApprox(a.HoldProgress, b.HoldProgress);
    }
}
