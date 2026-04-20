# 01_godot_basics

## Purpose

Phase 1 learning project. A 2D collect game (Collector) built with Godot 4.6.1 + C# (.NET 10).

---

## Architecture

```
                    Resource-based EventChannel (decoupled)
                    on_coin_collected.tres ─────────────┐
                    on_hazard_hit.tres ─────────────┐   │
                    on_score_changed.tres ──────┐   │   │
                    on_lives_changed.tres ──┐   │   │   │
                    on_wave_cleared.tres ┐  │   │   │   │
                    on_game_over.tres ┐  │  │   │   │   │
                                      │  │  │   │   │   │
  ┌───────────────┐  Raise()     ┌────┴──┴──┴───┴───┴───┴──┐  Raised +=     ┌─────────┐
  │  Coin         │─────────────→│     EventChannel         │←──────────────│  HUD    │
  │  (Area2D)     │              │     (Resource)           │               │(Canvas  │
  └───────────────┘              │                          │               │ Layer)  │
  ┌───────────────┐  Raise()     │  Neither publisher nor   │  Raised +=    └─────────┘
  │  Hazard       │─────────────→│  subscriber know each    │←──────────────┐
  │  (Area2D)     │              └──────────┬───────────────┘               │
  └───────────────┘                         │                              │
                                   Raised +=│                    ┌─────────┴────────┐
                                            │                    │  GameManager     │
                                            └───────────────────→│  (Node2D)        │
                                                                 │  Humble Object   │
                                                                 │  Shell           │
                                                                 └────────┬─────────┘
                                                                          │
                                                              ┌───────────┴──────────┐
                                                              │  GameStateCalculator  │
                                                              │  (static, pure)      │
                                                              │  No Godot dependency  │
                                                              └───────────┬──────────┘
                                                                          │
                                                              ┌───────────┴──────────┐
                                                              │  GameStateData       │
                                                              │  (struct, fields)    │
                                                              └──────────────────────┘
```

---

## Scene Tree

```
Main (Node2D) ── GameManager.cs
│   [Export] Config = game_config.tres
│   [Export] OnCoinCollected = on_coin_collected.tres
│   [Export] OnHazardHit = on_hazard_hit.tres
│   [Export] OnScoreChanged = on_score_changed.tres
│   [Export] OnLivesChanged = on_lives_changed.tres
│   [Export] OnWaveCleared = on_wave_cleared.tres
│   [Export] OnGameOver = on_game_over.tres
│
├── Player (CharacterBody2D) ── Player.cs, layer=1 mask=2
│   └── CollisionShape2D ── RectangleShape2D 40x40
│
├── Walls (Node2D) ── StaticBody2D x4 generated in _Ready()
│   ├── WallTop (StaticBody2D)
│   │   └── CollisionShape2D
│   ├── WallBottom (StaticBody2D)
│   │   └── CollisionShape2D
│   ├── WallLeft (StaticBody2D)
│   │   └── CollisionShape2D
│   └── WallRight (StaticBody2D)
│       └── CollisionShape2D
│
├── Coins (Node2D) ── Coin.tscn instantiated per wave
│   └── Coin (Area2D) ── layer=4 mask=1
│       └── CollisionShape2D
│
├── Hazards (Node2D) ── Hazard.tscn instantiated on timer
│   └── Hazard (Area2D) ── layer=8 mask=1
│       └── CollisionShape2D
│
└── UI (CanvasLayer) ── HUD.cs
    │   [Export] OnScoreChanged = on_score_changed.tres
    │   [Export] OnLivesChanged = on_lives_changed.tres
    │   [Export] OnWaveCleared = on_wave_cleared.tres
    │   [Export] OnGameOver = on_game_over.tres
    │
    ├── ScoreLabel (Label, 36px)
    ├── LivesLabel (Label, 36px)
    ├── WaveLabel (Label, 36px)
    └── GameOverLabel (Label, 60px)
```

---

## Collision layers

```
               Detection target (collision_mask)
               bit0     bit1     bit2     bit3
              Player    Wall     Coin    Hazard
  ┌─────────┬────────┬────────┬────────┬────────┐
  │ Player  │        │   ●    │        │        │
  │ bit0    │        │ slides │        │        │
  ├─────────┼────────┼────────┼────────┼────────┤
  │ Wall    │        │        │        │        │
  │ bit1    │        │        │        │        │
  ├─────────┼────────┼────────┼────────┼────────┤
  │ Coin    │   ●    │        │        │        │
  │ bit2    │ detect │        │        │        │
  ├─────────┼────────┼────────┼────────┼────────┤
  │ Hazard  │   ●    │        │        │        │
  │ bit3    │ detect │        │        │        │
  └─────────┴────────┴────────┴────────┴────────┘
```

---

## Signal flow (EventChannel)

```
  Coin.BodyEntered                              HUD.HandleScoreChanged
       │                                              ▲
       ▼                                              │
  OnCollected.Raise()                           OnScoreChanged.Raised
       │                                              ▲
       ▼                                              │
  on_coin_collected.tres ──→ GameManager.HandleCoinCollected
       (VoidEventChannel)         │
                                  ├── GameStateCalculator.CollectCoin(state)
                                  ├── OnScoreChanged.Raise(score)
                                  └── if IsWaveCleared: CallDeferred(BeginNextWave)


  Hazard.BodyEntered                            HUD.HandleLivesChanged
       │                                              ▲
       ▼                                              │
  OnHitPlayer.Raise()                           OnLivesChanged.Raised
       │                                              ▲
       ▼                                              │
  on_hazard_hit.tres ────→ GameManager.HandleHazardHit
       (VoidEventChannel)         │
                                  ├── GameStateCalculator.HitByHazard(state)
                                  ├── OnLivesChanged.Raise(lives)
                                  └── if IsGameOver: StopGame() → OnGameOver.Raise()
```

