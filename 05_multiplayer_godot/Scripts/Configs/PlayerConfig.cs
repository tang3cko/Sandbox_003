namespace SwarmSurvivor;

using Godot;

[GlobalClass]
public partial class PlayerConfig : Resource
{
    [Export(PropertyHint.Range, "0,100,0.1")] public float MoveSpeed { get; set; } = 8f;
    [Export(PropertyHint.Range, "1,10000,1")] public int MaxHealth { get; set; } = 100;
    [Export(PropertyHint.Range, "0.1,100,0.1")] public float CollectRadius { get; set; } = 3f;
    [Export(PropertyHint.Range, "0.1,100,0.1")] public float MagnetRadius { get; set; } = 6f;
    [Export(PropertyHint.Range, "0,100,0.1")] public float MagnetSpeed { get; set; } = 12f;
}
