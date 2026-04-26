namespace SwarmSurvivor;

using Godot;

public enum WeaponType
{
    Orbital,
    Projectile,
    Aura,
}

[GlobalClass]
public partial class WeaponConfig : Resource
{
    [Export] public string WeaponName { get; set; } = "Orb";
    [Export] public WeaponType Type { get; set; } = WeaponType.Orbital;
    [Export] public int Damage { get; set; } = 10;
    [Export] public float FireRate { get; set; } = 1f;
    [Export] public float Range { get; set; } = 5f;
    [Export] public float ProjectileSpeed { get; set; } = 15f;
    [Export] public int ProjectileCount { get; set; } = 1;
    [Export] public float OrbitalSpeed { get; set; } = 2f;
    [Export] public float OrbitalRadius { get; set; } = 2f;
    [Export] public Color ProjectileColor { get; set; } = new Color(0.3f, 0.6f, 1f);
}