---

## State transitions

```
  ┌─────────────────────────┐
  │  CreateInitial(lives=3) │
  │  score=0 wave=0         │
  │  coinsRemaining=0       │
  └───────────┬─────────────┘
              │ StartWave(coinsPerWave=5)
              ▼
  ┌─────────────────────────┐
  │  Playing                │
  │  wave=N coinsRemaining=5│◄──────────────┐
  └─────┬─────────────┬─────┘               │
        │             │                     │
   CollectCoin    HitByHazard               │
        │             │                     │
        ▼             ▼                     │
   score++        lives--                   │
   coins--        IsGameOver?               │
   IsWaveCleared?     │                     │
        │         ┌───┴───┐                 │
        │        No      Yes                │
        │         │       │                 │
        │         ▼       ▼                 │
        │   invincible  ┌──────────┐        │
        │   1.5s        │ GameOver │        │
        │               │ Press R  │        │
        │               └────┬─────┘        │
        │                    │ Restart       │
   ┌────┴────┐               │              │
   │coins<=0 │               ▼              │
   └────┬────┘          ClearAll            │
        │               ResetState          │
        └───── StartWave ───────────────────┘
```

---

## Lifecycle (Godot vs Unity)

```
  Frame order              Godot                          Unity
  ────────────────────────────────────────────────────────────────
       │
       ▼
  _Ready()                 Node enters tree               Start()
       │                   GameManager: build walls,
       │                   subscribe EventChannels
       ▼
  _PhysicsProcess(δ)       Fixed timestep                 FixedUpdate()
       │                   Player: Input → MoveAndSlide
       │                   Hazard: position += dir * δ
       │                   δ is double (cast to float)
       ▼
  _Process(δ)              Variable framerate             Update()
       │                   GameManager: hazard timer
       │                   δ is double (cast to float)
       ▼
  _UnhandledInput(ev)      Unconsumed input only          (no equivalent)
       │                   GameManager: R key restart
       │                   UI consumes first → this last
       ▼
  _Draw()                  On QueueRedraw()               OnRenderObject()
       │                   Player/Coin/Hazard: DrawRect
       │                   CanvasItem only (Node2D)
       ▼
  next frame
```

---

## Unity to Godot mapping demonstrated

| Unity | Godot | Where |
|-------|-------|-------|
| GameObject + Component | Node + child Node | Walls: StaticBody2D > CollisionShape2D |
| CharacterController | CharacterBody2D + MoveAndSlide | Player.cs |
| Trigger Collider | Area2D + monitoring | Coin.tscn, Hazard.tscn |
| Static Rigidbody | StaticBody2D | GameManager.cs BuildWalls() |
| EventChannelSO | Resource + Signal (.tres) | VoidEventChannel, IntEventChannel |
| ScriptableObject | Resource + [GlobalClass] | GameConfig.cs |
| [SerializeField] | [Export] | All scripts |
| Start() | _Ready() | All scripts |
| Update() | _Process(double) | GameManager.cs |
| FixedUpdate() | _PhysicsProcess(double) | Player.cs, Hazard.cs |
| Prefab | PackedScene (.tscn) | Coin.tscn, Hazard.tscn |
| Instantiate(prefab) | PackedScene.Instantiate\<T\>() | GameManager.cs |
| Destroy(go) | QueueFree() | Coin.cs, Hazard.cs |
| Screen Space Canvas | CanvasLayer | HUD.cs |
| Humble Object | GameStateData + Calculator | GameState.cs, GameStateCalculator.cs |
| Layer Collision Matrix | collision_layer / collision_mask | Main.tscn, Coin.tscn, Hazard.tscn |

---

## Project structure

```
01_godot_basics/
├── project.godot
├── 01_godot_basics.csproj
├── 01_godot_basics.sln
├── Scripts/
│   ├── VoidEventChannel.cs        EventChannel (void)
│   ├── IntEventChannel.cs         EventChannel (int)
│   ├── GameStateData.cs           State struct (pure)
│   ├── GameStateCalculator.cs     Logic (pure static)
│   ├── GameConfig.cs              Config Resource
│   ├── GameManager.cs             Orchestrator shell
│   ├── Player.cs                  CharacterBody2D
│   ├── Coin.cs                    Area2D collectible
│   ├── Hazard.cs                  Area2D threat
│   └── HUD.cs                    CanvasLayer UI
├── Scenes/
│   ├── Main.tscn
│   ├── Coin.tscn
│   └── Hazard.tscn
└── Resources/
    ├── game_config.tres
    └── Events/
        ├── on_coin_collected.tres
        ├── on_hazard_hit.tres
        ├── on_score_changed.tres
        ├── on_lives_changed.tres
        ├── on_wave_cleared.tres
        └── on_game_over.tres
```

---

## Environment

- Godot 4.6.1 (.NET build, install via `brew install --cask godot-mono`)
- .NET SDK 10+
- WQHD (2560x1440) fullscreen
