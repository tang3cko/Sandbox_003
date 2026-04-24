namespace SwarmSurvivor;

using System;
using Godot;
using ReactiveSO;

public partial class SwarmManager : Node3D
{
    [Export] public WaveConfig WaveConfig { get; set; }
    [Export] private VoidEventChannel _onEnemyKilled;
    [Export] private Vector3EventChannel _onEnemyKilledAt;
    [Export] private IntEventChannel _onPlayerDamaged;

    private const int MaxEntities = 600;

    private float[] _posX, _posZ;
    private float[] _velX, _velZ;
    private int[] _health;
    private int[] _enemyTypeIndex;
    private float[] _damageFlashTimer;
    private float[] _deathTimer;
    private int _activeCount;

    private SwarmRenderer _renderer;
    private PlayerController _player;
    private float _contactDamageTimer;
    private const float ContactDamageInterval = 1.0f;

    public int ActiveCount => _activeCount;

    public override void _Ready()
    {
        _posX = new float[MaxEntities];
        _posZ = new float[MaxEntities];
        _velX = new float[MaxEntities];
        _velZ = new float[MaxEntities];
        _health = new int[MaxEntities];
        _enemyTypeIndex = new int[MaxEntities];
        _damageFlashTimer = new float[MaxEntities];
        _deathTimer = new float[MaxEntities];
        _activeCount = 0;

        _renderer = GetNode<SwarmRenderer>("SwarmRenderer");
        _player = GetNode<PlayerController>("../Player");
    }

    public void SpawnEnemy(float x, float z, int typeIndex)
    {
        if (_activeCount >= MaxEntities) return;
        if (WaveConfig?.EnemyTypes == null || typeIndex >= WaveConfig.EnemyTypes.Length) return;

        var config = WaveConfig.EnemyTypes[typeIndex];
        int i = _activeCount;

        _posX[i] = x;
        _posZ[i] = z;
        _velX[i] = 0f;
        _velZ[i] = 0f;
        _health[i] = config.Health;
        _enemyTypeIndex[i] = typeIndex;
        _damageFlashTimer[i] = 0f;
        _deathTimer[i] = SwarmCalculator.AliveMarker;

        _activeCount++;
    }

    public override void _Process(double delta)
    {
        if (_player == null || WaveConfig?.EnemyTypes == null) return;

        float dt = (float)delta;
        float playerX = _player.GlobalPosition.X;
        float playerZ = _player.GlobalPosition.Z;
        float arenaHalf = WaveConfig.ArenaHalfSize;

        UpdateMovement(dt, playerX, playerZ, arenaHalf);
        UpdateTimers(dt);
        CheckPlayerContact(dt, playerX, playerZ);
        RemoveDeadEntities();
        UploadToRenderer();
    }

    private void UpdateMovement(float dt, float playerX, float playerZ, float arenaHalf)
    {
        for (int i = 0; i < _activeCount; i++)
        {
            if (SwarmCalculator.IsDying(_deathTimer[i])) continue;

            var config = WaveConfig.EnemyTypes[_enemyTypeIndex[i]];
            var move = SwarmCalculator.MoveToward(
                _posX[i], _posZ[i], playerX, playerZ, config.MoveSpeed, dt);

            float sepX = 0f, sepZ = 0f;
            for (int j = 0; j < _activeCount; j++)
            {
                if (i == j || SwarmCalculator.IsDying(_deathTimer[j])) continue;
                var sep = SwarmCalculator.CalculateSeparation(
                    _posX[i], _posZ[i], _posX[j], _posZ[j],
                    config.SeparationRadius, config.SeparationForce);
                sepX += sep.VelX;
                sepZ += sep.VelZ;
            }

            _posX[i] = SwarmCalculator.ClampToArena(move.NewX + sepX * dt, arenaHalf);
            _posZ[i] = SwarmCalculator.ClampToArena(move.NewZ + sepZ * dt, arenaHalf);
            _velX[i] = move.VelX + sepX;
            _velZ[i] = move.VelZ + sepZ;
        }
    }

