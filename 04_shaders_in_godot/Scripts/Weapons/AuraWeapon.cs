namespace SwarmSurvivor;

using Godot;

public partial class AuraWeapon : Node3D
{
    private MeshInstance3D _visualMesh;
    private bool _active;
    private WeaponConfig _config;
    private PlayerController _player;

    public override void _Ready()
    {
        _active = false;
        _player = GetNode<PlayerController>("../Player");

        var quad = new QuadMesh { Size = new Vector2(1f, 1f) };
        _visualMesh = new MeshInstance3D
        {
            Mesh = quad,
            Rotation = new Vector3(-Mathf.Pi / 2f, 0f, 0f),
            Position = new Vector3(0f, 0.05f, 0f),
            Visible = false,
        };

        var material = new ShaderMaterial();
        var shader = GD.Load<Shader>("res://Shaders/player_aura.gdshader");
        if (shader != null)
        {
            material.Shader = shader;
        }

        _visualMesh.MaterialOverride = material;
        AddChild(_visualMesh);
    }

    public void SetActive(bool active, WeaponConfig config)
    {
        _active = active;
        _config = config;
        _visualMesh.Visible = active;

        if (active && config != null)
        {
            float diameter = config.Range * 2f;
            _visualMesh.Scale = new Vector3(diameter, diameter, diameter);
        }
    }

    public override void _Process(double delta)
    {
        if (!_active || _visualMesh == null || _player == null) return;

        _visualMesh.GlobalPosition = new Vector3(
            _player.GlobalPosition.X, 0.05f, _player.GlobalPosition.Z);
    }
}
