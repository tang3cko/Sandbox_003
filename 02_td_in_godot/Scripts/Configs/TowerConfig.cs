namespace TowerDefense;
using Godot;

[GlobalClass]
public partial class TowerConfig : Resource
{
    [Export] public string DisplayName { get; set; } = "";
    [Export] public int Cost { get; set; } = 50;
    [Export] public float Range { get; set; } = 8.0f;
    [Export] public float FireRate { get; set; } = 1.0f;  // shots per second
    [Export] public int Damage { get; set; } = 25;
    [Export] public float ProjectileSpeed { get; set; } = 20.0f;
    [Export] public float AreaDamageRadius { get; set; } = 0.0f;  // 0 = single target
    [Export] public float SlowAmount { get; set; } = 0.0f;  // 0 = no slow
    [Export] public float SlowDuration { get; set; } = 0.0f;
    [Export] public Color Color { get; set; } = Colors.Blue;
}
