namespace ArenaSurvivor;

using Godot;

public partial class Projectile : Node3D
{
    private Vector3 _direction;
    private ProjectileConfig _config;
    private float _lifetime;
    private MeshInstance3D _mesh;

    public void Initialize(Vector3 startPosition, Vector3 direction, ProjectileConfig config)
    {
        GlobalPosition = startPosition;
        _direction = direction.Normalized();
        _config = config;
        _lifetime = config.Lifetime;

        BuildVisual();
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        _lifetime -= dt;

        if (_lifetime <= 0f)
        {
            QueueFree();
            return;
        }

        GlobalPosition += _direction * _config.Speed * dt;
        CheckHit();
    }

    private void CheckHit()
    {
        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not Enemy enemy || enemy.IsDead) continue;

            // Compare on XZ plane, ignoring Y offset from capsule center
            var enemyPosFlat = enemy.GlobalPosition with { Y = 0 };
            var projPosFlat = GlobalPosition with { Y = 0 };
            if (enemyPosFlat.DistanceTo(projPosFlat) > 1.5f) continue;

            enemy.TakeDamage(_config.Damage, GlobalPosition, 3.0f);
            QueueFree();
            return;
        }
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
}
