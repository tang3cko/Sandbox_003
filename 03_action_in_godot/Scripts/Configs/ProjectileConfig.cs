namespace ArenaSurvivor;
using Godot;

[GlobalClass]
public partial class ProjectileConfig : Resource
{
    [Export] public int Damage { get; set; } = 15;
    [Export] public float Speed { get; set; } = 25.0f;
    [Export] public float Lifetime { get; set; } = 3.0f;
    [Export] public float Scale { get; set; } = 0.3f;
    [Export] public Color Color { get; set; } = Colors.Cyan;
}
