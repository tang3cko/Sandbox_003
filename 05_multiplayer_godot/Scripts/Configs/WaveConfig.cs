namespace SwarmSurvivor;

using Godot;

[GlobalClass]
public partial class WaveConfig : Resource
{
    [Export(PropertyHint.Range, "1,1000,1")] public int BaseEnemyCount { get; set; } = 50;
    [Export(PropertyHint.Range, "0,10,0.01")] public float GrowthRate { get; set; } = 1.0f;
    [Export(PropertyHint.Range, "0.01,60,0.01")] public float BaseSpawnInterval { get; set; } = 0.3f;
    [Export(PropertyHint.Range, "0.01,60,0.01")] public float TimeBetweenWaves { get; set; } = 3f;
    [Export(PropertyHint.Range, "0.1,1000,0.1")] public float ArenaHalfSize { get; set; } = 25f;
    [Export(PropertyHint.Range, "0.1,1000,0.1")] public float SpawnDistanceMin { get; set; } = 15f;
    [Export(PropertyHint.Range, "0.1,1000,0.1")] public float SpawnDistanceMax { get; set; } = 20f;
    [Export] public EnemyTypeConfig[] EnemyTypes { get; set; } = [];
}
