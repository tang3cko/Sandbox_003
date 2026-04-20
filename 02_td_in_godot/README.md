# 02_td_in_godot

## Purpose

Phase 2-3 learning project. A 3D Tower Defense game built with Godot 4.6.2 + C# (.NET 10).

Demonstrates Signal + Resource architecture (ReactiveSO) and Humble Object pattern with pure function TDD.

---

## Architecture

```
                    Resource-based ReactiveSO (decoupled)
                    ┌──────────────────────────────────────────┐
                    │  EventChannels (.tres)                   │
                    │  on_enemy_killed, on_enemy_reached_end,  │
                    │  on_gold_earned, on_game_over,           │
                    │  on_wave_completed, on_tower_placed      │
                    │                                          │
                    │  Variables (.tres)                       │
                    │  gold, lives, score, wave_number         │
                    │                                          │
                    │  RuntimeSets (.tres)                     │
                    │  active_enemies, placed_towers           │
                    └──────┬───────────────────────┬───────────┘
                           │                       │
              Raise() / Add() / Value =    Raised += / Items
                           │                       │
  ┌────────────┐    ┌──────┴──────┐    ┌───────────┴──────────┐
  │  Enemy     │───→│  Resource   │←───│  GameManager         │
  │  (Node3D)  │    │  Layer      │    │  (Humble Object      │
  └────────────┘    │             │    │   Shell)              │
  ┌────────────┐    │  No direct  │    └──────────┬───────────┘
  │  Tower     │───→│  references │               │
  │  (Node3D)  │    │  between    │    ┌──────────┴───────────┐
  └────────────┘    │  systems    │    │  GameStateCalculator  │
  ┌────────────┐    │             │    │  (static, pure)      │
  │  HUD       │←───│             │    │  No Godot dependency │
  │  (Control) │    └─────────────┘    └──────────┬───────────┘
  └────────────┘                                  │
                                       ┌──────────┴───────────┐
                                       │  GameState           │
                                       │  (struct, fields)    │
                                       └──────────────────────┘
```

---

## Scene Tree

```
Main (Node3D) ── GameManager.cs
│   [Export] Config = game_config.tres
│   [Export] _gold = gold.tres
│   [Export] _lives = lives.tres
│   [Export] _score = score.tres
│   [Export] _waveNumber = wave_number.tres
│   [Export] _onEnemyKilled = on_enemy_killed.tres
│   [Export] _onEnemyReachedEnd = on_enemy_reached_end.tres
│   [Export] _onGoldEarned = on_gold_earned.tres
│   [Export] _onGameOver = on_game_over.tres
│   [Export] _onWaveCompleted = on_wave_completed.tres
│   [Export] _activeEnemies = active_enemies.tres
│
├── WorldEnvironment
├── Camera3D (isometric-style, y=25)
├── DirectionalLight3D (shadow enabled)
├── Ground (MeshInstance3D, PlaneMesh 40x40)
│
├── Enemies (Node3D) ── spawned Enemy instances
│   └── Enemy (Node3D) ── Enemy.cs
│       [Export] _activeEnemies = active_enemies.tres
│       [Export] _onEnemyKilled = on_enemy_killed.tres
│       [Export] _onEnemyReachedEnd = on_enemy_reached_end.tres
│       [Export] _onGoldEarned = on_gold_earned.tres
│
├── Towers (Node3D) ── placed Tower instances
│   └── Tower (Node3D) ── Tower.cs
│       [Export] _activeEnemies = active_enemies.tres
│       [Export] _placedTowers = placed_towers.tres
│
├── WaveManager (Node3D) ── WaveManager.cs
│   [Export] Waypoints = PackedVector3Array(8 points)
│
├── TowerPlacement (Node3D) ── TowerPlacement.cs
│   [Export] _placedTowers = placed_towers.tres
│   [Export] _gold = gold.tres
│   [Export] _onTowerPlaced = on_tower_placed.tres
│
├── Setup (Node3D) ── MainSetup.cs (wires HUD buttons + camera)
│
└── CanvasLayer
    └── HUD (Control) ── HUD.cs
        [Export] _gold = gold.tres
        [Export] _lives = lives.tres
        [Export] _waveNumber = wave_number.tres
        [Export] _score = score.tres
        [Export] _onGameOver = on_game_over.tres
```

---

## Signal flow (ReactiveSO)

```
  Enemy.Die()                                    HUD._gold.ValueChanged
       │                                              ▲
       ▼                                              │
  _onEnemyKilled.Raise()                         gold.Value = state.Gold
  _onGoldEarned.Raise(reward)                         ▲
       │                                              │
       ▼                                              │
  on_enemy_killed.tres ──→ GameManager.HandleEnemyKilled
  on_gold_earned.tres ───→ GameManager.HandleGoldEarned
       (EventChannel)            │
                                 ├── GameStateCalculator.EnemyKilled(state, reward)
                                 └── _gold.Value = state.Gold


  Enemy.ReachEnd()                               HUD._lives.ValueChanged
       │                                              ▲
       ▼                                              │
  _onEnemyReachedEnd.Raise()                     lives.Value = state.Lives
       │                                              ▲
       ▼                                              │
  on_enemy_reached_end.tres ──→ GameManager.HandleEnemyReachedEnd
       (VoidEventChannel)              │
                                       ├── GameStateCalculator.EnemyReachedEnd(state, 1)
                                       ├── _lives.Value = state.Lives
                                       └── if IsGameOver: _onGameOver.Raise()


  TowerPlacement.PlaceTower()
       │
       ▼
  GameManager.TryPlaceTower(cost)
       │
       ├── GameStateCalculator.TryPlaceTower(state, cost)
       ├── _gold.Value = state.Gold
       └── return CanAfford
```

