# 05_multiplayer_godot — Swarm Survivor Multiplayer

Phase 6: GDShader, MultiMesh, CompositorEffect

A top-down auto-battler survival game where massive enemy swarms (50-500 per wave) are rendered via MultiMesh, with custom shaders for per-instance visual effects and compositor-driven screen feedback.

---

## Architecture

### Fundamental Design Shift

Phases 1-5: **1 entity = 1 Node3D**
Phase 6: **All entities = data arrays + MultiMesh rendering**

Enemies, projectiles, and XP gems have **zero individual scene nodes**. State lives in flat arrays (Structure-of-Arrays layout), and each frame the arrays are uploaded to MultiMeshInstance3D for GPU-efficient rendering.

### Dependency Flow

```text
UI / CanvasLayer
    ↓ (observe)
ReactiveSO Resources (EventChannel, Variable)
    ↓ (modified by)
GameManager (Humble Object Shell)
    ↓ (calls)
Pure Calculators (SwarmCalculator, WaveCalculator, UpgradeCalculator)
    ↓ (return)
Immutable State / Result Records
```

### Data-Driven Entity Management

```text
SwarmManager                    GemManager
├── float[] posX, posZ          ├── float[] posX, posZ
├── float[] velX, velZ          ├── int[]   xpValue
├── int[]   health              └── float[] lifetime
├── int[]   enemyTypeIndex
├── float[] damageFlashTimer    ProjectileManager
├── float[] deathTimer          ├── float[] posX, posZ
└── int     activeCount         ├── float[] dirX, dirZ
       ↓                        ├── float[] speed
  SwarmRenderer                 └── int     count
  (MultiMeshInstance3D)              ↓
       ↓                        MultiMeshInstance3D
  swarm_enemy.gdshader               ↓
  (per-instance effects)        projectile.gdshader
```

---

## Game Design

### Core Loop

```text
Move (WASD) → Weapons auto-fire → Kill enemies → XP gems drop
→ Collect (magnetic) → Level up → Choose upgrade → Repeat
```

### Wave Progression

| Wave | Enemy Count | Notes |
|------|-------------|-------|
| 1 | 50 | Base |
| 2 | 100 | Speed up |
| 3 | 200 | Mixed types |
| 4 | 350 | High density |
| 5 | 500 | Final wave |

### Enemy Types

| Type | HP | Speed | XP | Color |
|------|-----|-------|-----|-------|
| Swarmling | Low | Normal | 1 | Red |
| Charger | Medium | Fast | 2 | Yellow |
| Tank | High | Slow | 5 | Green |

### Weapon Types

| Type | Behavior |
|------|----------|
| Orbital | Rotating orbs around player, AoE damage |
| Projectile | Fires toward nearest enemy |
| Aura | Continuous AoE damage around player |

---

## Shader Architecture

### GDShader Files (5)

| File | Type | Key Techniques |
|------|------|----------------|
| `swarm_enemy.gdshader` | Vertex + Fragment | INSTANCE_CUSTOM per-instance data, noise dissolve, rim lighting |
| `xp_gem.gdshader` | Vertex + Fragment | TIME-based animation, fresnel glow, scale pulse |
| `projectile.gdshader` | Vertex + Fragment | Velocity-stretch, additive glow |
| `arena_ground.gdshader` | Fragment | Grid pattern, FBM noise, danger zone pulse |
| `player_aura.gdshader` | Fragment | Radial noise gradient, alpha transparency |

### Shader Includes (2)

| File | Provided Functions |
|------|-------------------|
| `noise.gdshaderinc` | hash21, hash22, value_noise, fbm |
| `color_utils.gdshaderinc` | fresnel_rim, apply_damage_flash, pulse |

### Per-Instance Shader Data (INSTANCE_CUSTOM)

```text
Enemy MultiMesh:
  R = damageFlash (0-1, normalized timer)
  G = deathProgress (0-1, dissolve amount)
  B = typeIndex (enemy type for color)
  A = unused

Projectile MultiMesh:
  R = directionX (normalized)
  G = directionZ (normalized)
  B, A = unused
```

### Screen Effects (CompositorEffect pattern via CanvasLayer + ShaderMaterial)

| Effect | Trigger | Visual |
|--------|---------|--------|
| DamageVignetteEffect | on_player_damaged | Red edge vignette, fades quickly |
| LevelUpFlashEffect | on_level_up | Golden center glow, fades |
| LowHealthPulseEffect | health Variable | Pulsing red vignette at low HP |

---

## ReactiveSO Resources

### EventChannels (8)

