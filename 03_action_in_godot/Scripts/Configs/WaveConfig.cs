namespace ArenaSurvivor;
using Godot;

[GlobalClass]
public partial class WaveConfig : Resource
{
    [Export] public float SpawnInterval { get; set; } = 1.5f;
    [Export] public float TimeBetweenWaves { get; set; } = 5.0f;
    [Export] public EnemyConfig[] EnemyTypes { get; set; } = [];
}
