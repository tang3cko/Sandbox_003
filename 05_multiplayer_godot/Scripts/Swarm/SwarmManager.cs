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

    public GameStateSync GameState { get; set; }

    private const int MaxEntities = 600;
    private const float ContactRadius = ContactCalculator.DefaultContactRadius;
    private const float ContactDamageInterval = ContactCalculator.DefaultContactDamageInterval;

    private float[] _posX, _posZ;
    private float[] _velX, _velZ;
    private int[] _health;
    private int[] _enemyTypeIndex;
    private float[] _damageFlashTimer;
    private float[] _deathTimer;
    private int[] _entityId;
    private ushort _nextEntityId = 1;
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
    private readonly SpatialSeparationGrid _separationGrid = new();
    private float _simAccumulator;

    public int ActiveCount => _activeCount;
    public int TargetCount => _targets.Count;

    public void GetSoAReference(
        out float[] posX, out float[] posZ,
        out float[] velX, out float[] velZ,
        out int[] typeIndex,
        out float[] flashTimer,
        out float[] deathTimer,
        out int[] entityId)
    {
        posX = _posX;
        posZ = _posZ;
        velX = _velX;
        velZ = _velZ;
        typeIndex = _enemyTypeIndex;
        flashTimer = _damageFlashTimer;
        deathTimer = _deathTimer;
        entityId = _entityId;
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
        _entityId = new int[MaxEntities];
        _activeCount = 0;

        _renderer = GetNode<SwarmRenderer>("SwarmRenderer");

        _onEnemyKilled ??= GD.Load<VoidEventChannel>("res://Resources/Events/on_enemy_killed.tres");
        _onEnemyKilledAt ??= GD.Load<Vector3EventChannel>("res://Resources/Events/on_enemy_killed_at.tres");
        _onPlayerDamaged ??= GD.Load<IntEventChannel>("res://Resources/Events/on_player_damaged.tres");

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
            NetLog.Error($"[SwarmManager] {player.Name} does not implement IDamageable; skipping registration");
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
        _entityId[i] = _nextEntityId++;
        if (_nextEntityId == 0) _nextEntityId = 1;

        _activeCount++;
    }

    public override void _Process(double delta)
    {
        bool isGameOver = GameState?.IsGameOver ?? false;
        if (isGameOver)
        {
            // Consult the pure calculator each tick. The plan is data only — it's the
            // manager's job to execute. Clearing is idempotent (Plan says nothing when
            // counts are 0), so repeat consultation is safe.
            var plan = GameOverCleanupCalculator.Compute(
                liveEnemyCount: _activeCount,
                liveProjectileCount: 0,
                queuedWaveCount: 0,
                isGameOver: true);
            if (plan.ClearEnemies) ClearAllEnemies();
            UploadToRenderer();
            return;
        }

        if (_targets.Count == 0 || WaveConfig?.EnemyTypes == null) return;

        var (newAcc, ticks) = TickAccumulator.Advance(_simAccumulator, (float)delta);
        _simAccumulator = newAcc;

        const float dt = TickAccumulator.DefaultFixedTimeStep;
        float arenaHalf = WaveConfig.ArenaHalfSize;
        for (int t = 0; t < ticks; t++)
        {
            int targetCount = CacheTargetPositions();
            UpdateMovement(dt, arenaHalf, targetCount);
            UpdateTimers(dt);
            CheckPlayerContact(dt, targetCount);
            RemoveDeadEntities();
        }

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
        // Build the spatial grid once per tick using the maximum SeparationRadius
        // across all enemy types. Per-entity queries still pass that entity's
        // own radius; CalculateSeparation rejects out-of-range pairs, so the
        // result is identical to the naive O(N^2) scan as long as cellSize >=
        // any per-entity radius (3x3 cell scan covers it).
        float maxSeparationRadius = 0f;
        var enemyTypes = WaveConfig.EnemyTypes;
        for (int t = 0; t < enemyTypes.Length; t++)
        {
            float r = enemyTypes[t].SeparationRadius;
            if (r > maxSeparationRadius) maxSeparationRadius = r;
        }
        _separationGrid.Build(_posX, _posZ, _deathTimer, _activeCount, maxSeparationRadius);

        for (int i = 0; i < _activeCount; i++)
        {
            if (SwarmCalculator.IsDying(_deathTimer[i])) continue;

            int nearestIdx = SwarmCalculator.FindNearestTargetIndex(
                _posX[i], _posZ[i], _targetPosBufX, _targetPosBufZ, targetCount);
            if (nearestIdx < 0) continue;

            var config = enemyTypes[_enemyTypeIndex[i]];
            var move = SwarmCalculator.MoveToward(
                _posX[i], _posZ[i],
                _targetPosBufX[nearestIdx], _targetPosBufZ[nearestIdx],
                config.MoveSpeed, dt);

            var sep = _separationGrid.ComputeAccumulatedSeparation(
                i, _posX, _posZ, _deathTimer, _activeCount,
                config.SeparationRadius, config.SeparationForce);
            float sepX = sep.X;
            float sepZ = sep.Z;

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
        for (int t = 0; t < targetCount; t++)
        {
            var entry = _targets[t];
            entry.ContactCooldown = ContactCalculator.TickCooldown(entry.ContactCooldown, dt);
            if (!ContactCalculator.IsCooldownReady(entry.ContactCooldown)) continue;

            float px = _targetPosBufX[t];
            float pz = _targetPosBufZ[t];
            for (int i = 0; i < _activeCount; i++)
            {
                if (SwarmCalculator.IsDying(_deathTimer[i])) continue;

                if (ContactCalculator.IsWithinContactRadius(_posX[i], _posZ[i], px, pz, ContactRadius))
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
        var result = NearestEntityFinder.FindNearestAlivePosition(
            fromX, fromZ, _posX, _posZ, _deathTimer, _activeCount);
        return (result.X, result.Z);
    }

    public bool TryDamageFirstInRadius(
        float centerX, float centerZ,
        float radius, int damage,
        out float hitX, out float hitZ)
    {
        float radiusSq = radius * radius;
        int firstHit = -1;
        float firstDistSq = float.MaxValue;

        for (int i = 0; i < _activeCount; i++)
        {
            if (SwarmCalculator.IsDying(_deathTimer[i])) continue;

            float distSq = SwarmCalculator.DistanceSquared(
                _posX[i], _posZ[i], centerX, centerZ);

            if (distSq < radiusSq && distSq < firstDistSq)
            {
                firstHit = i;
                firstDistSq = distSq;
            }
        }

        if (firstHit >= 0)
        {
            hitX = _posX[firstHit];
            hitZ = _posZ[firstHit];
            DamageEntity(firstHit, damage);
            return true;
        }

        hitX = 0f;
        hitZ = 0f;
        return false;
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
                    _entityId[i] = _entityId[last];
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

    // Drops every active entity slot. Used by the game-over cleanup path.
    // Leaves backing arrays allocated and resets the active count to 0.
    public void ClearAllEnemies()
    {
        _activeCount = 0;
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
