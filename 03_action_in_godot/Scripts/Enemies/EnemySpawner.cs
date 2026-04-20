namespace ArenaSurvivor;

using Godot;
using ReactiveSO;

public partial class EnemySpawner : Node3D
{
    [Export] public WaveConfig WaveConfig { get; set; }
    [Export] private Node3DRuntimeSet _activeEnemies;
    [Export] private IntVariable _waveNumber;
    [Export] private VoidEventChannel _onWaveCompleted;
    [Export] private VoidEventChannel _onEnemyKilled;
    [Export] private IntEventChannel _onScoreEarned;

    private PackedScene _enemyScene;
    private Node3D _target;
    private WaveState _waveState;
    private float _spawnTimer;
    private int _enemiesToSpawn;
    private float _waveBreakTimer;
    private float _arenaRadius = 18f;
    private RandomNumberGenerator _rng;

    public bool IsWaveActive => _waveState.IsWaveActive;

    public void SetTarget(Node3D target)
    {
        _target = target;
    }

    public override void _Ready()
    {
        _enemyScene = GD.Load<PackedScene>("res://Scenes/Enemy.tscn");
        _rng = new RandomNumberGenerator();
        _rng.Randomize();

        _waveState = WaveCalculator.CreateInitial();
        _onEnemyKilled.Raised += HandleEnemyKilled;

        _waveBreakTimer = 3.0f;
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;

        if (_waveState.IsGameOver) return;

        if (!_waveState.IsWaveActive)
        {
            _waveBreakTimer -= dt;
            if (_waveBreakTimer <= 0f)
            {
                StartNextWave();
            }
            return;
        }

        if (_enemiesToSpawn > 0)
        {
            _spawnTimer -= dt;
            if (_spawnTimer <= 0f)
            {
                SpawnEnemy();
                _enemiesToSpawn--;
                _spawnTimer = WaveConfig.SpawnInterval;
            }
        }
    }

    private void StartNextWave()
    {
        var result = WaveCalculator.StartNextWave(_waveState);
        _waveState = result.State;
        _enemiesToSpawn = result.EnemyCount;
        _spawnTimer = 0.5f;

        _waveNumber.Value = _waveState.WaveNumber;
    }

    private void HandleEnemyKilled()
    {
        var result = WaveCalculator.EnemyDefeated(_waveState, 100);
        _waveState = result.State;

        if (result.IsWaveComplete)
        {
            _onWaveCompleted?.Raise();
            _waveBreakTimer = WaveConfig.TimeBetweenWaves;
        }
    }

    public void HandlePlayerDefeated()
    {
        var result = WaveCalculator.PlayerDefeated(_waveState);
        _waveState = result.State;
    }

    private void SpawnEnemy()
    {
        if (_enemyScene == null || _target == null) return;

        var enemy = _enemyScene.Instantiate<Enemy>();

        var enemyTypes = WaveConfig.EnemyTypes;
        if (enemyTypes.Length > 0)
        {
            var index = _rng.RandiRange(0, enemyTypes.Length - 1);
            enemy.Config = enemyTypes[index];
        }

        var angle = _rng.RandfRange(0, Mathf.Tau);
        var spawnPos = new Vector3(
            Mathf.Cos(angle) * _arenaRadius,
            0,
            Mathf.Sin(angle) * _arenaRadius
        );

        GetTree().Root.GetNode("Main/Enemies").AddChild(enemy);
        enemy.GlobalPosition = spawnPos;
        enemy.Initialize(_target);

        _waveState = WaveCalculator.EnemySpawned(_waveState);
    }

    public override void _ExitTree()
    {
        _onEnemyKilled.Raised -= HandleEnemyKilled;
    }
}
