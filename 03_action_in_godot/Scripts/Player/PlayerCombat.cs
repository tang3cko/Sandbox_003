namespace ArenaSurvivor;

using Godot;
using ReactiveSO;

public partial class PlayerCombat : Node3D
{
    [Export] public WeaponConfig Weapon { get; set; }
    [Export] public ProjectileConfig ProjectileConfig { get; set; }
    [Export] private VoidEventChannel _onEnemyKilled;
    [Export] private VoidEventChannel _onPlayerAttacked;

    private PlayerController _player;
    private PackedScene _projectileScene;
    private float _hitStopTimer;
    private bool _isHitStopped;
    private Area3D _meleeArea;

    public override void _Ready()
    {
        _player = GetParent<PlayerController>();
        _projectileScene = GD.Load<PackedScene>("res://Scenes/Projectile.tscn");
        _meleeArea = CreateMeleeArea();
    }

    private Area3D CreateMeleeArea()
    {
        var area = new Area3D();
        area.Name = "MeleeArea";
        // Layer 4 = PlayerHitbox, detect Layer 5 = EnemyHitbox
        area.CollisionLayer = 1 << 3;
        area.CollisionMask = 1 << 4;
        area.Monitoring = true;

        var shape = new CollisionShape3D();
        var box = new BoxShape3D();
        box.Size = new Vector3(Weapon?.Range ?? 2.5f, 1.5f, Weapon?.Range ?? 2.5f);
        shape.Shape = box;
        shape.Position = new Vector3(0, 0.9f, -(Weapon?.Range ?? 2.5f) * 0.5f);
        area.AddChild(shape);

        _player.GetNode("WeaponPivot").AddChild(area);
        return area;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("attack"))
        {
            TryMeleeAttack();
        }
        else if (@event.IsActionPressed("dodge"))
        {
            _player.HandleDodgeInput();
        }
        else if (@event.IsActionPressed("shoot"))
        {
            TryShoot();
        }
    }

    public override void _Process(double delta)
    {
        if (_isHitStopped)
        {
            _hitStopTimer -= (float)delta;
            if (_hitStopTimer <= 0f)
            {
                _isHitStopped = false;
                Engine.TimeScale = 1.0;
            }
        }
    }

    private void TryMeleeAttack()
    {
        var result = _player.HandleAttackInput(Weapon.BaseDamage);
        if (result.Damage <= 0) return;

        _onPlayerAttacked?.Raise();

        if (_meleeArea == null) return;

        var overlapping = _meleeArea.GetOverlappingAreas();
        foreach (var area in overlapping)
        {
            if (area.GetParent() is not Enemy enemy || enemy.IsDead) continue;

            enemy.TakeDamage(result.Damage, _player.GlobalPosition, Weapon.KnockbackForce);
            ApplyHitStop(Weapon.HitStopDuration);

            if (result.IsComboFinisher)
            {
                ApplyHitStop(Weapon.HitStopDuration * 3f);
            }
        }
    }

    private void TryShoot()
    {
        if (_player.IsAttacking || _player.IsDodging) return;
        if (_projectileScene == null || ProjectileConfig == null) return;

        var projectile = _projectileScene.Instantiate<Projectile>();
        GetTree().Root.AddChild(projectile);

        var spawnPos = _player.GlobalPosition + Vector3.Up * 1.2f + (-_player.GlobalTransform.Basis.Z * 0.8f);
        var direction = -_player.GlobalTransform.Basis.Z;

        projectile.Initialize(spawnPos, direction, ProjectileConfig);
    }

    private void ApplyHitStop(float duration)
    {
        _isHitStopped = true;
        _hitStopTimer = duration;
        Engine.TimeScale = 0.05;
    }
}
