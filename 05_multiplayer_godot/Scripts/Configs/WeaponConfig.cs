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
    [Export(PropertyHint.Range, "0,10000,1")] public int Damage { get; set; } = 10;
    [Export(PropertyHint.Range, "0.01,100,0.01")] public float FireRate { get; set; } = 1f;
    [Export(PropertyHint.Range, "0.1,100,0.1")] public float Range { get; set; } = 5f;
    [Export(PropertyHint.Range, "0,100,0.1")] public float ProjectileSpeed { get; set; } = 15f;
    [Export(PropertyHint.Range, "1,1000,1")] public int ProjectileCount { get; set; } = 1;
    [Export(PropertyHint.Range, "-20,20,0.1")] public float OrbitalSpeed { get; set; } = 2f;
    [Export(PropertyHint.Range, "0.1,100,0.1")] public float OrbitalRadius { get; set; } = 2f;
    [Export] public Color ProjectileColor { get; set; } = new Color(0.3f, 0.6f, 1f);
}
