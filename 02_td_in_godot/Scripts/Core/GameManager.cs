namespace TowerDefense;

using Godot;
using ReactiveSO;

public partial class GameManager : Node3D
{
    [Export] public GameConfig Config { get; set; }
    [Export] private IntVariable _gold;
    [Export] private IntVariable _lives;
    [Export] private IntVariable _score;
    [Export] private IntVariable _waveNumber;
    [Export] private VoidEventChannel _onEnemyKilled;
    [Export] private VoidEventChannel _onEnemyReachedEnd;
    [Export] private IntEventChannel _onGoldEarned;
    [Export] private VoidEventChannel _onGameOver;
    [Export] private VoidEventChannel _onWaveCompleted;
    [Export] private Node3DRuntimeSet _activeEnemies;

    private WaveManager _waveManager;
    private GameState _state;

    public override void _Ready()
    {
        _waveManager = GetNode<WaveManager>("WaveManager");
        StartGame();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_state.IsGameOver) return;

        if (@event is InputEventKey { Pressed: true } keyEvent && keyEvent.Keycode == Key.R)
        {
            Restart();
        }
    }

    private void StartGame()
    {
        _state = GameStateCalculator.CreateInitial(Config.StartingGold, Config.StartingLives);
        SyncVariables();

        _onEnemyKilled.Raised += HandleEnemyKilled;
        _onEnemyReachedEnd.Raised += HandleEnemyReachedEnd;
        _onGoldEarned.Raised += HandleGoldEarned;

        var timer = GetTree().CreateTimer(2.0);
        timer.Timeout += StartNextWave;
    }

    private void Restart()
    {
        _onEnemyKilled.Raised -= HandleEnemyKilled;
        _onEnemyReachedEnd.Raised -= HandleEnemyReachedEnd;
        _onGoldEarned.Raised -= HandleGoldEarned;

        ClearAllEntities();
        GetNode<HUD>("CanvasLayer/HUD").HideGameOver();
        StartGame();
    }

    private void ClearAllEntities()
    {
        foreach (var child in GetNode("Enemies").GetChildren())
            child.QueueFree();

        foreach (var child in GetNode("Towers").GetChildren())
            child.QueueFree();

        _activeEnemies.Clear();
    }

    private void HandleEnemyKilled()
    {
        _state.Score += 10;
        _score.Value = _state.Score;
        CheckWaveComplete();
    }

    private void HandleGoldEarned(int amount)
    {
        var result = GameStateCalculator.EnemyKilled(_state, amount);
        _state = result.State;
        _gold.Value = _state.Gold;
        _score.Value = _state.Score;
    }

    private void HandleEnemyReachedEnd()
    {
        var result = GameStateCalculator.EnemyReachedEnd(_state, 1);
        _state = result.State;
        _lives.Value = _state.Lives;

        if (result.IsGameOver)
        {
            _onGameOver?.Raise();
        }

        CheckWaveComplete();
    }

    public TowerPlacedResult TryPlaceTower(int cost)
    {
        var result = GameStateCalculator.TryPlaceTower(_state, cost);
        if (result.CanAfford)
        {
            _state = result.State;
            _gold.Value = _state.Gold;
        }
        return result;
    }

    private void CheckWaveComplete()
    {
        if (_waveManager.IsSpawning) return;
        if (_activeEnemies.Count > 0) return;
        if (_state.IsGameOver) return;

        _onWaveCompleted?.Raise();

        int nextIndex = _state.WaveNumber;
        if (nextIndex >= Config.Waves.Length)
        {
            _state.IsGameOver = true;
            _onGameOver?.Raise();
            return;
        }

        var timer = GetTree().CreateTimer(3.0);
        timer.Timeout += StartNextWave;
    }

    private void StartNextWave()
    {
        if (_state.IsGameOver) return;

        int nextIndex = _state.WaveNumber;
        if (nextIndex >= Config.Waves.Length)
            return;

        _state = GameStateCalculator.AdvanceWave(_state);
        _waveNumber.Value = _state.WaveNumber;
        _waveManager.StartWave(Config.Waves[nextIndex]);
    }

    private void SyncVariables()
    {
        _gold.SetWithoutNotify(_state.Gold);
        _lives.SetWithoutNotify(_state.Lives);
        _score.SetWithoutNotify(_state.Score);
        _waveNumber.SetWithoutNotify(_state.WaveNumber);

        _gold.Value = _state.Gold;
        _lives.Value = _state.Lives;
    }

    public override void _ExitTree()
    {
        _onEnemyKilled.Raised -= HandleEnemyKilled;
        _onEnemyReachedEnd.Raised -= HandleEnemyReachedEnd;
        _onGoldEarned.Raised -= HandleGoldEarned;
    }
}
