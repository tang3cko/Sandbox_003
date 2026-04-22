namespace ArenaSurvivor;

using Godot;

public partial class Projectile : Area3D
{
    private Vector3 _direction;
    private ProjectileConfig _config;
    private float _lifetime;
    private MeshInstance3D _mesh;
    private bool _hit;

    public void Initialize(Vector3 startPosition, Vector3 direction, ProjectileConfig config)
    {
        GlobalPosition = startPosition;
        _direction = direction.Normalized();
        _config = config;
        _lifetime = config.Lifetime;

        // Layer 6 = Projectile, detect Layer 5 = EnemyHitbox
        CollisionLayer = 1 << 5;
        CollisionMask = 1 << 4;
        Monitoring = true;

        BuildVisual();
        BuildCollision();

        AreaEntered += OnAreaEntered;
    }

    public override void _Process(double delta)
    {
        if (_hit) return;

        var dt = (float)delta;
        _lifetime -= dt;

        if (_lifetime <= 0f)
        {
            QueueFree();
            return;
        }

        GlobalPosition += _direction * _config.Speed * dt;
    }

    private void OnAreaEntered(Area3D area)
    {
        if (_hit) return;
        if (area.GetParent() is not Enemy enemy || enemy.IsDead) return;

        _hit = true;
        enemy.TakeDamage(_config.Damage, GlobalPosition, 3.0f);
        QueueFree();
    }

    private void BuildVisual()
    {
        _mesh = new MeshInstance3D();
        var sphere = new SphereMesh();
        sphere.Radius = _config.Scale;
        sphere.Height = _config.Scale * 2f;
        _mesh.Mesh = sphere;

        var mat = new StandardMaterial3D();
        mat.AlbedoColor = _config.Color;
        mat.EmissionEnabled = true;
        mat.Emission = _config.Color;
        mat.EmissionEnergyMultiplier = 3.0f;
        _mesh.MaterialOverride = mat;
        AddChild(_mesh);
    }

    private void BuildCollision()
    {
        var shape = new CollisionShape3D();
        var sphere = new SphereShape3D();
        sphere.Radius = _config.Scale * 1.5f;
        shape.Shape = sphere;
        AddChild(shape);
    }
}
