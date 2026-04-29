namespace SwarmSurvivor;

using Godot;

public partial class GameStateSync : Node
{
    [Export] public int CurrentWave { get; set; }
    [Export] public int TotalEnemiesInWave { get; set; }
    [Export] public int EnemiesRemaining { get; set; }
    [Export] public int TotalKills { get; set; }
    [Export] public bool IsWaveActive { get; set; }
    [Export] public bool IsVictory { get; set; }
    [Export] public bool IsGameOver { get; set; }

    public override void _Ready()
    {
        var config = new SceneReplicationConfig();
        AddSyncProperty(config, ".:CurrentWave");
        AddSyncProperty(config, ".:TotalEnemiesInWave");
        AddSyncProperty(config, ".:EnemiesRemaining");
        AddSyncProperty(config, ".:TotalKills");
        AddSyncProperty(config, ".:IsWaveActive");
        AddSyncProperty(config, ".:IsVictory");
        AddSyncProperty(config, ".:IsGameOver");

        var sync = new MultiplayerSynchronizer
        {
            Name = "Synchronizer",
            ReplicationConfig = config,
        };
        AddChild(sync);

        if (Multiplayer != null)
        {
            Multiplayer.PeerConnected += OnPeerConnected;
        }
    }

    public override void _ExitTree()
    {
        if (Multiplayer != null)
        {
            Multiplayer.PeerConnected -= OnPeerConnected;
        }
    }

    public void ApplyWaveState(WaveState state)
    {
        if (Multiplayer.MultiplayerPeer == null) return;
        if (!Multiplayer.IsServer()) return;
        CurrentWave = state.WaveNumber;
        TotalEnemiesInWave = state.TotalEnemiesInWave;
        EnemiesRemaining = state.EnemiesRemaining;
        TotalKills = state.TotalKills;
        IsWaveActive = state.IsWaveActive;
        IsVictory = state.IsVictory;
        IsGameOver = state.IsGameOver;
    }

    /// <summary>
    /// Server-side: send the current GameState snapshot to a single peer (late-join).
    /// Snapshot covers Wave / EnemiesRemaining / TotalKills / IsVictory / IsGameOver.
    /// </summary>
    public void SendInitialStateToPeer(int peerId)
    {
        if (Multiplayer.MultiplayerPeer == null) return;
        if (!Multiplayer.IsServer()) return;

        var packet = new byte[GameStateSnapshot.CalculateBufferSize()];
        GameStateSnapshot.Encode(packet,
            wave: CurrentWave,
            enemiesRemaining: EnemiesRemaining,
            totalKills: TotalKills,
            isVictory: IsVictory,
            isGameOver: IsGameOver);

        RpcId(peerId, MethodName.ApplyInitialState, packet);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority,
         CallLocal = false,
         TransferMode = MultiplayerPeer.TransferModeEnum.Reliable,
         TransferChannel = NetworkConfig.ChannelLobby)]
    private void ApplyInitialState(byte[] data)
    {
        if (!GameStateSnapshot.Decode(data, data?.Length ?? 0,
            out int wave, out int enemiesRemaining, out int totalKills,
            out bool isVictory, out bool isGameOver))
        {
            return;
        }

        CurrentWave = wave;
        EnemiesRemaining = enemiesRemaining;
        TotalKills = totalKills;
        IsVictory = isVictory;
        IsGameOver = isGameOver;
    }

    private void OnPeerConnected(long peerId)
    {
        if (Multiplayer.MultiplayerPeer == null) return;
        if (!Multiplayer.IsServer()) return;
        SendInitialStateToPeer((int)peerId);
    }

    private static void AddSyncProperty(SceneReplicationConfig config, string path)
    {
        var nodePath = new NodePath(path);
        config.AddProperty(nodePath);
        config.PropertySetSpawn(nodePath, true);
        config.PropertySetReplicationMode(nodePath, SceneReplicationConfig.ReplicationMode.OnChange);
    }
}
