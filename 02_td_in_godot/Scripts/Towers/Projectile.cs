namespace TowerDefense;

using Godot;

public partial class Projectile : Node3D
{
    private Node3D _target;
    private Vector3 _lastTargetPosition;
    private int _damage;
    private float _speed;
    private float _areaDamageRadius;
    private float _slowAmount;
    private float _slowDuration;
    private MeshInstance3D _mesh;

    public void Initialize(Vector3 startPosition, Node3D target, int damage, float speed,
        float areaDamageRadius, float slowAmount, float slowDuration, Color color)
    {
        GlobalPosition = startPosition;
        _target = target;
        _lastTargetPosition = target.GlobalPosition;
        _damage = damage;
        _speed = speed;
        _areaDamageRadius = areaDamageRadius;
        _slowAmount = slowAmount;
        _slowDuration = slowDuration;

        _mesh = new MeshInstance3D();
        var sphere = new SphereMesh();
        sphere.Radius = 0.15f;
        sphere.Height = 0.3f;
        _mesh.Mesh = sphere;
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = color;
        mat.EmissionEnabled = true;
        mat.Emission = color;
        mat.EmissionEnergyMultiplier = 2.0f;
        _mesh.MaterialOverride = mat;
        AddChild(_mesh);
    }

    public override void _Process(double delta)
    {
        if (IsInstanceValid(_target))
            _lastTargetPosition = _target.GlobalPosition;

        var direction = (_lastTargetPosition - GlobalPosition).Normalized();
        GlobalPosition += direction * _speed * (float)delta;

        if (GlobalPosition.DistanceTo(_lastTargetPosition) < 0.5f)
        {
            Hit();
        }
    }

    private void Hit()
    {
        if (_areaDamageRadius > 0)
        {
            // Area damage
            foreach (var node in GetTree().GetNodesInGroup("enemies"))
            {
                if (node is Enemy enemy && !enemy.IsDead)
                {
                    if (enemy.GlobalPosition.DistanceTo(GlobalPosition) <= _areaDamageRadius)
                    {
                        enemy.TakeDamage(_damage);
                        if (_slowAmount > 0)
                            enemy.ApplySlow(_slowAmount, _slowDuration);
                    }
                }
            }
        }
        else
        {
            // Single target
            if (IsInstanceValid(_target) && _target is Enemy enemy && !enemy.IsDead)
            {
                enemy.TakeDamage(_damage);
                if (_slowAmount > 0)
                    enemy.ApplySlow(_slowAmount, _slowDuration);
            }
        }

        QueueFree();
    }
}
