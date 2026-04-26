namespace EscapeRoom;

using Godot;

[GlobalClass]
public partial class DoorInteractable : InteractableComponent
{
    [Export] public bool ConsumeKey { get; set; } = false;
    [Export] public string ConsumedItemId { get; set; } = "";
    [Export] public float OpenAngleDegrees { get; set; } = -90f;
    [Export] public float OpenDuration { get; set; } = 0.6f;
    [Export] private NodePath _pivotPath = new();
    [Export] private NodePath _bodyPath = new();

    private Node3D _pivot;
    private CollisionObject3D _body;
    private bool _isOpen;

    public override void _Ready()
    {
        if (string.IsNullOrEmpty(Verb) || Verb == "Use") Verb = "Open";
        if (string.IsNullOrEmpty(TargetName)) TargetName = "Door";

        if (_pivotPath != null && !_pivotPath.IsEmpty)
            _pivot = GetNodeOrNull<Node3D>(_pivotPath);
        if (_bodyPath != null && !_bodyPath.IsEmpty)
            _body = GetNodeOrNull<CollisionObject3D>(_bodyPath);

        if (_pivot == null)
            GD.PushWarning($"{nameof(DoorInteractable)} '{Name}': '_pivotPath' unresolved; door will not animate.");
        if (_body == null)
            GD.PushWarning($"{nameof(DoorInteractable)} '{Name}': '_bodyPath' unresolved; door collision will remain after opening.");
    }

    public override bool CanBeInteractedBy(IInteractor interactor)
    {
        return !_isOpen && base.CanBeInteractedBy(interactor);
    }

    protected override InteractionResult OnInteract(IInteractor interactor)
    {
        if (_isOpen) return InteractionResult.Rejected;

        if (ConsumeKey && !string.IsNullOrEmpty(ConsumedItemId))
            InventorySystem.Instance?.RemoveItemById(ConsumedItemId);

        _isOpen = true;
        Enabled = false;
        Open();
        return InteractionResult.Success;
    }

    private void Open()
    {
        if (_pivot != null)
        {
            var tween = CreateTween();
            tween.SetEase(Tween.EaseType.Out);
            tween.SetTrans(Tween.TransitionType.Cubic);
            var target = _pivot.RotationDegrees + new Vector3(0, OpenAngleDegrees, 0);
            tween.TweenProperty(_pivot, "rotation_degrees", target, OpenDuration);
        }

        if (_body != null)
            _body.SetCollisionLayerValue(3, false);

        SetCollisionLayerValue(2, false);
    }
}
