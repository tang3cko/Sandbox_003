namespace SwarmSurvivor;

using Godot;

public enum UpgradeType
{
    AddWeapon,
    IncreaseDamage,
    IncreaseSpeed,
    IncreaseHealth,
    IncreaseMagnet,
    IncreaseFireRate,
}

[GlobalClass]
public partial class UpgradeConfig : Resource
{
    [Export] public int Id { get; set; }
    [Export] public string UpgradeName { get; set; } = "";
    [Export] public string Description { get; set; } = "";
    [Export] public UpgradeType Type { get; set; }
    [Export] public float Value { get; set; } = 1f;
    [Export] public WeaponConfig WeaponToAdd { get; set; }
}
