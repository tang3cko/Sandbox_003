namespace SwarmSurvivor;

using Godot;

[GlobalClass]
public partial class PlayerConfig : Resource
{
    [Export] public float MoveSpeed { get; set; } = 8f;
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public float CollectRadius { get; set; } = 3f;
    [Export] public float MagnetRadius { get; set; } = 6f;
    [Export] public float MagnetSpeed { get; set; } = 12f;
}
