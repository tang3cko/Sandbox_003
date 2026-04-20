namespace Collector;

using Godot;

public partial class GameManager : Node2D
{
    private GameStateData state;
    private float hazardTimer;

    private Player player;
    private Node2D wallsContainer;
    private Node2D coinsContainer;
    private Node2D hazardsContainer;
    private PackedScene coinScene;
    private PackedScene hazardScene;
    private Rect2 arenaBounds;
    private RandomNumberGenerator rng;
    private Vector2 playerStartPosition;

    [Export] public GameConfig Config { get; private set; }
    [Export] public VoidEventChannel OnCoinCollected { get; private set; }
    [Export] public VoidEventChannel OnHazardHit { get; private set; }
    [Export] public IntEventChannel OnScoreChanged { get; private set; }
    [Export] public IntEventChannel OnLivesChanged { get; private set; }
    [Export] public IntEventChannel OnWaveCleared { get; private set; }
    [Export] public VoidEventChannel OnGameOver { get; private set; }

    public override void _Ready()
    {
        if (Config == null)
        {
            GD.PrintErr($"[{GetType().Name}] Config not assigned.");
            return;
        }

        coinScene = GD.Load<PackedScene>("res://Scenes/Coin.tscn");
        if (coinScene == null)
        {
            GD.PrintErr($"[{GetType().Name}] Failed to load Coin.tscn.");
            return;
        }

        hazardScene = GD.Load<PackedScene>("res://Scenes/Hazard.tscn");
        if (hazardScene == null)
        {
            GD.PrintErr($"[{GetType().Name}] Failed to load Hazard.tscn.");
            return;
        }

        rng = new RandomNumberGenerator();
        rng.Randomize();

        player = GetNode<Player>("Player");
        wallsContainer = GetNode<Node2D>("Walls");
        coinsContainer = GetNode<Node2D>("Coins");
        hazardsContainer = GetNode<Node2D>("Hazards");

        playerStartPosition = player.Position;
        player.Initialize(Config.PlayerSpeed);

        arenaBounds = new Rect2(0, 0, Config.ArenaWidth, Config.ArenaHeight);
        BuildWalls();
        CenterArena();

        OnCoinCollected.Raised += HandleCoinCollected;
        OnHazardHit.Raised += HandleHazardHit;

        StartGame();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!state.isGameOver)
            return;

        if (@event is InputEventKey { Pressed: true } keyEvent && keyEvent.Keycode == Key.R)
        {
            Restart();
        }
    }

    public override void _Process(double delta)
    {
        if (state.isGameOver)
            return;

        hazardTimer -= (float)delta;
        if (hazardTimer <= 0f)
        {
            SpawnHazard();
            hazardTimer = Config.HazardSpawnInterval;
        }
    }

    private void StartGame()
    {
        state = GameStateCalculator.CreateInitial(Config.StartingLives);
        hazardTimer = Config.HazardSpawnInterval;

        player.SetActive(true);

        OnLivesChanged?.Raise(state.lives);
        OnScoreChanged?.Raise(state.score);

        BeginNextWave();
    }

    private void Restart()
    {
        ClearAllEntities();

        player.Position = playerStartPosition;
        player.Modulate = Colors.White;

        StartGame();
    }

    private void StopGame()
    {
        player.SetActive(false);

        foreach (var child in hazardsContainer.GetChildren())
        {
            if (child is Hazard hazard)
            {
                hazard.SetPhysicsProcess(false);
            }
        }

        OnGameOver?.Raise();
    }

    private void ClearAllEntities()
    {
        foreach (var child in coinsContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var child in hazardsContainer.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void BuildWalls()
    {
        float w = Config.ArenaWidth;
        float h = Config.ArenaHeight;
        float t = Config.WallThickness;

        CreateWall(new Vector2(w / 2, -t / 2), new Vector2(w + t * 2, t));
        CreateWall(new Vector2(w / 2, h + t / 2), new Vector2(w + t * 2, t));
        CreateWall(new Vector2(-t / 2, h / 2), new Vector2(t, h));
        CreateWall(new Vector2(w + t / 2, h / 2), new Vector2(t, h));
    }

    private void CreateWall(Vector2 position, Vector2 size)
    {
        var wall = new StaticBody2D();
        wall.Position = position;

        var shape = new CollisionShape2D();
        var rect = new RectangleShape2D();
        rect.Size = size;
        shape.Shape = rect;
        wall.AddChild(shape);

        wallsContainer.AddChild(wall);
    }

    private void BeginNextWave()
    {
        state = GameStateCalculator.StartWave(state, Config.CoinsPerWave);
        OnWaveCleared?.Raise(state.wave);

        float margin = 60f;
        for (int i = 0; i < Config.CoinsPerWave; i++)
        {
            var coin = coinScene.Instantiate<Coin>();
            coin.Position = new Vector2(
                rng.RandfRange(margin, Config.ArenaWidth - margin),
                rng.RandfRange(margin, Config.ArenaHeight - margin)
            );
            coinsContainer.AddChild(coin);
        }
    }

    private void SpawnHazard()
    {
        var hazard = hazardScene.Instantiate<Hazard>();

        int edge = rng.RandiRange(0, 3);
        Vector2 position;
        Vector2 direction;
        float margin = 50f;

        switch (edge)
        {
            case 0:
                position = new Vector2(rng.RandfRange(0, Config.ArenaWidth), -margin);
                direction = Vector2.Down;
                break;
            case 1:
                position = new Vector2(rng.RandfRange(0, Config.ArenaWidth), Config.ArenaHeight + margin);
                direction = Vector2.Up;
                break;
            case 2:
                position = new Vector2(-margin, rng.RandfRange(0, Config.ArenaHeight));
                direction = Vector2.Right;
                break;
            default:
                position = new Vector2(Config.ArenaWidth + margin, rng.RandfRange(0, Config.ArenaHeight));
                direction = Vector2.Left;
                break;
        }

        hazard.Position = position;
        hazard.Initialize(Config.HazardSpeed, direction, arenaBounds);
        hazardsContainer.AddChild(hazard);
    }

    private void HandleCoinCollected()
    {
        var result = GameStateCalculator.CollectCoin(state);
        state = result.State;
        OnScoreChanged?.Raise(state.score);

        if (result.IsWaveCleared)
        {
            CallDeferred(MethodName.BeginNextWave);
        }
    }

    private void HandleHazardHit()
    {
        var result = GameStateCalculator.HitByHazard(state);
        state = result.State;
        OnLivesChanged?.Raise(state.lives);

        if (result.IsGameOver)
        {
            StopGame();
        }
        else
        {
            player.StartInvincibility(1.5f);
        }
    }

    private void CenterArena()
    {
        var viewport = GetViewport().GetVisibleRect().Size;
        var offset = (viewport - new Vector2(Config.ArenaWidth, Config.ArenaHeight)) / 2;
        Position = offset;
    }
}
