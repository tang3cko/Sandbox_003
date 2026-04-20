namespace TowerDefense;
using Godot;

[GlobalClass]
public partial class EnemyConfig : Resource
{
    [Export] public string DisplayName { get; set; } = "";
    [Export] public float Speed { get; set; } = 3.0f;
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public int GoldReward { get; set; } = 10;
    [Export] public int Damage { get; set; } = 1;
    [Export] public float Scale { get; set; } = 1.0f;
    [Export] public Color Color { get; set; } = Colors.Red;
}
