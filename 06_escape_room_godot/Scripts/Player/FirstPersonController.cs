namespace EscapeRoom;

using Godot;

[GlobalClass]
public partial class FirstPersonController : CharacterBody3D
{
    [Export] public float MoveSpeed { get; set; } = 4.0f;
    [Export] public float MouseSensitivity { get; set; } = 0.0025f;
    [Export] public float Gravity { get; set; } = 9.8f;
    [Export] private NodePath _headPath = new();

    private Node3D _head;
    private float _pitch;

    public override void _Ready()
    {
        AddToGroup("player");

        _head = ResolveOrError<Node3D>(_headPath, nameof(_headPath));
        if (_head == null) return;

        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_head == null) return;

        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            RotateY(-motion.Relative.X * MouseSensitivity);
            _pitch = Mathf.Clamp(
                _pitch - motion.Relative.Y * MouseSensitivity,
                -Mathf.Pi / 2f + 0.01f,
                Mathf.Pi / 2f - 0.01f);
            _head.Rotation = new Vector3(_pitch, 0f, 0f);
            return;
        }

        if (@event.IsActionPressed("toggle_mouse"))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Captured;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        var direction = Transform.Basis * new Vector3(input.X, 0f, input.Y);
        if (direction.LengthSquared() > 0.01f)
            direction = direction.Normalized();

        var velocity = Velocity;
        velocity.X = direction.X * MoveSpeed;
        velocity.Z = direction.Z * MoveSpeed;
        velocity.Y = IsOnFloor() ? 0f : velocity.Y - Gravity * (float)delta;
        Velocity = velocity;

        MoveAndSlide();
    }

    private T ResolveOrError<T>(NodePath path, string fieldName) where T : class
    {
        if (path == null || path.IsEmpty)
        {
            GD.PushError($"{nameof(FirstPersonController)}: '{fieldName}' is empty.");
            return null;
        }
        var node = GetNodeOrNull(path) as T;
        if (node == null)
            GD.PushError($"{nameof(FirstPersonController)}: '{fieldName}' could not be resolved as {typeof(T).Name}.");
        return node;
    }
}
