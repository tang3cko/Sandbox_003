namespace SwarmSurvivor;

using Godot;

[GlobalClass]
public partial class EnemyTypeConfig : Resource
{
    [Export] public string TypeName { get; set; } = "Swarmling";
    [Export(PropertyHint.Range, "1,10000,1")] public int Health { get; set; } = 20;
    [Export(PropertyHint.Range, "0,100,0.1")] public float MoveSpeed { get; set; } = 3f;
    [Export(PropertyHint.Range, "0,10000,1")] public int ContactDamage { get; set; } = 10;
    [Export(PropertyHint.Range, "0,10000,1")] public int XPValue { get; set; } = 1;
    [Export(PropertyHint.Range, "0,100,0.1")] public float SeparationRadius { get; set; } = 1.2f;
    [Export(PropertyHint.Range, "0,100,0.1")] public float SeparationForce { get; set; } = 8f;
    [Export] public Color BaseColor { get; set; } = new Color(0.8f, 0.2f, 0.2f);
    [Export(PropertyHint.Range, "0.01,10,0.01")] public float MeshRadius { get; set; } = 0.4f;
    [Export(PropertyHint.Range, "0.01,10,0.01")] public float MeshHeight { get; set; } = 0.8f;
}
