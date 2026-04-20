namespace ArenaSurvivor;

using Godot;

public partial class MainSetup : Node
{
    public override void _Ready()
    {
        var camera = GetNode<Camera3D>("../Camera3D");
        var player = GetNode<PlayerController>("../Player");
        var spawner = GetNode<EnemySpawner>("../EnemySpawner");
        var screenShake = GetNode<ScreenShake>("../ScreenShake");

        player.SetCamera(camera);
        spawner.SetTarget(player);
        screenShake.SetCamera(camera);
    }
}
