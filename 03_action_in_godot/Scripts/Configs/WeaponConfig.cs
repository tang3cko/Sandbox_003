namespace ArenaSurvivor;
using Godot;

[GlobalClass]
public partial class WeaponConfig : Resource
{
    [Export] public string DisplayName { get; set; } = "";
    [Export] public int BaseDamage { get; set; } = 20;
    [Export] public float AttackSpeed { get; set; } = 1.0f;
    [Export] public float Range { get; set; } = 2.5f;
    [Export] public float KnockbackForce { get; set; } = 5.0f;
    [Export] public float HitStopDuration { get; set; } = 0.05f;
    [Export] public Color TrailColor { get; set; } = Colors.White;
}
