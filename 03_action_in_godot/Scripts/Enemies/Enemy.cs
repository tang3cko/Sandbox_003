namespace ArenaSurvivor;

using Godot;
using ReactiveSO;

public partial class Enemy : Node3D
{
    [Export] public EnemyConfig Config { get; set; }
    [Export] private Node3DRuntimeSet _activeEnemies;
    [Export] private VoidEventChannel _onEnemyKilled;
    [Export] private IntEventChannel _onScoreEarned;

    private int _currentHealth;
    private NavigationAgent3D _navAgent;
    private Node3D _target;
    private float _attackCooldown;
    private MeshInstance3D _mesh;
    private EnemyAnimator _animator;
    private bool _isDead;
    private float _knockbackX;
    private float _knockbackZ;

    public bool IsDead => _isDead;

    public void Initialize(Node3D target)
    {
        _target = target;
        _currentHealth = Config.MaxHealth;
        _isDead = false;

        _navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");

        BuildVisual();

        _animator = new EnemyAnimator();
        _animator.Name = "EnemyAnimator";
        AddChild(_animator);
        _animator.Initialize(_mesh);

        _activeEnemies?.Add(this);
    }

    public override void _Process(double delta)
    {
        if (_isDead) return;

        var dt = (float)delta;
        _attackCooldown -= dt;

        // Knockback decay
        _knockbackX = Mathf.MoveToward(_knockbackX, 0, 20f * dt);
        _knockbackZ = Mathf.MoveToward(_knockbackZ, 0, 20f * dt);

        if (_target == null || !GodotObject.IsInstanceValid(_target)) return;

        var distToTarget = GlobalPosition.DistanceTo(_target.GlobalPosition);

        if (distToTarget <= Config.AttackRange)
        {
            TryAttack();
            // Apply knockback only
            GlobalPosition += new Vector3(_knockbackX * dt, 0, _knockbackZ * dt);
        }
        else
        {
            _navAgent.TargetPosition = _target.GlobalPosition;
            var nextPos = _navAgent.GetNextPathPosition();
            var toNext = nextPos - GlobalPosition;
            toNext.Y = 0;

            if (toNext.LengthSquared() > 0.001f)
            {
                var direction = toNext.Normalized();
                var moveAmount = Config.MoveSpeed * dt;
                GlobalPosition += new Vector3(
                    direction.X * moveAmount + _knockbackX * dt,
                    0,
                    direction.Z * moveAmount + _knockbackZ * dt
                );

                // Face movement direction
                var lookTarget = GlobalPosition + new Vector3(direction.X, 0, direction.Z);
                if (GlobalPosition.DistanceSquaredTo(lookTarget) > 0.01f)
                    LookAt(lookTarget, Vector3.Up);
            }
        }

        // Keep on ground
        GlobalPosition = GlobalPosition with { Y = 0 };
    }

    public void TakeDamage(int damage, Vector3 sourcePosition, float knockbackForce)
    {
        if (_isDead) return;

        _currentHealth -= damage;

        // Knockback
        var knockbackDir = (GlobalPosition - sourcePosition).Normalized();
        var actualKnockback = knockbackForce * (1f - Config.KnockbackResistance);
        _knockbackX = knockbackDir.X * actualKnockback;
        _knockbackZ = knockbackDir.Z * actualKnockback;

        // Hit flash (material) + hit animation (squash via AnimationPlayer)
        if (_mesh?.MaterialOverride is StandardMaterial3D mat)
        {
            mat.AlbedoColor = Colors.White;
            var tween = CreateTween();
            tween.TweenProperty(mat, "albedo_color", Config.Color, 0.15f);
        }
        _animator?.PlayHit();

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void TryAttack()
    {
        if (_attackCooldown > 0f) return;
        _attackCooldown = Config.AttackCooldown;

        if (_target is PlayerController player)
        {
            player.TakeDamage(Config.Damage);
        }
    }

    private void Die()
    {
        _isDead = true;
        _activeEnemies?.Remove(this);
        _onEnemyKilled?.Raise();
        _onScoreEarned?.Raise(Config.ScoreReward);

        _animator?.PlayDeath();

        // QueueFree after death animation
        var tween = CreateTween();
        tween.TweenInterval(0.5f);
        tween.TweenCallback(Callable.From(QueueFree));
    }

    private void BuildVisual()
    {
        _mesh = new MeshInstance3D();
        var capsule = new CapsuleMesh();
        capsule.Radius = 0.35f * Config.Scale;
        capsule.Height = 1.6f * Config.Scale;
        _mesh.Mesh = capsule;
        _mesh.Position = new Vector3(0, 0.8f * Config.Scale, 0);

        var mat = new StandardMaterial3D();
        mat.AlbedoColor = Config.Color;
        _mesh.MaterialOverride = mat;
        AddChild(_mesh);
    }

    public override void _ExitTree()
    {
        _activeEnemies?.Remove(this);
    }
}
