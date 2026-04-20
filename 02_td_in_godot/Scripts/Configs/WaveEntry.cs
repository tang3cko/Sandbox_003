namespace TowerDefense;
using Godot;

[GlobalClass]
public partial class WaveEntry : Resource
{
    [Export] public EnemyConfig EnemyType { get; set; }
    [Export] public int Count { get; set; } = 5;
    [Export] public float SpawnInterval { get; set; } = 1.0f;
    [Export] public float DelayBefore { get; set; } = 0.0f;
}