---

## State transitions

```
  ┌───────────────────────────┐
  │  CreateInitial            │
  │  gold=200 lives=20        │
  │  score=0 wave=0           │
  └───────────┬───────────────┘
              │ StartNextWave (2s delay)
              ▼
  ┌───────────────────────────┐
  │  Playing                  │
  │  wave=N                   │◄──────────────────┐
  └─────┬───────────┬─────────┘                   │
        │           │                             │
   EnemyKilled  EnemyReachedEnd                   │
        │           │                             │
        ▼           ▼                             │
   score+=10    lives -= damage                   │
   gold+=reward IsGameOver?                       │
        │           │                             │
        │       ┌───┴───┐                         │
        │      No      Yes                        │
        │       │       │                         │
        │       │       ▼                         │
        │       │  ┌──────────┐                   │
        │       │  │ GameOver │                   │
        │       │  │ Press R  │──→ Restart ───────┤
        │       │  └──────────┘                   │
        │       │                                 │
   CheckWaveComplete                              │
        │                                         │
   ┌────┴────────┐                                │
   │ all enemies │                                │
   │ cleared?    │                                │
   └────┬────────┘                                │
        │ Yes                                     │
   ┌────┴────────┐                                │
   │ last wave?  │                                │
   └──┬──────┬───┘                                │
     No     Yes                                   │
      │      │                                    │
      │      ▼                                    │
      │ ┌──────────┐                              │
      │ │ Victory! │                              │
      │ │ Press R  │──→ Restart ──────────────────┘
      │ └──────────┘
      │
      └── StartNextWave (3s delay) ───────────────┘
```

---

## ReactiveSO library (addons/ReactiveSO/)

| Pattern | Types | Purpose |
|---------|-------|---------|
| EventChannel | VoidEventChannel, IntEventChannel, FloatEventChannel, BoolEventChannel, StringEventChannel, Vector2EventChannel, Vector3EventChannel | Decoupled event notification |
| Variable | IntVariable, FloatVariable, BoolVariable, StringVariable, Vector2Variable, Vector3Variable | Reactive shared state with change detection |
| RuntimeSet | Node2DRuntimeSet, Node3DRuntimeSet | Dynamic entity collection tracking |

Based on [ReactiveSO](../EventChannels/) (Unity Asset Store), adapted for Godot's Resource + Signal system.

---

## Game design

| Element | Detail |
|---------|--------|
| Genre | 3D Tower Defense |
| Enemies | Goblin (balanced), Wolf (fast/fragile), Golem (slow/tanky) |
| Towers | Arrow (fast single-target), Cannon (slow AoE), Ice (slow effect) |
| Waves | 5 waves, progressive difficulty |
| Win | Survive all 5 waves |
| Lose | Lives reach 0 |
| Restart | Press R on game over / victory |

---

## Testing

Pure function tests via xUnit. No Godot runtime required.

```bash
cd Tests && dotnet test
```

```
Passed!  - Failed: 0, Passed: 12, Total: 12, Duration: 17ms
```

| Test class | Coverage |
|------------|----------|
| GameStateCalculatorTest | CreateInitial, EnemyKilled, EnemyReachedEnd, TryPlaceTower, AdvanceWave |

Humble Object pattern enables this: GameStateCalculator has zero Godot dependencies.

---

## Project structure

```
02_td_in_godot/
├── project.godot
├── 02_td_in_godot.csproj
├── addons/ReactiveSO/Runtime/
│   ├── Channels/                 EventChannel types (7)
│   ├── Variables/                Variable types (6)
│   └── RuntimeSets/             RuntimeSet types (2)
├── Scripts/
│   ├── Configs/
│   │   ├── EnemyConfig.cs        Enemy stats Resource
│   │   ├── TowerConfig.cs        Tower stats Resource
│   │   ├── WaveEntry.cs          Single wave entry Resource
│   │   ├── WaveConfig.cs         Wave composition Resource
│   │   └── GameConfig.cs         Game settings Resource
│   ├── Core/
│   │   ├── GameManager.cs        Orchestrator (Humble Object shell)
│   │   ├── GameStateCalculator.cs  Pure logic (no Godot dependency)
│   │   ├── WaveManager.cs        Enemy wave spawner
│   │   └── MainSetup.cs          Runtime wiring
│   ├── Enemies/
│   │   └── Enemy.cs              Path-following enemy
│   ├── Towers/
│   │   ├── Tower.cs              Auto-targeting tower
│   │   ├── Projectile.cs         Tower projectile
│   │   └── TowerPlacement.cs     Grid-based placement
│   └── UI/
│       └── HUD.cs                Gold, lives, wave, score, game over
├── Scenes/
│   ├── Main.tscn                 Game scene (all resources wired)
│   ├── Enemy.tscn                Enemy prefab
│   ├── Tower.tscn                Tower prefab
│   └── Projectile.tscn           Projectile prefab
├── Resources/
│   ├── game_config.tres          Game settings (5 waves, 200 gold, 20 lives)
│   ├── Events/                   EventChannel instances (11)
│   ├── Variables/                Variable instances (4)
│   ├── RuntimeSets/              RuntimeSet instances (2)
│   ├── Enemies/                  EnemyConfig instances (3)
│   ├── Towers/                   TowerConfig instances (3)
│   └── Waves/                    WaveConfig + WaveEntry instances
└── Tests/
    ├── Tests.csproj              xUnit test project (net10.0)
    └── GameStateCalculatorTest.cs  12 tests, all passing
```

---

## Environment

- Godot 4.6.2 (.NET build, install via `brew install --cask godot-mono`)
- .NET SDK 10+
