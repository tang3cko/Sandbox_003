namespace TowerDefense;

using Godot;
using ReactiveSO;

public partial class Enemy : Node3D
{
    [Export] public EnemyConfig Config { get; set; }

    // ReactiveSO resources (assigned in scene/inspector)
    [Export] private Node3DRuntimeSet _activeEnemies;
    [Export] private VoidEventChannel _onEnemyKilled;
    [Export] private VoidEventChannel _onEnemyReachedEnd;
    [Export] private IntEventChannel _onGoldEarned;

    private int _currentHealth;
    private Vector3[] _waypoints;
    private int _waypointIndex;
    private float _speedMultiplier = 1.0f;
    private float _slowTimer;
    private MeshInstance3D _mesh;

    public bool IsDead => _currentHealth <= 0;

    public void Initialize(Vector3[] waypoints)
    {
        _waypoints = waypoints;
        _waypointIndex = 0;
        _currentHealth = Config.MaxHealth;
        _speedMultiplier = 1.0f;
        _slowTimer = 0f;

        Position = waypoints[0];

        // Create visual mesh (capsule)
        _mesh = new MeshInstance3D();
        var capsule = new CapsuleMesh();
        capsule.Radius = 0.3f * Config.Scale;
        capsule.Height = 1.0f * Config.Scale;
        _mesh.Mesh = capsule;

        var material = new StandardMaterial3D();
        material.AlbedoColor = Config.Color;
        _mesh.MaterialOverride = material;
        AddChild(_mesh);

        // Register to runtime set
        _activeEnemies?.Add(this);
    }

    public override void _Process(double delta)
    {
        if (IsDead || _waypoints == null || _waypointIndex >= _waypoints.Length)
            return;

        // Update slow
        if (_slowTimer > 0)
        {
            _slowTimer -= (float)delta;
            if (_slowTimer <= 0)
                _speedMultiplier = 1.0f;
        }

        // Move toward current waypoint
        var target = _waypoints[_waypointIndex];
        var speed = Config.Speed * _speedMultiplier * (float)delta;
        var direction = (target - GlobalPosition).Normalized();

        // Face movement direction (Y-axis rotation only)
        if (direction.LengthSquared() > 0.001f)
        {
            var lookTarget = GlobalPosition + new Vector3(direction.X, 0, direction.Z);
            if (GlobalPosition.DistanceSquaredTo(lookTarget) > 0.001f)
                LookAt(lookTarget, Vector3.Up);
        }

        GlobalPosition = GlobalPosition.MoveToward(target, speed);

        if (GlobalPosition.DistanceTo(target) < 0.1f)
        {
            _waypointIndex++;
            if (_waypointIndex >= _waypoints.Length)
            {
                ReachEnd();
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        _currentHealth -= damage;

        // Flash effect
        if (_mesh?.MaterialOverride is StandardMaterial3D mat)
        {
            mat.AlbedoColor = Colors.White;
            var tween = CreateTween();
            tween.TweenProperty(mat, "albedo_color", Config.Color, 0.15f);
        }

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    public void ApplySlow(float amount, float duration)
    {
        _speedMultiplier = 1.0f - Mathf.Clamp(amount, 0f, 0.8f);
        _slowTimer = duration;
    }

    private void Die()
    {
        _activeEnemies?.Remove(this);
        _onEnemyKilled?.Raise();
        _onGoldEarned?.Raise(Config.GoldReward);

        // Death animation
        var tween = CreateTween();
        tween.TweenProperty(this, "scale", Vector3.Zero, 0.3f);
        tween.TweenCallback(Callable.From(QueueFree));
    }

    private void ReachEnd()
    {
        _activeEnemies?.Remove(this);
        _onEnemyReachedEnd?.Raise();
        QueueFree();
    }

    public override void _ExitTree()
    {
        _activeEnemies?.Remove(this);
    }
}
