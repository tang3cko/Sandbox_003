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
    [Export(PropertyHint.Range, "0,10000,1")] public int Id { get; set; }
    [Export] public string UpgradeName { get; set; } = "";
    [Export] public string Description { get; set; } = "";
    [Export] public UpgradeType Type { get; set; }
    [Export(PropertyHint.Range, "0,1000,0.01,or_greater")] public float Value { get; set; } = 1f;
    [Export] public WeaponConfig WeaponToAdd { get; set; }
}
