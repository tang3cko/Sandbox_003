namespace SwarmSurvivor;

using Godot;
using ReactiveSO;

public partial class GameManager : Node3D
{
    [Export] public PlayerConfig PlayerConfig { get; set; }
    [Export] public WaveConfig WaveConfig { get; set; }
    [Export] public WeaponConfig[] StartingWeapons { get; set; }
    [Export] public UpgradeConfig[] AvailableUpgrades { get; set; }
    [Export] private IntVariable _health;
    [Export] private IntVariable _score;
    [Export] private IntVariable _waveNumber;
    [Export] private IntVariable _killCount;
    [Export] private VoidEventChannel _onPlayerDied;
    [Export] private IntEventChannel _onPlayerDamaged;
    [Export] private VoidEventChannel _onEnemyKilled;
    [Export] private VoidEventChannel _onWaveCompleted;
    [Export] private VoidEventChannel _onGameOver;
    [Export] private IntEventChannel _onUpgradeSelected;

    private WeaponSystem _weaponSystem;
    private SwarmSpawner _swarmSpawner;
    private ScreenShake _screenShake;
    private PauseMenu _pauseMenu;
    private GemManager _gemManager;

    public override void _Ready()
    {
        if (PlayerConfig == null || WaveConfig == null)
        {
            GD.PrintErr($"[{GetType().Name}] Config not assigned.");
            return;
        }

        _weaponSystem = GetNode<WeaponSystem>("WeaponSystem");
        _swarmSpawner = GetNode<SwarmSpawner>("SwarmSpawner");
        _screenShake = GetNode<ScreenShake>("ScreenShake");
        _pauseMenu = GetNodeOrNull<PauseMenu>("PauseMenuLayer/PauseMenu");
        _gemManager = GetNode<GemManager>("GemManager");

        if (_health != null) _health.Value = PlayerConfig.MaxHealth;
        if (_score != null) _score.Value = 0;
        if (_killCount != null) _killCount.Value = 0;

        var hud = GetNodeOrNull<HUD>("HUD");
        hud?.SetMaxHealth(PlayerConfig.MaxHealth);

        ConnectEvents();
        SetupStartingWeapons();
    }

    private void ConnectEvents()
    {
        if (_onPlayerDied != null) _onPlayerDied.Raised += HandlePlayerDied;
        if (_onPlayerDamaged != null) _onPlayerDamaged.Raised += HandlePlayerDamaged;
        if (_onEnemyKilled != null) _onEnemyKilled.Raised += HandleEnemyKilled;
        if (_onWaveCompleted != null) _onWaveCompleted.Raised += HandleWaveCompleted;
        if (_onUpgradeSelected != null) _onUpgradeSelected.Raised += HandleUpgradeSelected;
    }

    private void SetupStartingWeapons()
    {
        if (StartingWeapons == null) return;
        foreach (var weapon in StartingWeapons)
        {
            _weaponSystem.AddWeapon(weapon);
        }
    }

    private void HandlePlayerDied()
    {
        _swarmSpawner.HandlePlayerDefeated();
        _onGameOver?.Raise();
        GetTree().Paused = true;
    }

    private void HandlePlayerDamaged(int health)
    {
        _screenShake?.Shake(0.15f, 0.2f);
    }

    private void HandleEnemyKilled()
    {
        if (_killCount != null) _killCount.Value++;
        if (_score != null) _score.Value += 10 * (_waveNumber?.Value ?? 1);
        _screenShake?.Shake(0.05f, 0.08f);
    }

    private void HandleWaveCompleted()
    {
        var waveState = _swarmSpawner.GetWaveState();
        if (waveState.IsVictory)
        {
            _pauseMenu?.ShowVictory();
        }
    }

    private void HandleUpgradeSelected(int id)
    {
        if (AvailableUpgrades == null) return;

        foreach (var upgrade in AvailableUpgrades)
        {
            if (upgrade.Id != id) continue;

            switch (upgrade.Type)
            {
                case UpgradeType.AddWeapon:
                    if (upgrade.WeaponToAdd != null)
                    {
                        _weaponSystem.AddWeapon(upgrade.WeaponToAdd);
                    }
                    break;
                case UpgradeType.IncreaseDamage:
                    _weaponSystem.ApplyDamageMultiplier(upgrade.Value);
                    break;
                case UpgradeType.IncreaseFireRate:
                    _weaponSystem.ApplyFireRateMultiplier(upgrade.Value);
                    break;
                case UpgradeType.IncreaseMagnet:
                    _gemManager.SetMagnetRadius(
                        PlayerConfig.MagnetRadius * upgrade.Value);
                    break;
                case UpgradeType.IncreaseSpeed:
                    PlayerConfig.MoveSpeed *= upgrade.Value;
                    break;
                case UpgradeType.IncreaseHealth:
                    PlayerConfig.MaxHealth += (int)upgrade.Value;
                    if (_health != null) _health.Value = PlayerConfig.MaxHealth;
                    break;
            }
            break;
        }
    }

    public override void _ExitTree()
    {
        if (_onPlayerDied != null) _onPlayerDied.Raised -= HandlePlayerDied;
        if (_onPlayerDamaged != null) _onPlayerDamaged.Raised -= HandlePlayerDamaged;
        if (_onEnemyKilled != null) _onEnemyKilled.Raised -= HandleEnemyKilled;
        if (_onWaveCompleted != null) _onWaveCompleted.Raised -= HandleWaveCompleted;
        if (_onUpgradeSelected != null) _onUpgradeSelected.Raised -= HandleUpgradeSelected;
    }
}
