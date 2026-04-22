# 03_action_in_godot

## Purpose

Phase 4 learning project. A 3D arena survival action game built with Godot 4.6.2 + C# (.NET 10).

Demonstrates Physics (Area3D), Navigation, Animation (AnimationPlayer + AnimationTree), Audio (spatial + bus routing), Particles (GPUParticles3D), and Scene management.

---

## Architecture

```
                    Resource-based ReactiveSO (decoupled)
                    ┌──────────────────────────────────────────┐
                    │  EventChannels (.tres)                   │
                    │  on_player_died, on_player_damaged,      │
                    │  on_player_attacked, on_player_dodged,   │
                    │  on_enemy_killed, on_score_earned,       │
                    │  on_wave_completed, on_game_over         │
                    │                                          │
                    │  Variables (.tres)                       │
                    │  health, stamina, score, wave_number     │
                    │                                          │
                    │  RuntimeSets (.tres)                     │
                    │  active_enemies                          │
                    └──────┬───────────────────────┬───────────┘
                           │                       │
              Raise() / Add() / Value =    Raised += / Items
                           │                       │
  ┌────────────┐    ┌──────┴──────┐    ┌───────────┴──────────┐
  │  Enemy     │───→│  Resource   │←───│  GameManager         │
  │  (Node3D)  │    │  Layer      │    │  (Humble Object      │
  └────────────┘    │             │    │   Shell)              │
  ┌────────────┐    │  No direct  │    └──────────┬───────────┘
  │  Player    │───→│  references │               │
  │ Controller │    │  between    │    ┌──────────┴───────────┐
  └────────────┘    │  systems    │    │  CombatCalculator    │
  ┌────────────┐    │             │    │  WaveCalculator      │
  │  HUD       │←───│             │    │  (static, pure)      │
  │  (Control) │    └─────────────┘    └──────────┬───────────┘
  └────────────┘                                  │
                                       ┌──────────┴───────────┐
                                       │  PlayerState         │
                                       │  WaveState           │
                                       │  (struct, fields)    │
                                       └──────────────────────┘
```

---

## Scene Tree

```
Title (Control) ── TitleScreen.cs
    Press any key → ChangeSceneToFile("Main.tscn")

Main (Node3D) ── GameManager.cs
│   [Export] PlayerConfig, WaveConfig, WeaponConfig, ProjectileConfig
│   [Export] _health, _stamina, _score, _waveNumber (Variables)
│   [Export] _onPlayerDied, _onPlayerAttacked, _onPlayerDodged (EventChannels)
│   [Export] _onPlayerDamaged, _onEnemyKilled, _onScoreEarned
│   [Export] _onWaveCompleted, _onGameOver
│   [Export] _activeEnemies (RuntimeSet)
│
├── WorldEnvironment
├── Camera3D (isometric-style, y=18)
├── DirectionalLight3D (shadow enabled)
├── Ground (StaticBody3D, PlaneMesh 50x50)
├── NavigationRegion3D (NavigationMesh 50x50)
│
├── Enemies (Node3D) ── spawned Enemy instances
│   └── Enemy (Node3D) ── Enemy.cs
│       ├── NavigationAgent3D
│       ├── MeshInstance3D (CapsuleMesh, code-generated)
│       ├── Area3D "Hitbox" (Layer 5 = EnemyHitbox)
│       │   └── CollisionShape3D (CapsuleShape3D)
│       └── EnemyAnimator (Node) ── EnemyAnimator.cs
│           ├── AnimationPlayer (walk, hit, death)
│           └── AnimationTree (StateMachine)
│
├── Player (CharacterBody3D) ── PlayerController.cs
│   ├── CollisionShape3D (CapsuleShape3D)
│   ├── MeshInstance3D (CapsuleMesh, blue)
│   ├── WeaponPivot (Node3D)
│   │   ├── MeshInstance3D (BoxMesh, sword)
│   │   └── Area3D "MeleeArea" (Layer 4 = PlayerHitbox, Mask = EnemyHitbox)
│   │       └── CollisionShape3D (BoxShape3D)
│   ├── MovementDust (GPUParticles3D) ── MovementDust.cs
│   ├── PlayerAnimator (Node) ── PlayerAnimator.cs
│   │   ├── AnimationPlayer (idle, walk, attack, dodge, RESET)
│   │   └── AnimationTree (StateMachine)
│   └── PlayerCombat (Node3D) ── PlayerCombat.cs
│
├── EnemySpawner (Node3D) ── EnemySpawner.cs
├── AudioManager (Node3D) ── AudioManager.cs
├── ScreenShake (Node) ── ScreenShake.cs
├── AmbientParticles (GPUParticles3D) ── AmbientParticles.cs
│
├── CanvasLayer (ProcessMode = Always)
│   ├── HUD (Control) ── HUD.cs
│   └── PauseMenu (Control) ── PauseMenu.cs
│
└── Setup (Node) ── MainSetup.cs
```

