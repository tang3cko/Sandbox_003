namespace SwarmSurvivor;

using System.Collections.Generic;
using Godot;

public partial class WeaponSystem : Node3D
{
    private PlayerController _player;
    private SwarmManager _swarmManager;
    private ProjectileManager _projectileManager;
    private AuraWeapon _auraWeapon;

    private readonly List<ActiveWeapon> _weapons = new();
    private float _orbitalAngle;

    private struct ActiveWeapon
    {
        public WeaponConfig Config;
        public float Timer;
        public float DamageMultiplier;
        public float SpeedMultiplier;
    }

    public override void _Ready()
    {
        _player = GetNode<PlayerController>("../Player");
        _swarmManager = GetNode<SwarmManager>("../SwarmManager");
        _projectileManager = GetNode<ProjectileManager>("../ProjectileManager");
        _auraWeapon = GetNode<AuraWeapon>("../AuraWeapon");
    }

    public void AddWeapon(WeaponConfig config)
    {
        _weapons.Add(new ActiveWeapon
        {
            Config = config,
            Timer = 0f,
            DamageMultiplier = 1f,
            SpeedMultiplier = 1f,
        });

        if (config.Type == WeaponType.Aura && _auraWeapon != null)
        {
            _auraWeapon.SetActive(true, config);
        }
    }

    public override void _Process(double delta)
    {
        if (_player == null || _swarmManager == null) return;

        float dt = (float)delta;
        float playerX = _player.GlobalPosition.X;
        float playerZ = _player.GlobalPosition.Z;

        _orbitalAngle += dt;

        for (int i = 0; i < _weapons.Count; i++)
        {
            var weapon = _weapons[i];
            weapon.Timer -= dt;

            if (weapon.Timer <= 0f)
            {
                FireWeapon(weapon, playerX, playerZ);
                weapon.Timer = 1f / (weapon.Config.FireRate * weapon.SpeedMultiplier);
            }

            _weapons[i] = weapon;
        }

        UpdateOrbitals(playerX, playerZ);
    }

    private void FireWeapon(ActiveWeapon weapon, float playerX, float playerZ)
    {
        int damage = (int)(weapon.Config.Damage * weapon.DamageMultiplier);

        switch (weapon.Config.Type)
        {
            case WeaponType.Orbital:
                int orbCount = _projectileManager?.GetOrbitalCount() ?? 0;
                if (orbCount > 0)
                {
                    for (int o = 0; o < orbCount; o++)
                    {
                        var (ox, oz) = _projectileManager.GetOrbitalPosition(o);
                        _swarmManager.DamageInRadius(ox, oz, 0.9f, damage);
                    }
                }
                else
                {
                    _swarmManager.DamageInRadius(playerX, playerZ,
                        weapon.Config.OrbitalRadius + 0.5f, damage);
                }
                break;

            case WeaponType.Projectile:
                var nearest = _swarmManager.GetNearestAlivePosition(playerX, playerZ);
                float dx = nearest.x - playerX;
                float dz = nearest.z - playerZ;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist > 0.1f && dist < weapon.Config.Range * 2f)
                {
                    float nx = dx / dist;
                    float nz = dz / dist;
                    for (int p = 0; p < weapon.Config.ProjectileCount; p++)
                    {
                        float angleOffset = weapon.Config.ProjectileCount > 1
                            ? (p - weapon.Config.ProjectileCount / 2f) * 0.2f
                            : 0f;
                        float cos = Mathf.Cos(angleOffset);
                        float sin = Mathf.Sin(angleOffset);
                        float dirX = nx * cos - nz * sin;
                        float dirZ = nx * sin + nz * cos;

                        _projectileManager?.SpawnProjectile(
                            playerX, playerZ, dirX, dirZ,
                            weapon.Config.ProjectileSpeed,
                            damage, weapon.Config.Range,
                            weapon.Config.ProjectileColor);
                    }
                }
                break;

            case WeaponType.Aura:
                _swarmManager.DamageInRadius(playerX, playerZ,
                    weapon.Config.Range, damage);
                break;
        }
    }

    private void UpdateOrbitals(float playerX, float playerZ)
    {
        int orbitalIndex = 0;
        for (int i = 0; i < _weapons.Count; i++)
        {
            if (_weapons[i].Config.Type != WeaponType.Orbital) continue;

            float angle = _orbitalAngle * _weapons[i].Config.OrbitalSpeed
                + orbitalIndex * Mathf.Pi * 2f / GetOrbitalCount();
            float radius = _weapons[i].Config.OrbitalRadius;
            float ox = playerX + Mathf.Cos(angle) * radius;
            float oz = playerZ + Mathf.Sin(angle) * radius;

            _projectileManager?.UpdateOrbital(orbitalIndex, ox, oz,
                _weapons[i].Config.ProjectileColor);
            orbitalIndex++;
        }

        _projectileManager?.SetOrbitalCount(orbitalIndex);
    }

    private int GetOrbitalCount()
    {
        int count = 0;
        for (int i = 0; i < _weapons.Count; i++)
        {
            if (_weapons[i].Config.Type == WeaponType.Orbital) count++;
        }
        return count > 0 ? count : 1;
    }

    public void ApplyDamageMultiplier(float multiplier)
    {
        for (int i = 0; i < _weapons.Count; i++)
        {
            var w = _weapons[i];
            w.DamageMultiplier *= multiplier;
            _weapons[i] = w;
        }
    }

    public void ApplyFireRateMultiplier(float multiplier)
    {
        for (int i = 0; i < _weapons.Count; i++)
        {
            var w = _weapons[i];
            w.SpeedMultiplier *= multiplier;
            _weapons[i] = w;
        }
    }
}
