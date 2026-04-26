namespace SwarmSurvivor;

using Godot;

[GlobalClass]
public partial class WaveConfig : Resource
{
    [Export] public int BaseEnemyCount { get; set; } = 50;
    [Export] public float GrowthRate { get; set; } = 1.0f;
    [Export] public float BaseSpawnInterval { get; set; } = 0.3f;
    [Export] public float TimeBetweenWaves { get; set; } = 3f;
    [Export] public float ArenaHalfSize { get; set; } = 25f;
    [Export] public float SpawnDistanceMin { get; set; } = 15f;
    [Export] public float SpawnDistanceMax { get; set; } = 20f;
    [Export] public EnemyTypeConfig[] EnemyTypes { get; set; } = [];
}
