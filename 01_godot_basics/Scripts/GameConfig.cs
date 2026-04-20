namespace Collector;

using Godot;

[GlobalClass]
public partial class GameConfig : Resource
{
    [Export] public float PlayerSpeed { get; set; } = 400f;
    [Export] public float HazardSpeed { get; set; } = 200f;
    [Export] public float HazardSpawnInterval { get; set; } = 2.0f;
    [Export] public int StartingLives { get; set; } = 3;
    [Export] public int CoinsPerWave { get; set; } = 5;
    [Export] public float ArenaWidth { get; set; } = 1800f;
    [Export] public float ArenaHeight { get; set; } = 1000f;
    [Export] public float WallThickness { get; set; } = 40f;
}
