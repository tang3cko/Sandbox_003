namespace ArenaSurvivor;
using Godot;

[GlobalClass]
public partial class EnemyConfig : Resource
{
    [Export] public string DisplayName { get; set; } = "";
    [Export] public int MaxHealth { get; set; } = 50;
    [Export] public float MoveSpeed { get; set; } = 4.0f;
    [Export] public int Damage { get; set; } = 10;
    [Export] public float AttackRange { get; set; } = 2.0f;
    [Export] public float AttackCooldown { get; set; } = 1.5f;
    [Export] public int ScoreReward { get; set; } = 100;
    [Export] public int GoldReward { get; set; } = 10;
    [Export] public float DetectionRange { get; set; } = 15.0f;
    [Export] public float Scale { get; set; } = 1.0f;
    [Export] public Color Color { get; set; } = Colors.Red;
    [Export] public float KnockbackResistance { get; set; } = 0.0f;
}
