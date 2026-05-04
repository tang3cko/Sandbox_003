# Sandbox_003

## Purpose

A sandbox project for learning Godot 4.6 with C#.

---

## Projects

| Directory | Phase | Description |
|-----------|-------|-------------|
| `01_godot_basics/` | 1 | 2D collect game. Scene Tree, Node types, Signal, Resource, EventChannel pattern |
| `02_td_in_godot/` | 2-3 | 3D Tower Defense. Signal + Resource architecture, Humble Object, TDD |
| `03_action_in_godot/` | 4 | 3D action game. Physics, Navigation, Animation, Audio, Particles, Scene management |
| `04_shaders_in_godot/` | 5 | GDShader, MultiMesh, CompositorEffect |
| `05_multiplayer_godot/` | 6 | ENet, RPC, Server Authority (optional: Steam) |
| `06_escape_room_godot/` | 7 | Escape room / walking simulator. Interaction system, Inventory, Puzzle, Trigger + Signal architecture |
| `07_persistence_in_godot/` | 8 | 2D persistence sandbox. Encrypted save data, ConfigFile settings, version migration, all-in-one UI |
| `08_voxel_terrain_godot/` | 9 | Destructible voxel terrain. Blocky face-culling mesher with strategy pattern for future Marching Cubes |

---

## Phases

| Phase | Focus |
|-------|-------|
| **1** | Paradigm shift -- Scene Tree, Node inheritance, Signal, Resource |
| **2** | Signal + Resource patterns -- EventChannel, Variable, RuntimeSet equivalents |
| **3** | Test-driven development -- Humble Object, pure function separation |
| **4** | Core Godot systems -- Physics, Navigation, Animation, Audio, Particles, Scene management |
| **5** | 3D / Rendering -- GDShader, MultiMesh, CompositorEffect |
| **6** | Multiplayer -- ENet, Server Authority, Steam |
| **7** | Escape room -- Interaction system, Trigger + Signal, Inventory, Puzzle, UI |
| **8** | Persistence -- FileAccess encryption (AES-256-CFB), ConfigFile, save version migration, atomic write |
| **9** | Destructible voxel terrain -- ChunkData, face-culling mesher, IVoxelMesher strategy, raycast digging |

---

## Unity to Godot paradigm mapping

| Unity | Godot | Notes |
|-------|-------|-------|
| GameObject + Component | Node | One node, one role. Composition over inheritance |
| Prefab | PackedScene | Scene = reusable node tree |
| ScriptableObject | Resource | `.tres` files. Both dynamic and static references |
| Event/Delegate | Signal | Type-safe (Godot 4.6+) |
| EventChannelSO | Resource + Signal | Resource-based EventChannel pattern |
| MonoBehaviour | Node | `_ready()`, `_process()` lifecycle |
| Tag/Layer | Group / Collision Layer | Group is string-based |
| SceneManager | SceneTree | Tree structure IS scene management |
| [SerializeField] | @export | Expose to inspector |
| Invoke/Coroutine | Tween / await | GDScript coroutines are async/await style |
| Rigidbody | RigidBody3D | Godot separates CharacterBody3D and RigidBody3D |
| NavMeshAgent | NavigationAgent3D | Baked NavigationMesh + agent system |
| Animator | AnimationTree | AnimationPlayer + AnimationTree with state machine |
| AudioSource | AudioStreamPlayer3D | Spatial audio with bus routing |
| ParticleSystem | GPUParticles3D | Shader-based particle system |

---

## Project structure

```text
Sandbox_003/
├── 01_godot_basics/        # Phase 1
│   └── project.godot
├── 02_td_in_godot/         # Phase 2-3
│   └── project.godot
├── 03_action_in_godot/     # Phase 4
│   └── project.godot
├── 04_shaders_in_godot/    # Phase 5
│   └── project.godot
├── 05_multiplayer_godot/   # Phase 6
│   └── project.godot
├── 06_escape_room_godot/   # Phase 7
│   └── project.godot
├── 07_persistence_in_godot/ # Phase 8
│   └── project.godot
└── 08_voxel_terrain_godot/  # Phase 9
    └── project.godot
```

---

## Environment

- Godot 4.6 (.NET build, install via `brew install --cask godot-mono`)
- .NET SDK 10+
