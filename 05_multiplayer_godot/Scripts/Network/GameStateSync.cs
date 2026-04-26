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

    private static void AddSyncProperty(SceneReplicationConfig config, string path)
    {
        var nodePath = new NodePath(path);
        config.AddProperty(nodePath);
        config.PropertySetSpawn(nodePath, true);
        config.PropertySetReplicationMode(nodePath, SceneReplicationConfig.ReplicationMode.OnChange);
    }
}
