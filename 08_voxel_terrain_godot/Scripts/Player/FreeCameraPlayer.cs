using Godot;

namespace VoxelTerrain.Player;

public partial class FreeCameraPlayer : Node3D
{
    [Export] public float MoveSpeed { get; set; } = 12f;
    [Export] public float MouseSensitivity { get; set; } = 0.003f;

    private Camera3D _camera;
    private float _yaw;
    private float _pitch;

    public override void _Ready()
    {
        _camera = new Camera3D { Name = "Camera" };
        AddChild(_camera);
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _yaw -= motion.Relative.X * MouseSensitivity;
            _pitch -= motion.Relative.Y * MouseSensitivity;
            _pitch = Mathf.Clamp(_pitch, -Mathf.Pi / 2f + 0.01f, Mathf.Pi / 2f - 0.01f);
            Rotation = new Vector3(_pitch, _yaw, 0f);
        }
        if (@event.IsActionPressed("release_mouse"))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Captured;
        }
    }

    public override void _Process(double delta)
    {
        var input = new Vector3(
            Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left"),
            Input.GetActionStrength("move_up") - Input.GetActionStrength("move_down"),
            Input.GetActionStrength("move_back") - Input.GetActionStrength("move_forward"));
        if (input.LengthSquared() < 0.0001f) return;

        var basisDir = (Transform.Basis * input).Normalized();
        Position += basisDir * MoveSpeed * (float)delta;
    }

    public Camera3D Camera => _camera;
}