---

## Collision layers

```
               Detection target (collision_mask)
               bit0     bit1     bit2     bit3     bit4     bit5
              Player   Enemy   Environ  PlyHit   EnmHit   Projct
  ┌─────────┬────────┬────────┬────────┬────────┬────────┬────────┐
  │ Player  │        │        │        │        │        │        │
  │ bit0    │        │        │        │        │        │        │
  ├─────────┼────────┼────────┼────────┼────────┼────────┼────────┤
  │ Enemy   │        │        │        │        │        │        │
  │ bit1    │        │        │        │        │        │        │
  ├─────────┼────────┼────────┼────────┼────────┼────────┼────────┤
  │ Environ │        │        │        │        │        │        │
  │ bit2    │        │        │        │        │        │        │
  ├─────────┼────────┼────────┼────────┼────────┼────────┼────────┤
  │ PlyHit  │        │        │        │        │   ●    │        │
  │ bit3    │        │        │        │        │ melee  │        │
  ├─────────┼────────┼────────┼────────┼────────┼────────┼────────┤
  │ EnmHit  │        │        │        │        │        │        │
  │ bit4    │        │        │        │        │        │        │
  ├─────────┼────────┼────────┼────────┼────────┼────────┼────────┤
  │ Projct  │        │        │        │        │   ●    │        │
  │ bit5    │        │        │        │        │ bullet │        │
  └─────────┴────────┴────────┴────────┴────────┴────────┴────────┘
```

---

## Signal flow (ReactiveSO)

```
  PlayerCombat.TryMeleeAttack()                  HUD._health.ValueChanged
       │                                              ▲
       ▼                                              │
  _onPlayerAttacked.Raise()                      health.Value = state.Health
       │                                              ▲
       ▼                                              │
  on_player_attacked.tres ──→ AudioManager.HandleAttack
       (VoidEventChannel)

  Enemy.TakeDamage() → Die()                     HUD._score.ValueChanged
       │                                              ▲
       ▼                                              │
  _onEnemyKilled.Raise()                         score.Value += amount
  _onScoreEarned.Raise(reward)                        ▲
       │                                              │
       ▼                                              │
  on_enemy_killed.tres ──→ GameManager.HandleEnemyKilled
  on_score_earned.tres ──→ GameManager.HandleScoreEarned
       (EventChannel)            │
                                 ├── ScreenShake.Shake()
                                 └── _score.Value += amount

  PlayerController.TakeDamage()
       │
       ├── CombatCalculator.TakeDamage(state, damage)
       ├── _onPlayerDamaged.Raise(damage)
       └── if IsDead: _onPlayerDied.Raise()
                            │
                            ▼
                  GameManager.HandlePlayerDied()
                            │
                            ├── _onGameOver.Raise()
                            └── GetTree().Paused = true
```

---

## State transitions

