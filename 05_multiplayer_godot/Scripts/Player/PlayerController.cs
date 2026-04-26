namespace SwarmSurvivor;

using Godot;
using ReactiveSO;

public partial class PlayerController : CharacterBody3D, IDamageable
{
    [Export] public PlayerConfig Config { get; set; }
    [Export] private IntVariable _health;
    [Export] private IntEventChannel _onPlayerDamaged;
    [Export] private VoidEventChannel _onPlayerDied;

    private int _currentHealth;
    private float _invincibleTimer;
    private const float InvincibleDuration = 0.5f;

    public override void _Ready()
    {
        if (Config == null)
        {
            GD.PrintErr("PlayerController: Config is null");
            return;
        }

        _currentHealth = Config.MaxHealth;
        if (_health != null) _health.Value = _currentHealth;

        var capsule = new CapsuleMesh { Radius = 0.5f, Height = 1.2f };
        var meshInstance = new MeshInstance3D { Mesh = capsule };
        meshInstance.Position = new Vector3(0f, 0.6f, 0f);
        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.2f, 0.6f, 1f),
            Emission = new Color(0.1f, 0.3f, 0.5f),
            EmissionEnabled = true,
        };
        meshInstance.MaterialOverride = material;
        AddChild(meshInstance);

        var collision = new CollisionShape3D();
        var shape = new CapsuleShape3D { Radius = 0.5f, Height = 1.2f };
        collision.Shape = shape;
        collision.Position = new Vector3(0f, 0.6f, 0f);
        AddChild(collision);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Config == null) return;

        var input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        var direction = new Vector3(input.X, 0f, input.Y);

        if (direction.LengthSquared() > 0.01f)
        {
            direction = direction.Normalized();
            Velocity = direction * Config.MoveSpeed;

            var lookTarget = GlobalPosition + direction;
            LookAt(lookTarget, Vector3.Up);
        }
        else
        {
            Velocity = Vector3.Zero;
        }

        MoveAndSlide();

        if (_invincibleTimer > 0f)
        {
            _invincibleTimer -= (float)delta;
        }
    }

    public void TakeDamage(int damage)
    {
        if (_invincibleTimer > 0f) return;

        _currentHealth -= damage;
        if (_currentHealth < 0) _currentHealth = 0;
        if (_health != null) _health.Value = _currentHealth;

        _invincibleTimer = InvincibleDuration;
        _onPlayerDamaged?.Raise(_currentHealth);

        if (_currentHealth <= 0)
        {
            _onPlayerDied?.Raise();
        }
    }
}
