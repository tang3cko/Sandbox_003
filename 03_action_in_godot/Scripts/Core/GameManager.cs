namespace ArenaSurvivor;

using Godot;
using ReactiveSO;

public partial class GameManager : Node3D
{
    [Export] public PlayerConfig PlayerConfig { get; set; }
    [Export] public WaveConfig WaveConfig { get; set; }
    [Export] public WeaponConfig WeaponConfig { get; set; }
    [Export] public ProjectileConfig ProjectileConfig { get; set; }
    [Export] private IntVariable _health;
    [Export] private FloatVariable _stamina;
    [Export] private IntVariable _score;
    [Export] private IntVariable _waveNumber;
    [Export] private VoidEventChannel _onPlayerDied;
    [Export] private VoidEventChannel _onPlayerAttacked;
    [Export] private VoidEventChannel _onPlayerDodged;
    [Export] private IntEventChannel _onPlayerDamaged;
    [Export] private VoidEventChannel _onEnemyKilled;
    [Export] private IntEventChannel _onScoreEarned;
    [Export] private VoidEventChannel _onWaveCompleted;
    [Export] private VoidEventChannel _onGameOver;
    [Export] private Node3DRuntimeSet _activeEnemies;

    private PlayerController _player;
    private EnemySpawner _spawner;
    private ScreenShake _screenShake;

    public override void _Ready()
    {
        _player = GetNode<PlayerController>("Player");
        _spawner = GetNode<EnemySpawner>("EnemySpawner");
        _screenShake = GetNode<ScreenShake>("ScreenShake");

        ConnectEvents();
    }

    private void ConnectEvents()
    {
        _onPlayerDied.Raised += HandlePlayerDied;
        _onPlayerDamaged.Raised += HandlePlayerDamaged;
        _onEnemyKilled.Raised += HandleEnemyKilled;
        _onScoreEarned.Raised += HandleScoreEarned;
    }

    private void HandlePlayerDied()
    {
        _spawner.HandlePlayerDefeated();
        _onGameOver?.Raise();
        GetTree().Paused = true;
    }

    private void HandlePlayerDamaged(int damage)
    {
        _screenShake.Shake(0.15f, 0.2f);
        HitEffect.Create(_player.GlobalPosition + Vector3.Up, Colors.Red, this);
    }

    private void HandleEnemyKilled()
    {
        _screenShake.Shake(0.08f, 0.1f);
    }

    private void HandleScoreEarned(int amount)
    {
        _score.Value += amount;
    }

    public override void _ExitTree()
    {
        _onPlayerDied.Raised -= HandlePlayerDied;
        _onPlayerDamaged.Raised -= HandlePlayerDamaged;
        _onEnemyKilled.Raised -= HandleEnemyKilled;
        _onScoreEarned.Raised -= HandleScoreEarned;
    }
}
