namespace SwarmSurvivor;

using Godot;

[GlobalClass]
public partial class EnemyTypeConfig : Resource
{
    [Export] public string TypeName { get; set; } = "Swarmling";
    [Export] public int Health { get; set; } = 20;
    [Export] public float MoveSpeed { get; set; } = 3f;
    [Export] public int ContactDamage { get; set; } = 10;
    [Export] public int XPValue { get; set; } = 1;
    [Export] public float SeparationRadius { get; set; } = 1.2f;
    [Export] public float SeparationForce { get; set; } = 8f;
    [Export] public Color BaseColor { get; set; } = new Color(0.8f, 0.2f, 0.2f);
    [Export] public float MeshRadius { get; set; } = 0.4f;
    [Export] public float MeshHeight { get; set; } = 0.8f;
}
