namespace TowerDefense;

using Godot;

public partial class WaveManager : Node3D
{
    [Export] public Vector3[] Waypoints { get; set; } = [];

    private PackedScene _enemyScene;
    private bool _isSpawning;

    public bool IsSpawning => _isSpawning;

    public override void _Ready()
    {
        _enemyScene = GD.Load<PackedScene>("res://Scenes/Enemy.tscn");
    }

    public async void StartWave(WaveConfig wave)
    {
        _isSpawning = true;

        foreach (var entry in wave.Entries)
        {
            if (entry.DelayBefore > 0)
                await ToSignal(GetTree().CreateTimer(entry.DelayBefore), SceneTreeTimer.SignalName.Timeout);

            for (int i = 0; i < entry.Count; i++)
            {
                SpawnEnemy(entry.EnemyType);

                if (i < entry.Count - 1)
                    await ToSignal(GetTree().CreateTimer(entry.SpawnInterval), SceneTreeTimer.SignalName.Timeout);
            }
        }

        _isSpawning = false;
    }

    private void SpawnEnemy(EnemyConfig config)
    {
        var enemy = _enemyScene.Instantiate<Enemy>();
        enemy.Config = config;
        GetTree().Root.GetNode("Main/Enemies").AddChild(enemy);
        enemy.Initialize(Waypoints);
        enemy.AddToGroup("enemies");
    }
}
