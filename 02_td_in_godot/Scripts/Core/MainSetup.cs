namespace TowerDefense;

using Godot;

public partial class MainSetup : Node3D
{
    public override void _Ready()
    {
        // Wire up tower buttons
        var hud = GetNode<HUD>("../CanvasLayer/HUD");
        var placement = GetNode<TowerPlacement>("../TowerPlacement");
        var camera = GetNode<Camera3D>("../Camera3D");
        var gameManager = GetParent<GameManager>();

        placement.Setup(camera, gameManager);

        // Load tower configs and add buttons
        var arrowTower = GD.Load<TowerConfig>("res://Resources/Towers/arrow_tower.tres");
        var cannonTower = GD.Load<TowerConfig>("res://Resources/Towers/cannon_tower.tres");
        var iceTower = GD.Load<TowerConfig>("res://Resources/Towers/ice_tower.tres");

        hud.AddTowerButton(arrowTower, placement);
        hud.AddTowerButton(cannonTower, placement);
        hud.AddTowerButton(iceTower, placement);
    }
}
