namespace TowerDefense;
using Godot;

[GlobalClass]
public partial class GameConfig : Resource
{
    [Export] public int StartingGold { get; set; } = 200;
    [Export] public int StartingLives { get; set; } = 20;
    [Export] public WaveConfig[] Waves { get; set; } = [];
}
