namespace ArenaSurvivor;

using System.Linq;
using Godot;
using ReactiveSO;

public partial class PlayerCombat : Node3D
{
    [Export] public WeaponConfig Weapon { get; set; }
    [Export] public ProjectileConfig ProjectileConfig { get; set; }
    [Export] private VoidEventChannel _onEnemyKilled;
    [Export] private VoidEventChannel _onPlayerAttacked;
    [Export] private Node3DRuntimeSet _activeEnemies;

    private PlayerController _player;
    private PackedScene _projectileScene;
    private float _hitStopTimer;
    private bool _isHitStopped;

    public override void _Ready()
    {
        _player = GetParent<PlayerController>();
        _projectileScene = GD.Load<PackedScene>("res://Scenes/Projectile.tscn");
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

        if (_activeEnemies == null) return;

        // Snapshot to prevent Collection modified exception
        var enemies = _activeEnemies.Items.ToList();

        foreach (var item in enemies)
        {
            if (item is not Node3D node || !GodotObject.IsInstanceValid(node)) continue;
            if (node is not Enemy enemy) continue;

            var dist = _player.GlobalPosition.DistanceTo(enemy.GlobalPosition);
            if (dist > Weapon.Range) continue;

            var toEnemy = (enemy.GlobalPosition - _player.GlobalPosition).Normalized();
            var forward = -_player.GlobalTransform.Basis.Z;
            if (toEnemy.Dot(forward) < 0.3f) continue;

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
