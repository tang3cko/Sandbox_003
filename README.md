# Sandbox_003

## Purpose

A sandbox project for learning Godot 4.6 with C#.

---

## Projects

| Directory | Phase | Description |
|-----------|-------|-------------|
| `01_godot_basics/` | 1 | 2D collect game. Scene Tree, Node types, Signal, Resource, EventChannel pattern |
| `02_td_in_godot/` | 2-4 | TDD, Humble Object, Signal + Resource architecture |
| `03_shaders_in_godot/` | 5 | GDShader, MultiMesh, CompositorEffect |
| `04_multiplayer_godot/` | 6 | ENet, RPC, Steam integration |

---

## Phases

| Phase | Focus |
|-------|-------|
| **1** | Paradigm shift -- Scene Tree, Node inheritance, Signal, Resource |
| **2** | Signal + Resource patterns -- EventChannel, Variable, RuntimeSet equivalents |
| **3** | GDScript type system and C# interop |
| **4** | Test-driven development -- Humble Object, GUT, xUnit |
| **5** | 3D / Rendering -- GDShader, MultiMesh, CompositorEffect |
| **6** | Multiplayer -- ENet, Server Authority, Steam |
| **7** | Production project |

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

---

## Project structure

```text
Sandbox_003/
├── 01_godot_basics/        # Phase 1
│   └── project.godot
├── 02_td_in_godot/         # Phase 2-4
│   └── project.godot
├── 03_shaders_in_godot/    # Phase 5
│   └── project.godot
└── 04_multiplayer_godot/   # Phase 6
    └── project.godot
```

---

## Environment

- Godot 4.6 (.NET build, install via `brew install --cask godot-mono`)
- .NET SDK 10+
