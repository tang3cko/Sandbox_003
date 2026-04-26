namespace SwarmSurvivor;

using Godot;

public partial class MultiplayerGameSetup : Node3D
{
    private const string PlayerScenePath = "res://Scenes/NetworkedPlayer.tscn";
    private const string WaveConfigPath = "res://Resources/Waves/wave_config.tres";

    private Node3D _playersContainer;
    private MultiplayerSpawner _spawner;
    private PackedScene _playerScene;

    private SwarmManager _swarmManager;
    private SwarmSpawner _swarmSpawner;
    private GameStateSync _gameState;
    private MultiplayerHUD _hud;

    public override void _Ready()
    {
        SetupArena();
        SetupCamera();
        SetupLighting();
        SetupPlayersContainer();
        SetupGameState();

        _playerScene = GD.Load<PackedScene>(PlayerScenePath);

        if (NetworkManager.Instance == null)
        {
            GD.PrintErr("[MultiplayerGameSetup] NetworkManager autoload missing");
            return;
        }

        var waveConfig = GD.Load<WaveConfig>(WaveConfigPath);
        if (waveConfig == null)
        {
            GD.PrintErr($"[MultiplayerGameSetup] Failed to load WaveConfig at {WaveConfigPath}");
        }

        if (NetworkManager.Instance.IsServer)
        {
            NetworkManager.Instance.PeerJoined += OnPeerJoined;
            NetworkManager.Instance.PeerLeft += OnPeerLeft;

            if (waveConfig != null) SetupServerSwarm(waveConfig);

            SpawnPlayer(NetworkConfig.ServerPeerId);
            foreach (var peerId in Multiplayer.GetPeers())
            {
                SpawnPlayer(peerId);
            }
        }
        else
        {
            if (waveConfig != null) SetupClientSwarm(waveConfig);
        }

        SetupHUD();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Multiplayer.MultiplayerPeer == null) return;
        if (!Multiplayer.IsServer()) return;
        if (@event.IsActionPressed("start_swarm"))
        {
            _swarmSpawner?.StartSpawning();
            GetViewport().SetInputAsHandled();
        }
    }

    private void SetupServerSwarm(WaveConfig waveConfig)
    {
        _swarmManager = new SwarmManager { Name = "SwarmManager", WaveConfig = waveConfig };
        var renderer = new SwarmRenderer { Name = "SwarmRenderer" };
        _swarmManager.AddChild(renderer);
        AddChild(_swarmManager);

        _swarmSpawner = new SwarmSpawner
        {
            Name = "SwarmSpawner",
            WaveConfig = waveConfig,
            AutoStart = false,
            GameState = _gameState,
        };
        AddChild(_swarmSpawner);

        var sync = new SwarmNetworkSync { Name = "SwarmNetworkSync", WaveConfig = waveConfig };
        AddChild(sync);

        GD.Print("[MultiplayerGameSetup] Server swarm initialized (Press Enter to start spawning).");
    }

    private void SetupGameState()
    {
        _gameState = new GameStateSync { Name = "GameStateSync" };
        AddChild(_gameState);
    }

    private void SetupHUD()
    {
        _hud = new MultiplayerHUD
        {
            Name = "HUD",
            PlayersContainer = _playersContainer,
            GameState = _gameState,
        };
        AddChild(_hud);
    }

    private void SetupClientSwarm(WaveConfig waveConfig)
    {
        var sync = new SwarmNetworkSync { Name = "SwarmNetworkSync", WaveConfig = waveConfig };
        var renderer = new SwarmRenderer { Name = "SwarmRenderer" };
        sync.AddChild(renderer);
        AddChild(sync);

        GD.Print("[MultiplayerGameSetup] Client swarm receiver initialized.");
    }

    public override void _ExitTree()
    {
        if (NetworkManager.Instance == null) return;

        NetworkManager.Instance.PeerJoined -= OnPeerJoined;
        NetworkManager.Instance.PeerLeft -= OnPeerLeft;
    }

    private void OnPeerJoined(long peerId)
    {
        SpawnPlayer(peerId);
    }

    private void OnPeerLeft(long peerId)
    {
        var name = peerId.ToString();
        var player = _playersContainer.GetNodeOrNull<Node3D>(name);
        if (player == null) return;

        if (player is NetworkedPlayer networkedPlayer
            && Multiplayer.MultiplayerPeer != null
            && Multiplayer.IsServer())
        {
            networkedPlayer.PlayerDied -= OnPlayerDied;
        }

        _swarmManager?.UnregisterTarget(player);
        player.QueueFree();
        GD.Print($"[MultiplayerGameSetup] Player removed: peer={peerId}");
    }

    private void SpawnPlayer(long peerId)
    {
        if (_playersContainer.HasNode(peerId.ToString()))
        {
            GD.Print($"[MultiplayerGameSetup] Skip spawn (already exists): peer={peerId}");
            return;
        }

        GD.Print($"[MultiplayerGameSetup] Requesting spawn: peer={peerId}");
        var spawned = _spawner.Spawn((int)peerId);

        if (spawned is NetworkedPlayer player)
        {
            _swarmManager?.RegisterTarget(player);
            if (Multiplayer.MultiplayerPeer != null && Multiplayer.IsServer())
            {
                player.PlayerDied += OnPlayerDied;
            }
            GD.Print($"[MultiplayerGameSetup] Swarm target registered: peer={peerId}");
        }
    }

    private void OnPlayerDied(long peerId)
    {
        var player = _playersContainer.GetNodeOrNull<Node3D>(peerId.ToString());
        if (player != null) _swarmManager?.UnregisterTarget(player);
        GD.Print($"[MultiplayerGameSetup] Player died, unregistered: peer={peerId}");
    }

    private NetworkedPlayer OnSpawnPlayer(Variant data)
    {
        var peerId = data.AsInt32();
        var spawnPos = GetSpawnPosition(peerId);

        var player = _playerScene.Instantiate<NetworkedPlayer>();
        player.Name = peerId.ToString();
        player.SetMultiplayerAuthority(peerId);

        var healthSync = player.GetNodeOrNull<MultiplayerSynchronizer>("HealthSync");
        healthSync?.SetMultiplayerAuthority(NetworkConfig.ServerPeerId, false);

        player.Position = spawnPos;

        GD.Print($"[MultiplayerGameSetup] SpawnFunction: peer={peerId} authority set, pos={spawnPos}");
        return player;
    }

    private static Vector3 GetSpawnPosition(long peerId)
    {
        var index = peerId == NetworkConfig.ServerPeerId ? 0 : (int)(peerId % 8);
        var angle = index * Mathf.Tau / 8f;
        return new Vector3(Mathf.Cos(angle) * 4f, 0f, Mathf.Sin(angle) * 4f);
    }

    private void SetupArena()
    {
        var ground = new StaticBody3D();
        var groundMesh = new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(50f, 50f) },
        };
        var fallback = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.1f, 0.12f, 0.15f),
        };
        groundMesh.MaterialOverride = fallback;
        ground.AddChild(groundMesh);

        var groundShape = new CollisionShape3D
        {
            Shape = new WorldBoundaryShape3D(),
        };
        ground.AddChild(groundShape);

        AddChild(ground);
    }

    private void SetupCamera()
    {
        var camera = new Camera3D
        {
            Position = new Vector3(0f, 25f, 15f),
            RotationDegrees = new Vector3(-60f, 0f, 0f),
            Projection = Camera3D.ProjectionType.Perspective,
            Fov = 50f,
        };
        AddChild(camera);
    }

    private void SetupLighting()
    {
        var light = new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-45f, -30f, 0f),
            ShadowEnabled = true,
            LightEnergy = 0.8f,
        };
        AddChild(light);

        var env = new WorldEnvironment();
        var environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.02f, 0.03f, 0.05f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.15f, 0.15f, 0.2f),
            AmbientLightEnergy = 0.5f,
        };
        env.Environment = environment;
        AddChild(env);
    }

    private void SetupPlayersContainer()
    {
        _playersContainer = new Node3D { Name = "Players" };
        AddChild(_playersContainer);

        _spawner = new MultiplayerSpawner { Name = "PlayerSpawner" };
        AddChild(_spawner);
        _spawner.SpawnPath = _playersContainer.GetPath();
        _spawner.SpawnFunction = new Callable(this, MethodName.OnSpawnPlayer);
    }
}