    private void UpdateTimers(float dt)
    {
        for (int i = 0; i < _activeCount; i++)
        {
            _damageFlashTimer[i] = SwarmCalculator.TickFlash(_damageFlashTimer[i], dt);

            if (SwarmCalculator.IsDying(_deathTimer[i]))
            {
                var tick = SwarmCalculator.TickDeath(
                    _deathTimer[i], dt, SwarmCalculator.DefaultDeathDuration);
                _deathTimer[i] = tick.NewDeathTimer;
            }
        }
    }

    private void CheckPlayerContact(float dt, float playerX, float playerZ)
    {
        _contactDamageTimer -= dt;
        if (_contactDamageTimer > 0f) return;

        float contactRadiusSq = 1.5f * 1.5f;
        for (int i = 0; i < _activeCount; i++)
        {
            if (SwarmCalculator.IsDying(_deathTimer[i])) continue;

            float distSq = SwarmCalculator.DistanceSquared(
                _posX[i], _posZ[i], playerX, playerZ);

            if (distSq < contactRadiusSq)
            {
                var config = WaveConfig.EnemyTypes[_enemyTypeIndex[i]];
                _player.TakeDamage(config.ContactDamage);
                _contactDamageTimer = ContactDamageInterval;
                break;
            }
        }
    }

    public void DamageInRadius(float centerX, float centerZ, float radius, int damage)
    {
        float radiusSq = radius * radius;
        for (int i = 0; i < _activeCount; i++)
        {
            if (SwarmCalculator.IsDying(_deathTimer[i])) continue;

            float distSq = SwarmCalculator.DistanceSquared(
                _posX[i], _posZ[i], centerX, centerZ);

            if (distSq < radiusSq)
            {
                DamageEntity(i, damage);
            }
        }
    }

    public (float x, float z) GetNearestAlivePosition(float fromX, float fromZ)
    {
        int nearest = -1;
        float nearestDistSq = float.MaxValue;

        for (int i = 0; i < _activeCount; i++)
        {
            if (SwarmCalculator.IsDying(_deathTimer[i])) continue;

            float distSq = SwarmCalculator.DistanceSquared(
                _posX[i], _posZ[i], fromX, fromZ);

            if (distSq < nearestDistSq)
            {
                nearest = i;
                nearestDistSq = distSq;
            }
        }

        if (nearest >= 0)
        {
            return (_posX[nearest], _posZ[nearest]);
        }

        return (fromX, fromZ);
    }

    private void DamageEntity(int index, int damage)
    {
        var result = SwarmCalculator.TakeDamage(
            _health[index], damage, SwarmCalculator.DefaultFlashDuration);

        _health[index] = result.NewHealth;
        _damageFlashTimer[index] = result.FlashTimer;

        if (result.IsDead)
        {
            _deathTimer[index] = 0f;
            _onEnemyKilled?.Raise();
            _onEnemyKilledAt?.Raise(new Vector3(_posX[index], 0f, _posZ[index]));
        }
    }

    private void RemoveDeadEntities()
    {
        for (int i = _activeCount - 1; i >= 0; i--)
        {
            if (!SwarmCalculator.IsDying(_deathTimer[i])) continue;

            var tick = SwarmCalculator.TickDeath(
                _deathTimer[i], 0f, SwarmCalculator.DefaultDeathDuration);

            if (tick.ShouldRemove)
            {
                int last = _activeCount - 1;
                if (i != last)
                {
                    _posX[i] = _posX[last];
                    _posZ[i] = _posZ[last];
                    _velX[i] = _velX[last];
                    _velZ[i] = _velZ[last];
                    _health[i] = _health[last];
                    _enemyTypeIndex[i] = _enemyTypeIndex[last];
                    _damageFlashTimer[i] = _damageFlashTimer[last];
                    _deathTimer[i] = _deathTimer[last];
                }

                _activeCount--;
            }
        }
    }

    private void UploadToRenderer()
    {
        if (_renderer == null) return;

        _renderer.UpdateInstances(
            _posX, _posZ, _velX, _velZ,
            _enemyTypeIndex, _damageFlashTimer, _deathTimer,
            _activeCount, WaveConfig.EnemyTypes);
    }

    public int GetAliveCount()
    {
        int count = 0;
        for (int i = 0; i < _activeCount; i++)
        {
            if (SwarmCalculator.IsAlive(_deathTimer[i])) count++;
        }
        return count;
    }
}
