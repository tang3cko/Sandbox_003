namespace SwarmSurvivor;

using Godot;

public partial class ProjectileManager : Node3D
{
    private const int MaxProjectiles = 200;
    private const int MaxOrbitals = 8;

    private float[] _projX, _projZ;
    private float[] _dirX, _dirZ;
    private float[] _speed;
    private int[] _damage;
    private float[] _lifetime;
    private int[] _ownerId;
    private int[] _colorIdx;
    private int[] _projectileId;
    private ushort _nextProjectileId = 1;
    private int _projCount;

    private float[] _orbX, _orbZ;
    private Color[] _orbColor;
    private int _orbCount;

    private SwarmManager _swarmManager;
    private ProjectileRenderer _renderer;
    private float _simAccumulator;

    public GameStateSync GameState { get; set; }

    public int ProjectileCount => _projCount;

    public void GetSoAReference(
        out float[] posX, out float[] posZ,
        out float[] dirX, out float[] dirZ,
        out float[] lifetime,
        out int[] ownerId,
        out int[] colorIdx,
        out int[] projectileId)
    {
        posX = _projX;
        posZ = _projZ;
        dirX = _dirX;
        dirZ = _dirZ;
        lifetime = _lifetime;
        ownerId = _ownerId;
        colorIdx = _colorIdx;
        projectileId = _projectileId;
    }

    public override void _Ready()
    {
        _projX = new float[MaxProjectiles];
        _projZ = new float[MaxProjectiles];
        _dirX = new float[MaxProjectiles];
        _dirZ = new float[MaxProjectiles];
        _speed = new float[MaxProjectiles];
        _damage = new int[MaxProjectiles];
        _lifetime = new float[MaxProjectiles];
        _ownerId = new int[MaxProjectiles];
        _colorIdx = new int[MaxProjectiles];
        _projectileId = new int[MaxProjectiles];
        _projCount = 0;

        _orbX = new float[MaxOrbitals];
        _orbZ = new float[MaxOrbitals];
        _orbColor = new Color[MaxOrbitals];
        _orbCount = 0;

        _swarmManager = GetNodeOrNull<SwarmManager>("../SwarmManager");
        _renderer = GetNodeOrNull<ProjectileRenderer>("ProjectileRenderer");
    }

    public void SpawnProjectile(
        float x, float z, float dirX, float dirZ,
        float speed, int damage, float range,
        int colorIdx, int ownerPeerId)
    {
        if (_projCount >= MaxProjectiles) return;

        int i = _projCount;
        _projX[i] = x;
        _projZ[i] = z;
        _dirX[i] = dirX;
        _dirZ[i] = dirZ;
        _speed[i] = speed;
        _damage[i] = damage;
        _lifetime[i] = ProjectileLifetimeCalculator.ComputeInitialLifetime(range, speed);
        _ownerId[i] = ownerPeerId;
        _colorIdx[i] = colorIdx;
        _projectileId[i] = _nextProjectileId++;
        if (_nextProjectileId == 0) _nextProjectileId = 1;
        _projCount++;
    }

    public void UpdateOrbital(int index, float x, float z, Color color)
    {
        if (index >= MaxOrbitals) return;
        _orbX[index] = x;
        _orbZ[index] = z;
        _orbColor[index] = color;
    }

    public void SetOrbitalCount(int count)
    {
        _orbCount = count;
    }

    public (float x, float z) GetOrbitalPosition(int index)
    {
        if (index < 0 || index >= _orbCount) return (0f, 0f);
        return (_orbX[index], _orbZ[index]);
    }

    public int GetOrbitalCount() => _orbCount;

    public override void _Process(double delta)
    {
        if (GameState?.IsGameOver == true)
        {
            // Pure-calculator decision; idempotent so repeat ticks are safe.
            var plan = GameOverCleanupCalculator.Compute(
                liveEnemyCount: 0,
                liveProjectileCount: _projCount,
                queuedWaveCount: 0,
                isGameOver: true);
            if (plan.ClearProjectiles) ClearAllProjectiles();
            _renderer?.UpdateInstances(_projX, _projZ, _dirX, _dirZ, _lifetime, _ownerId, _colorIdx, _projCount);
            return;
        }

        var (newAcc, ticks) = TickAccumulator.Advance(_simAccumulator, (float)delta);
        _simAccumulator = newAcc;

        const float dt = TickAccumulator.DefaultFixedTimeStep;
        for (int t = 0; t < ticks; t++)
        {
            UpdateProjectiles(dt);
        }

        _renderer?.UpdateInstances(_projX, _projZ, _dirX, _dirZ, _lifetime, _ownerId, _colorIdx, _projCount);
    }

    private void UpdateProjectiles(float dt)
    {
        float hitRadius = ProjectileLifetimeCalculator.DefaultHitRadius;

        for (int i = _projCount - 1; i >= 0; i--)
        {
            var (nx, nz) = ProjectileLifetimeCalculator.Step(_projX[i], _projZ[i], _dirX[i], _dirZ[i], _speed[i], dt);
            _projX[i] = nx;
            _projZ[i] = nz;
            _lifetime[i] = ProjectileLifetimeCalculator.TickLifetime(_lifetime[i], dt);

            if (ProjectileLifetimeCalculator.IsLifetimeExpired(_lifetime[i]))
            {
                RemoveProjectile(i);
                continue;
            }

            if (_swarmManager != null
                && _swarmManager.TryDamageFirstInRadius(
                    _projX[i], _projZ[i], hitRadius, _damage[i], out _, out _))
            {
                RemoveProjectile(i);
            }
        }
    }

    // Drops every in-flight projectile. Backing arrays stay allocated.
    public void ClearAllProjectiles()
    {
        _projCount = 0;
    }

    private void RemoveProjectile(int index)
    {
        int last = _projCount - 1;
        if (index != last)
        {
            _projX[index] = _projX[last];
            _projZ[index] = _projZ[last];
            _dirX[index] = _dirX[last];
            _dirZ[index] = _dirZ[last];
            _speed[index] = _speed[last];
            _damage[index] = _damage[last];
            _lifetime[index] = _lifetime[last];
            _ownerId[index] = _ownerId[last];
            _colorIdx[index] = _colorIdx[last];
            _projectileId[index] = _projectileId[last];
        }
        _projCount--;
    }
}
