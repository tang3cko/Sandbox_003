namespace SwarmSurvivor;

using Godot;

public partial class MultiplayerHUD : CanvasLayer
{
    public Node3D PlayersContainer { get; set; }
    public GameStateSync GameState { get; set; }

    private HpOrbUI _hpOrb;
    private WavePanelUI _wavePanel;
    private OtherPlayersUI _otherPlayers;
    private SkillBarUI _skillBar;
    private WeaponBarUI _weaponBar;

    private NetworkedPlayer _cachedLocalPlayer;

    public override void _Ready()
    {
        _hpOrb = new HpOrbUI();
        AddChild(_hpOrb);

        _wavePanel = new WavePanelUI();
        AddChild(_wavePanel);

        _otherPlayers = new OtherPlayersUI { PlayersContainer = PlayersContainer };
        AddChild(_otherPlayers);

        _skillBar = new SkillBarUI();
        AddChild(_skillBar);

        _weaponBar = new WeaponBarUI();
        AddChild(_weaponBar);
    }

    public override void _Process(double delta)
    {
        UpdateLocalPlayer();
        UpdateWavePanel();
        _otherPlayers?.RefreshList();
    }

    private void UpdateLocalPlayer()
    {
        if (_hpOrb == null) return;

        var player = GetLocalPlayer();
        if (player == null || !IsInstanceValid(player))
        {
            _hpOrb.ClearHp();
            return;
        }

        _hpOrb.UpdateHp(player.CurrentHealth, player.MaxHealth, player.IsDead);
    }

    private void UpdateWavePanel()
    {
        if (_wavePanel == null || GameState == null) return;
        _wavePanel.UpdateWave(GameState.CurrentWave, GameState.EnemiesRemaining, GameState.TotalKills);
        _wavePanel.UpdateQuest(GameState.CurrentWave, GameState.IsVictory, GameState.IsGameOver);
    }

    private NetworkedPlayer GetLocalPlayer()
    {
        if (_cachedLocalPlayer != null && IsInstanceValid(_cachedLocalPlayer))
        {
            return _cachedLocalPlayer;
        }
        if (PlayersContainer == null || !IsInstanceValid(PlayersContainer)) return null;

        foreach (var child in PlayersContainer.GetChildren())
        {
            if (child is NetworkedPlayer p && p.IsLocalAuthority)
            {
                _cachedLocalPlayer = p;
                return p;
            }
        }
        return null;
    }
}
