namespace SwarmSurvivor;

using System.Collections.Generic;
using Godot;
using ReactiveSO;

public partial class SwarmManager : Node3D
{
    [Export] public WaveConfig WaveConfig { get; set; }
    [Export] private VoidEventChannel _onEnemyKilled;
    [Export] private Vector3EventChannel _onEnemyKilledAt;
    [Export] private IntEventChannel _onPlayerDamaged;

    private const int MaxEntities = 600;
    private const float ContactRadius = 1.5f;
    private const float ContactDamageInterval = 1.0f;

    private float[] _posX, _posZ;
    private float[] _velX, _velZ;
    private int[] _health;
    private int[] _enemyTypeIndex;
    private float[] _damageFlashTimer;
    private float[] _deathTimer;
    private int _activeCount;

    private SwarmRenderer _renderer;

    private sealed class TargetEntry
    {
        public Node3D Player;
        public IDamageable Damageable;
        public float ContactCooldown;
    }

    private readonly List<TargetEntry> _targets = new();
    private float[] _targetPosBufX = new float[NetworkConfig.MaxPlayers];
    private float[] _targetPosBufZ = new float[NetworkConfig.MaxPlayers];

    public int ActiveCount => _activeCount;
    public int TargetCount => _targets.Count;

    public void GetSoAReference(
        out float[] posX, out float[] posZ,
        out float[] velX, out float[] velZ,
        out int[] typeIndex,
        out float[] flashTimer,
        out float[] deathTimer)
    {
        posX = _posX;
        posZ = _posZ;
        velX = _velX;
        velZ = _velZ;
        typeIndex = _enemyTypeIndex;
        flashTimer = _damageFlashTimer;
        deathTimer = _deathTimer;
    }

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

        if (_targets.Count == 0)
        {
            var fallback = GetNodeOrNull<Node3D>("../Player");
            if (fallback != null) RegisterTarget(fallback);
        }
    }

    public void RegisterTarget(Node3D player)
    {
        if (player == null) return;
        if (player is not IDamageable damageable)
        {
            GD.PrintErr($"[SwarmManager] {player.Name} does not implement IDamageable; skipping registration");
            return;
        }
        for (int i = 0; i < _targets.Count; i++)
        {
            if (_targets[i].Player == player) return;
        }
        _targets.Add(new TargetEntry
        {
            Player = player,
            Damageable = damageable,
            ContactCooldown = 0f,
        });
        EnsureTargetBufferCapacity(_targets.Count);
    }

    public bool UnregisterTarget(Node3D player)
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            if (_targets[i].Player == player)
            {
                _targets.RemoveAt(i);
                return true;
            }
        }
        return false;
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
        if (_targets.Count == 0 || WaveConfig?.EnemyTypes == null) return;

        float dt = (float)delta;
        float arenaHalf = WaveConfig.ArenaHalfSize;
        int targetCount = CacheTargetPositions();

        UpdateMovement(dt, arenaHalf, targetCount);
        UpdateTimers(dt);
        CheckPlayerContact(dt, targetCount);
        RemoveDeadEntities();
        UploadToRenderer();
    }

    private int CacheTargetPositions()
    {
        int count = _targets.Count;
        EnsureTargetBufferCapacity(count);
        for (int t = 0; t < count; t++)
        {
            var pos = _targets[t].Player.GlobalPosition;
            _targetPosBufX[t] = pos.X;
            _targetPosBufZ[t] = pos.Z;
        }
        return count;
    }

    private void EnsureTargetBufferCapacity(int needed)
    {
        if (_targetPosBufX.Length >= needed) return;
        _targetPosBufX = new float[needed];
        _targetPosBufZ = new float[needed];
    }

    private void UpdateMovement(float dt, float arenaHalf, int targetCount)
    {
        for (int i = 0; i < _activeCount; i++)
        {
            if (SwarmCalculator.IsDying(_deathTimer[i])) continue;

            int nearestIdx = SwarmCalculator.FindNearestTargetIndex(
                _posX[i], _posZ[i], _targetPosBufX, _targetPosBufZ, targetCount);
            if (nearestIdx < 0) continue;

            var config = WaveConfig.EnemyTypes[_enemyTypeIndex[i]];
            var move = SwarmCalculator.MoveToward(
                _posX[i], _posZ[i],
                _targetPosBufX[nearestIdx], _targetPosBufZ[nearestIdx],
                config.MoveSpeed, dt);

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

    private void CheckPlayerContact(float dt, int targetCount)
    {
        float contactRadiusSq = ContactRadius * ContactRadius;
        for (int t = 0; t < targetCount; t++)
        {
            var entry = _targets[t];
            entry.ContactCooldown -= dt;
            if (entry.ContactCooldown > 0f) continue;

            float px = _targetPosBufX[t];
            float pz = _targetPosBufZ[t];
            for (int i = 0; i < _activeCount; i++)
            {
                if (SwarmCalculator.IsDying(_deathTimer[i])) continue;

                float distSq = SwarmCalculator.DistanceSquared(_posX[i], _posZ[i], px, pz);
                if (distSq < contactRadiusSq)
                {
                    var config = WaveConfig.EnemyTypes[_enemyTypeIndex[i]];
                    entry.Damageable?.TakeDamage(config.ContactDamage);
                    entry.ContactCooldown = ContactDamageInterval;
                    break;
                }
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