| Resource | Type | Publisher → Subscriber |
|----------|------|----------------------|
| on_player_damaged | IntEventChannel | Player → GameManager, DamageVignette, Audio |
| on_player_died | VoidEventChannel | Player → GameManager |
| on_enemy_killed | VoidEventChannel | SwarmManager → GameManager, Spawner, Audio |
| on_wave_completed | VoidEventChannel | SwarmSpawner → GameManager |
| on_game_over | VoidEventChannel | GameManager → PauseMenu |
| on_level_up | IntEventChannel | XPCollector → UpgradePanel, LevelUpFlash, Audio |
| on_upgrade_selected | IntEventChannel | UpgradePanel → GameManager |
| on_xp_collected | IntEventChannel | GemManager → XPCollector, Audio |

### Variables (6)

| Resource | Type | Writer | Reader |
|----------|------|--------|--------|
| health | IntVariable | PlayerController | HUD, LowHealthPulse |
| score | IntVariable | GameManager | HUD |
| wave_number | IntVariable | SwarmSpawner | HUD |
| level | IntVariable | XPCollector | HUD |
| xp | IntVariable | XPCollector | HUD |
| kill_count | IntVariable | GameManager | HUD |

### RuntimeSets

None. Enemies are data arrays, not nodes. This is an intentional architectural shift from Phase 5.

---

## Pure Calculators (Humble Object Pattern)

### SwarmCalculator

Zero Godot dependencies. Handles enemy movement, separation, damage, death progression.

Key methods: `MoveToward`, `CalculateSeparation`, `TakeDamage`, `TickDeath`, `ClampToArena`

### WaveCalculator

Wave state transitions. Tracks wave progression, enemy counts, victory/defeat conditions.

Key methods: `StartNextWave`, `EnemyDefeated`, `GetEnemyCountForWave`, `GetSpawnInterval`

### UpgradeCalculator

XP accumulation, level-up thresholds, random upgrade selection.

Key methods: `GainXP`, `CalculateXPForLevel`, `GetRandomChoices`

---

## Test Coverage

71 tests (all passing via `dotnet test`):

| Test Class | Count | Coverage |
|------------|-------|----------|
| SwarmCalculatorTest | 28 | Movement, separation, damage, death, arena bounds |
| WaveCalculatorTest | 22 | Wave progression, victory, defeat, spawn intervals |
| UpgradeCalculatorTest | 21 | XP gain, level-up, overflow, random choices |

---

## Project Structure

```text
05_multiplayer_godot/
├── Scripts/
│   ├── Configs/         PlayerConfig, EnemyTypeConfig, WaveConfig, WeaponConfig, UpgradeConfig
│   ├── Core/            GameManager, SwarmCalculator, WaveCalculator, UpgradeCalculator, MainSetup
│   ├── Player/          PlayerController, XPCollector
│   ├── Swarm/           SwarmManager, SwarmRenderer, SwarmSpawner
│   ├── Pickups/         GemManager
│   ├── Weapons/         WeaponSystem, ProjectileManager, AuraWeapon
│   ├── Rendering/       DamageVignetteEffect, LevelUpFlashEffect, LowHealthPulseEffect
│   ├── Effects/         ScreenShake, AudioManager
│   └── UI/              HUD, UpgradePanel, PauseMenu, TitleScreen
├── Shaders/
│   ├── includes/        noise.gdshaderinc, color_utils.gdshaderinc
│   ├── swarm_enemy.gdshader
│   ├── xp_gem.gdshader
│   ├── projectile.gdshader
│   ├── arena_ground.gdshader
│   └── player_aura.gdshader
├── Scenes/              Title.tscn, Main.tscn
├── Resources/           .tres files (configs, events, variables)
├── Tests/               SwarmCalculatorTest, WaveCalculatorTest, UpgradeCalculatorTest
└── addons/ReactiveSO/   EventChannel, Variable, RuntimeSet library
```

---

## Key Patterns Demonstrated

| Pattern | Where | Unity Equivalent |
|---------|-------|-----------------|
| MultiMeshInstance3D | SwarmRenderer, GemManager, ProjectileManager | Graphics.DrawMeshInstanced |
| INSTANCE_CUSTOM | swarm_enemy.gdshader | MaterialPropertyBlock per-instance |
| GDShader includes | noise.gdshaderinc, color_utils.gdshaderinc | .cginc / .hlsl includes |
| Shader uniforms | All .gdshader files | Material properties |
| CanvasItem shader (screen effect) | DamageVignette, LevelUpFlash, LowHealthPulse | OnRenderImage / URP Volume |
| Structure-of-Arrays | SwarmManager data layout | ECS / DOTS-style entity storage |
| Data-driven rendering | No entity nodes, pure array → MultiMesh | Indirect rendering pattern |