```
  ┌──────────────────────────────┐
  │  Title Screen                │
  │  Press any key               │
  └───────────┬──────────────────┘
              │ ChangeSceneToFile
              ▼
  ┌──────────────────────────────┐
  │  CreateInitial               │
  │  health=100 stamina=100      │
  │  score=0 wave=0              │
  └───────────┬──────────────────┘
              │ WaveCalculator.StartNextWave
              ▼
  ┌──────────────────────────────┐
  │  Playing                     │
  │  wave=N enemies spawning     │◄──────────────────┐
  └────┬──────────┬──────────────┘                   │
       │          │                                  │
  EnemyKilled  PlayerDamaged                         │
       │          │                                  │
       ▼          ▼                                  │
  score+=100  health -= damage                       │
  × waveNum   IsDead?                                │
       │          │                                  │
       │      ┌───┴───┐                              │
       │     No      Yes                             │
       │      │       │                              │
       │      │       ▼                              │
       │      │  ┌──────────────┐                    │
       │      │  │ GameOver     │                    │
       │      │  │ Paused=true  │                    │
       │      │  │ R:Restart    │──→ ReloadScene ────┤
       │      │  │ T:Title      │──→ Title Screen    │
       │      │  └──────────────┘                    │
       │      │                                      │
  CheckWaveComplete                                  │
       │                                             │
  ┌────┴────────┐                                    │
  │ all enemies │                                    │
  │ defeated?   │                                    │
  └────┬────────┘                                    │
       │ Yes                                         │
       └── StartNextWave (break timer) ──────────────┘

  ESC during Playing:
  ┌──────────────────────┐
  │  Paused              │
  │  ESC: Resume         │
  │  R: Restart          │
  │  T: Title            │
  └──────────────────────┘
```

---

## Animation system

| Entity | States | Tracks |
|--------|--------|--------|
| Player (idle) | Breathing bob | MeshInstance3D:position |
| Player (walk) | Bob + tilt | MeshInstance3D:position, rotation |
| Player (attack) | Weapon swing + lunge | WeaponPivot:rotation, MeshInstance3D:position |
| Player (dodge) | Squash & stretch + low | MeshInstance3D:scale, position |
| Enemy (walk) | Bob + sway | MeshInstance3D:position, rotation |
| Enemy (hit) | Squash impact | MeshInstance3D:scale |
| Enemy (death) | Shrink + sink | MeshInstance3D:scale, position |

