namespace TowerDefense;

using System.Linq;
using Godot;
using ReactiveSO;

public partial class Tower : Node3D
{
    [Export] public TowerConfig Config { get; set; }
    [Export] private Node3DRuntimeSet _activeEnemies;
    [Export] private Node3DRuntimeSet _placedTowers;

    private float _fireCooldown;
    private Node3D _target;
    private MeshInstance3D _baseMesh;
    private MeshInstance3D _barrelMesh;
    private PackedScene _projectileScene;

    public override void _Ready()
    {
        _projectileScene = GD.Load<PackedScene>("res://Scenes/Projectile.tscn");

        // Build visual
        _baseMesh = new MeshInstance3D();
        var box = new BoxMesh();
        box.Size = new Vector3(1.5f, 0.8f, 1.5f);
        _baseMesh.Mesh = box;
        _baseMesh.Position = new Vector3(0, 0.4f, 0);
        var baseMat = new StandardMaterial3D();
        baseMat.AlbedoColor = Config.Color;
        _baseMesh.MaterialOverride = baseMat;
        AddChild(_baseMesh);

        _barrelMesh = new MeshInstance3D();
        var cylinder = new CylinderMesh();
        cylinder.TopRadius = 0.15f;
        cylinder.BottomRadius = 0.2f;
        cylinder.Height = 1.2f;
        _barrelMesh.Mesh = cylinder;
        _barrelMesh.Position = new Vector3(0, 1.2f, 0);
        var barrelMat = new StandardMaterial3D();
        barrelMat.AlbedoColor = Config.Color.Darkened(0.3f);
        _barrelMesh.MaterialOverride = barrelMat;
        AddChild(_barrelMesh);

        _placedTowers?.Add(this);
        _fireCooldown = 0f;
    }

    public override void _Process(double delta)
    {
        _fireCooldown -= (float)delta;

        _target = FindClosestEnemy();

        if (_target != null)
        {
            // Rotate barrel toward target (Y-axis only)
            var lookPos = new Vector3(_target.GlobalPosition.X, _barrelMesh.GlobalPosition.Y, _target.GlobalPosition.Z);
            if (_barrelMesh.GlobalPosition.DistanceSquaredTo(lookPos) > 0.01f)
                _barrelMesh.LookAt(lookPos, Vector3.Up);

            if (_fireCooldown <= 0f)
            {
                Fire();
                _fireCooldown = 1.0f / Config.FireRate;
            }
        }
    }

    private Node3D FindClosestEnemy()
    {
        if (_activeEnemies == null || _activeEnemies.Count == 0)
            return null;

        Node3D closest = null;
        float closestDist = Config.Range * Config.Range;

        foreach (var item in _activeEnemies.Items)
        {
            if (item is not Node3D node || !IsInstanceValid(node))
                continue;
            var dist = GlobalPosition.DistanceSquaredTo(node.GlobalPosition);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = node;
            }
        }
        return closest;
    }

    private void Fire()
    {
        if (_target == null || _projectileScene == null) return;

        var projectile = _projectileScene.Instantiate<Projectile>();
        GetTree().Root.AddChild(projectile);
        projectile.Initialize(
            _barrelMesh.GlobalPosition + Vector3.Up * 0.5f,
            _target,
            Config.Damage,
            Config.ProjectileSpeed,
            Config.AreaDamageRadius,
            Config.SlowAmount,
            Config.SlowDuration,
            Config.Color
        );
    }

    public override void _ExitTree()
    {
        _placedTowers?.Remove(this);
    }
}
