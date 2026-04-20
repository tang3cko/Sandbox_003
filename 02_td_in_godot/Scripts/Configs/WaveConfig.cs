namespace TowerDefense;
using Godot;

[GlobalClass]
public partial class WaveConfig : Resource
{
    [Export] public WaveEntry[] Entries { get; set; } = [];
}