All animations include RESET tracks and constant default tracks for unused properties to prevent AnimationTree zero-reset (Godot #80971).

---

## Audio system

```
  Master
  ├── SFX (AudioStreamPlayer3D, spatial, InverseSquareDistance)
  │   ├── Attack (220 Hz + harmonics)
  │   ├── Dodge (440 Hz + harmonics)
  │   ├── Hit (110 Hz + harmonics)
  │   └── Kill (660 Hz + harmonics)
  ├── BGM (AudioStreamPlayer, global)
  └── Ambient (AudioStreamPlayer, global)
```

---

## Particle effects

| Effect | Type | Trigger |
|--------|------|---------|
| HitEffect | GPUParticles3D (one-shot, 12 particles) | Player takes damage |
| DeathBurst | GPUParticles3D (one-shot, 24 particles) | Enemy dies |
| MovementDust | GPUParticles3D (continuous, 8 particles) | Player velocity > 1.0 |
| AmbientParticles | GPUParticles3D (continuous, 40 particles) | Always (arena environment) |

---

## Game design

| Element | Detail |
|---------|--------|
| Genre | 3D arena survival action |
| Player | Melee combo (3-hit), ranged projectile, dodge roll |
| Enemies | Grunt (balanced), Brute (tanky), Runner (fast) |
| Waves | Progressive (3 + wave×2 enemies, difficulty +0.15/wave) |
| Combat | Stamina-based dodge, 3-hit combo with 2× finisher |
| Score | 100 × waveNumber per kill |
| Lose | Health reaches 0 |
| Controls | WASD move, Mouse aim, LMB attack, RMB shoot, Space dodge, ESC pause |

---

## Testing

Pure function tests via xUnit. No Godot runtime required.

```bash
cd Tests && dotnet test
```

```
Passed!  - Failed: 0, Passed: 48, Total: 48, Duration: 10ms
```

| Test class | Tests | Coverage |
|------------|-------|----------|
| CombatCalculatorTest | 21 | CreateInitial, TakeDamage, TryDodge, TryAttack, EndDodge, EndAttack, RegenStamina, UpdateComboTimer |
| WaveCalculatorTest | 27 | CreateInitial, StartNextWave, EnemySpawned, EnemyDefeated, PlayerDefeated, GetWaveDifficulty |

Humble Object pattern: CombatCalculator and WaveCalculator have zero Godot dependencies.

---

## Project structure

```
03_action_in_godot/
├── project.godot
├── 03_action_in_godot.csproj
├── default_bus_layout.tres          AudioBus layout (Master/SFX/BGM/Ambient)
├── addons/ReactiveSO/Runtime/
│   ├── Channels/                    EventChannel types (7)
│   ├── Variables/                   Variable types (6)
│   └── RuntimeSets/                RuntimeSet types (2)
├── Scripts/
│   ├── Configs/
│   │   ├── PlayerConfig.cs          Player stats Resource
│   │   ├── EnemyConfig.cs           Enemy stats Resource
│   │   ├── WeaponConfig.cs          Melee weapon Resource
│   │   ├── ProjectileConfig.cs      Ranged weapon Resource
│   │   └── WaveConfig.cs            Wave composition Resource
│   ├── Core/
│   │   ├── GameManager.cs           Orchestrator (Humble Object shell)
│   │   ├── CombatCalculator.cs      Pure combat logic (no Godot dependency)
│   │   ├── WaveCalculator.cs        Pure wave logic (no Godot dependency)
│   │   └── MainSetup.cs             Runtime wiring
│   ├── Player/
│   │   ├── PlayerController.cs      CharacterBody3D movement + state
│   │   ├── PlayerCombat.cs          Melee/ranged attack (Area3D detection)
│   │   └── PlayerAnimator.cs        AnimationPlayer + AnimationTree (StateMachine)
│   ├── Enemies/
│   │   ├── Enemy.cs                 Navigation + combat + Area3D hitbox
│   │   ├── EnemySpawner.cs          Wave-based spawning
│   │   └── EnemyAnimator.cs         AnimationPlayer + AnimationTree (StateMachine)
│   ├── Combat/
│   │   └── Projectile.cs            Area3D projectile with collision detection
│   ├── Effects/
│   │   ├── ScreenShake.cs           Camera shake on damage/kill
│   │   ├── HitEffect.cs             GPUParticles3D (damage burst)
│   │   ├── DeathBurst.cs            GPUParticles3D (enemy death burst)
│   │   ├── MovementDust.cs          GPUParticles3D (player footsteps)
│   │   ├── AmbientParticles.cs      GPUParticles3D (arena environment)
│   │   └── AudioManager.cs          Spatial audio + bus routing
│   └── UI/
│       ├── HUD.cs                   Health, stamina, score, wave, game over
│       ├── PauseMenu.cs             Pause/resume, restart, title transition
│       └── TitleScreen.cs           Title screen with scene transition
├── Scenes/
│   ├── Title.tscn                   Title screen scene
│   ├── Main.tscn                    Game scene (all resources wired)
│   ├── Enemy.tscn                   Enemy prefab
│   └── Projectile.tscn             Projectile prefab (Area3D)
├── Resources/
│   ├── Player/                      PlayerConfig (1)
│   ├── Enemies/                     EnemyConfig instances (3)
│   ├── Weapons/                     WeaponConfig + ProjectileConfig (2)
│   ├── Waves/                       WaveConfig (1)
│   └── Events/                      EventChannels (8), Variables (4), RuntimeSet (1)
└── Tests/
    ├── Tests.csproj                 xUnit test project (net10.0)
    ├── CombatCalculatorTest.cs      21 tests
    └── WaveCalculatorTest.cs        27 tests
```

---

## Environment

- Godot 4.6.2 (.NET build, install via `brew install --cask godot-mono`)
- .NET SDK 10+
