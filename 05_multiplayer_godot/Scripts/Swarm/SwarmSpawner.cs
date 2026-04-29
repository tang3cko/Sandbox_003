namespace SwarmSurvivor;

using System;
using Godot;
using ReactiveSO;

public partial class SwarmSpawner : Node3D
{
    [Export] public WaveConfig WaveConfig { get; set; }
    [Export] public bool AutoStart { get; set; } = true;
    [Export] private IntVariable _waveNumber;
    [Export] private VoidEventChannel _onWaveCompleted;
    [Export] private VoidEventChannel _onEnemyKilled;

    public GameStateSync GameState { get; set; }

    private SwarmManager _swarmManager;
    private WaveState _waveState;
    private float _spawnTimer;
    private float _waveDelayTimer;
    private bool _waitingForNextWave;
    private Random _rng;

    public bool IsSpawning => _waveState.IsWaveActive && _waveState.EnemiesSpawned < _waveState.TotalEnemiesInWave;

    public override void _Ready()
    {
        _swarmManager = GetNode<SwarmManager>("../SwarmManager");
        _waveState = WaveCalculator.CreateInitial();
        _rng = new Random();

        _onEnemyKilled ??= GD.Load<VoidEventChannel>("res://Resources/Events/on_enemy_killed.tres");
        _onWaveCompleted ??= GD.Load<VoidEventChannel>("res://Resources/Events/on_wave_completed.tres");

        if (_onEnemyKilled != null)
        {
            _onEnemyKilled.Raised += HandleEnemyKilled;
        }

        if (AutoStart)
        {
            _waitingForNextWave = true;
            _waveDelayTimer = WaveCalculator.InitialWaveDelay;
        }

        NetLog.Info($"[SwarmSpawner] _Ready: AutoStart={AutoStart}, WaveConfig={WaveConfig != null}, SwarmManager={_swarmManager != null}, EnemyKilled={_onEnemyKilled != null}");
        NetLog.Info($"[SwarmSpawner] EnemyTypes={WaveConfig?.EnemyTypes?.Length ?? -1}, BaseCount={WaveConfig?.BaseEnemyCount ?? -1}");

        GameState?.ApplyWaveState(_waveState);
    }

    public void StartSpawning()
    {
        if (_waveState.IsWaveActive || _waitingForNextWave) return;
        _waitingForNextWave = true;
        _waveDelayTimer = WaveCalculator.StartSpawningDelay;
        NetLog.Info("[SwarmSpawner] StartSpawning requested");
    }

    private void HandleEnemyKilled()
    {
        var result = WaveCalculator.EnemyDefeated(_waveState);
        _waveState = result.State;

        if (result.IsWaveComplete)
        {
            _onWaveCompleted?.Raise();

            if (_waveState.IsVictory)
            {
                GameState?.ApplyWaveState(_waveState);
                return;
            }

            _waitingForNextWave = true;
            _waveDelayTimer = WaveConfig?.TimeBetweenWaves ?? WaveCalculator.DefaultTimeBetweenWaves;
        }

        GameState?.ApplyWaveState(_waveState);
    }

    public override void _Process(double delta)
    {
        if (WaveConfig == null || _swarmManager == null) return;
        if (_waveState.IsGameOver || _waveState.IsVictory) return;

        float dt = (float)delta;

        if (_waitingForNextWave)
        {
            _waveDelayTimer -= dt;
            if (_waveDelayTimer <= 0f)
            {
                StartNextWave();
                _waitingForNextWave = false;
            }
            return;
        }

        if (!_waveState.IsWaveActive) return;
        if (_waveState.EnemiesSpawned >= _waveState.TotalEnemiesInWave) return;

        _spawnTimer -= dt;
        if (_spawnTimer <= 0f)
        {
            SpawnOneEnemy();
            _spawnTimer = WaveCalculator.GetSpawnInterval(
                _waveState.WaveNumber, WaveConfig.BaseSpawnInterval);
        }
    }

    private void StartNextWave()
    {
        var result = WaveCalculator.StartNextWave(
            _waveState, WaveConfig.BaseEnemyCount, WaveConfig.GrowthRate);
        _waveState = result.State;
        _spawnTimer = 0f;

        if (_waveNumber != null) _waveNumber.Value = _waveState.WaveNumber;
        NetLog.Info($"[SwarmSpawner] StartNextWave: wave={_waveState.WaveNumber}, total={_waveState.TotalEnemiesInWave}");

        GameState?.ApplyWaveState(_waveState);
    }

    private void SpawnOneEnemy()
    {
        int typeCount = WaveConfig.EnemyTypes?.Length ?? 0;
        if (typeCount == 0)
        {
            NetLog.Error($"[SwarmSpawner] SpawnOneEnemy FAILED: EnemyTypes is null or empty (typeCount={typeCount})");
            return;
        }

        int typeIndex = _rng.Next(typeCount);
        var spawnPos = GetRandomSpawnPosition();

        _swarmManager.SpawnEnemy(spawnPos.X, spawnPos.Z, typeIndex);
        _waveState = WaveCalculator.EnemySpawned(_waveState);
    }

    private Vector3 GetRandomSpawnPosition()
    {
        var (x, z) = SpawnPositionCalculator.ComputeRandomSpawnPosition(
            _rng.NextDouble(), _rng.NextDouble(),
            WaveConfig.SpawnDistanceMin, WaveConfig.SpawnDistanceMax);
        return new Vector3(x, 0f, z);
    }

    public void HandlePlayerDefeated()
    {
        var result = WaveCalculator.PlayerDefeated(_waveState);
        _waveState = result.State;

        GameState?.ApplyWaveState(_waveState);
    }

    public WaveState GetWaveState() => _waveState;

    public override void _ExitTree()
    {
        if (_onEnemyKilled != null)
        {
            _onEnemyKilled.Raised -= HandleEnemyKilled;
        }
    }
}
