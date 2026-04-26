# 06_escape_room_godot

## Purpose

Phase 7 learning project. A first-person walking simulator (escape room) built with Godot 4.6.2 + C# (.NET 10).

The escape-room mechanic (pick up the brass key, hold to open the locked door) is a **testbed**. The deliverable is a reusable interaction pipeline:

- Structured `InteractionContext` (no string templates baked into UI)
- `IInteractable` / `IInteractor` interfaces (no `Area3D` / `Node` lock-in)
- Composable `InteractionValidator` Resources (range, required-item, …)
- Focus-enter / focus-exit signals for highlight extension
- Hold-to-interact with progress reporting
- Runtime input-glyph resolution from `InputMap` (no hardcoded `[E]`)
- Decoupled HUD ↔ Player via `InteractionContextEventChannel` (Resource + Signal — same shape as Unity ScriptableObject EventChannels)

---

## Run

1. Open `project.godot` in Godot 4.6 (.NET).
2. Build the C# solution (Editor `Build` button or `dotnet build`).
3. Press F5. `Scenes/Main.tscn` loads Level + Player + HUD.

| Input | Action |
|-------|--------|
| `WASD` | Move |
| `Mouse` | Look |
| `E` | Interact (hold ~0.5 s for the door) |
| `Esc` | Toggle mouse capture |

---

## Architecture

```
                   InteractionContextEventChannel (.tres)
                   on_interaction_context_changed
                            ▲                 ▲
                Raise(ctx)  │    Raised +=    │
                            │                 │
   ┌───────────────────┐    │    ┌────────────┴─────────┐
   │  PlayerInteractor │────┘    │  InteractionPromptUI │
   │  (Node, IInteractor)        │  (Control)           │
   │                   │         │  Label + ProgressBar │
   │  ─ raycast        │         └──────────────────────┘
   │  ─ focus tracking │
   │  ─ hold state     │
   │  ─ glyph resolve  │
   └────────┬──────────┘
            │ ResolveTarget / SwapFocus / Interact
            ▼
   ┌──────────────────────────────────────────────────────────┐
   │  IInteractable (interface)                               │
   │     ├─ InteractableComponent  (abstract Area3D base)     │
   │     │     ├─ Validators: InteractionValidator[]          │
   │     │     ├─ Verb / TargetName / Priority                │
   │     │     ├─ RequiresHold / HoldDuration                 │
   │     │     ├─ FocusEntered / FocusExited / Interacted     │
   │     │     └─ template OnInteract → InteractionResult     │
   │     │                                                    │
   │     ├─ PickupInteractable    (Item → Inventory + free)   │
   │     └─ DoorInteractable      (Tween pivot, drop layer)   │
   └──────────────────────────────────────────────────────────┘
                                  │
                                  │ uses
                                  ▼
   ┌──────────────────────────────────────────────────────────┐
   │  InteractionValidator (abstract Resource)                │
   │     └─ RequiredItemValidator → InventorySystem.HasItem   │
   └──────────────────────────────────────────────────────────┘
                                  │
                                  ▼
                         InventorySystem (autoload Node)
                            ItemAdded / ItemRemoved signals
                                  ▲
                                  │ Raised +=
                          InventoryUI (Control)
```

---

## Scene Tree

```
Main (Node)
├── Level   ← Levels/TestRoom.tscn
│   ├── DirectionalLight3D + OmniLight3D (fill)
│   ├── Floor / Walls (StaticBody3D, Environment layer)
│   ├── Table (MeshInstance3D, decoration)
│   ├── NotePickup    ← Interactables/Pickup.tscn  (Item = strange_note.tres)
│   ├── KeyPickup     ← Interactables/Pickup.tscn  (Item = key_brass.tres)
│   └── MainDoor      ← Interactables/Door.tscn    (Validators = [key_brass_required.tres])
│
├── Player  ← Player/Player.tscn (CharacterBody3D)
│   ├── Collision (CollisionShape3D, capsule)
│   ├── Head (Node3D)
│   │   ├── Camera3D
│   │   └── InteractionRay (RayCast3D, mask=Interactable, areas only)
│   └── Interactor (Node) ── PlayerInteractor.cs
│
└── HUD     ← UI/HUD.tscn (CanvasLayer)
    ├── Crosshair (Label "+")
    ├── InteractionPrompt (Control) ── InteractionPromptUI.cs
    │   ├── Label
    │   └── HoldBar (ProgressBar)
    └── Inventory (Control) ── InventoryUI.cs
        └── Panel/Margin/HBox
```

---

## Interaction pipeline

### Frame flow

```
PlayerInteractor._PhysicsProcess(δ)
  └─ _ray.ForceRaycastUpdate()
  └─ ResolveTarget()
        ├─ collider as IInteractable
        ├─ filter: GodotObject.IsInstanceValid(collider)
        ├─ filter: !collider.IsQueuedForDeletion()
        └─ filter: target.CanBeInteractedBy(this)
                    └─ Enabled && every Validators[i].Validate(...)
  └─ SwapFocus(next)
        ├─ if (prev != next):
        │       prev?.OnFocusExit(this)
        │       next?.OnFocusEnter(this)
        └─ EmitContext()
              └─ ctx = (next != null) ? next.BuildContext(this, glyph, holdProgress) : empty
                    ├─ blocked path:  ctx.BlockReason = failingValidator.GetBlockReason()
                    └─ allowed path:  ctx.Verb / ctx.TargetName
              └─ if (ctx differs from last): _onContextChanged.Raise(ctx)
                                                ↓
                                  InteractionPromptUI.OnContext(ctx)
                                    Label.Text  = "[E] Open Door"  (or BlockReason in red)
                                    HoldBar.Visible = ctx.RequiresHold && 0 < HoldProgress < 1

PlayerInteractor._UnhandledInput(InteractAction pressed)
  ├─ target.RequiresHold == false:
  │     target.Interact(this) → re-validate → OnInteract template hook
  └─ target.RequiresHold == true:
        _isHolding = true; _holdProgress = 0; EmitContext (hold mode begins)
```

### Hold-to-interact

`MainDoor` is configured with `RequiresHold = true, HoldDuration = 0.5`. Pickup items use the default press-once behaviour.

```
Press E with door focused
   └─ _isHolding = true; _holdProgress = 0
       Each physics frame while holding:
         _holdProgress += δ / HoldDuration
         EmitContext (HoldProgress baked into ctx → ProgressBar grows)
       When _holdProgress >= 1:
         target.Interact(this) → DoorInteractable.OnInteract → Tween rotation + drop layers
         Reset hold state; force raycast update; re-emit context

Release E mid-hold:
   _isHolding = false; _holdProgress = 0; EmitContext (bar disappears)
```

### Input glyph resolution

`[E]` is **not** baked into UI. `PlayerInteractor._Ready()` calls `InputGlyphResolver.Resolve(InteractAction)` once and stamps the result into every emitted `InteractionContext`.

| Mapped event | Glyph |
|--------------|-------|
| `InputEventKey` | `OS.GetKeycodeString(physical_or_keycode).ToUpper()` |
| `InputEventMouseButton` | `LMB` / `RMB` / `MMB` / `M<n>` |
| `InputEventJoypadButton` | `PAD<index>` |
| (no event mapped) | uppercased action name |

---

## Interaction system contract

The reusable layer lives in `Scripts/Interaction/Core/` and `Scripts/Interaction/Validators/`.

### Interfaces

| Type | Role |
|------|------|
| `IInteractable` | Knows priority, validates, builds context, fires focus signals, performs `Interact`. |
| `IInteractor` | Exposes spatial `Body : Node3D` and `GlobalPosition` for distance / LoS validators. |
| `InteractionResult` (enum) | `Success` / `Rejected` / `InProgress` |

### Data Resources

| Resource | Role |
|----------|------|
| `InteractionContext` | Structured prompt: `Verb`, `TargetName`, `Glyph`, `BlockReason`, `TextColor`, `BlockedColor`, `RequiresHold`, `HoldProgress`, `Icon`. UI renders from this; system populates this. |
| `InteractionContextEventChannel` | Resource carrying `Raised(InteractionContext)`. Player and HUD reference the **same** `.tres` for full decoupling — neither side knows the other's tree path. |
| `InteractionValidator` (abstract) | `Validate(IInteractor, IInteractable) → bool` and `GetBlockReason() → string`. Composable via `[Export] InteractionValidator[] Validators` on `InteractableComponent`. |

### Provided concrete classes

| Class | Role |
|-------|------|
| `InteractableComponent : Area3D, IInteractable` | Base for raycast-detected interactables. Holds Verb / TargetName / Validators / RequiresHold / HoldDuration / InteractionPriority. Template method `OnInteract` for subclasses. |
| `PickupInteractable` | Adds `[Export] InventoryItem Item`. Auto-derives Verb=`"Take"` + TargetName from `Item.DisplayName`. Adds to `InventorySystem` + `QueueFree`. |
| `DoorInteractable` | Door-swing via `Tween` on a `Pivot` `Node3D`. Optional `ConsumeKey + ConsumedItemId`. Drops the body's Environment layer (player walks through) and removes self from Interactable layer (raycast stops hitting). |
| `RequiredItemValidator` | `InventorySystem.HasItem(ItemId)`; BlockReason = `"Requires <DisplayName>"`. |
| `PlayerInteractor : Node, IInteractor` | Owns raycast + focus tracking + hold state machine + glyph resolution + context emission. |
| `InteractionPromptUI : Control` | Renders `InteractionContext`: Label (glyph + verb + name, or BlockReason in red) + ProgressBar (during hold). |

---

## Inventory subsystem

`InventorySystem` is a Godot autoload (`project.godot[autoload]`). It exposes a static `Instance` for C# convenience.

| Member | Role |
|--------|------|
| `AddItem(InventoryItem)` | Add; emits `ItemAdded(item)` |
| `RemoveItem(InventoryItem)` | Remove by reference |
| `RemoveItemById(string)` | Remove first match by `ItemId` (used by `DoorInteractable.ConsumeKey`) |
| `HasItem(string itemId)` | Read for validators |
| `Items` (`IReadOnlyList<InventoryItem>`) | Snapshot for UI rebuild |
| `ItemAdded` / `ItemRemoved` (signals) | Subscribed by `InventoryUI` |

`InventoryItem` is a `[GlobalClass] Resource` with `ItemId`, `DisplayName`, `Description` (multiline, used as tooltip), `Icon`.

---

## Collision layers

```
                Detection target (collision_mask)
                  bit0      bit1     bit2
                Player  Interact  Environ
   ┌─────────┬────────┬─────────┬────────┐
   │ Player  │        │         │   ●    │
   │ bit0    │        │         │ slides │
   ├─────────┼────────┼─────────┼────────┤
   │ Interact│        │         │        │
   │ bit1    │        │         │        │
   ├─────────┼────────┼─────────┼────────┤
   │ Environ │        │         │        │
   │ bit2    │        │         │        │
   └─────────┴────────┴─────────┴────────┘

   Player CharacterBody3D : layer=Player, mask=Environment
   InteractionRay         : mask=Interactable, areas only
   InteractableComponent  : layer=Interactable
   Walls / Floor / Door   : layer=Environment
```

---

## Extending

### Add a new interactable

```csharp
[GlobalClass]
public partial class LeverInteractable : InteractableComponent
{
    [Export] public Node3D LinkedDoor { get; set; }

    public override void _Ready()
    {
        Verb = "Pull";
        TargetName = "Lever";
    }

    protected override InteractionResult OnInteract(IInteractor interactor)
    {
        // toggle linked door, etc.
        return InteractionResult.Success;
    }
}
```

Place an `Area3D` in your scene with the script attached. Set `Validators[]` in the inspector if needed.

### Add a new validator

```csharp
[GlobalClass]
public partial class TimeOfDayValidator : InteractionValidator
{
    [Export] public float MinHour { get; set; } = 18f;
    public override bool Validate(IInteractor _, IInteractable __) =>
        WorldClock.Instance.Hour >= MinHour;
    public override string GetBlockReason() => "Comes back at night";
}
```

Save as `Resources/Validators/.../my_validator.tres`. Drop into the `Validators` array on any `InteractableComponent`.

### Add an area effect (no interaction)

`TriggerVolume : Area3D` (in `Scripts/Interaction/TriggerVolume.cs`) is provided for music / fog / cutscene triggers. Drop it into a level, connect `PlayerEntered` / `PlayerExited` signals from the editor. Filters by `PlayerGroup` (default `"player"` — added by `FirstPersonController._Ready` to the player CharacterBody3D).

---

## Unity to Godot mapping

| Concept | Godot (this project) | Unity equivalent |
|---------|---------------------|------------------|
| Reusable data asset | `Resource` (`.tres`) | `ScriptableObject` |
| Decoupled events | `[Signal]` on a `Resource` | `UnityEvent` on a `ScriptableObject` (Hipple-style EventChannel) |
| Inspector-exposed field | `[Export]` | `[SerializeField]` |
| Trigger volume | `Area3D` + `monitorable=true` + child `CollisionShape3D` | `Collider` with `isTrigger=true` |
| Ray query | `RayCast3D` (in-scene, `ForceRaycastUpdate()`) | `Physics.Raycast(...)` (per-frame call) |
| Static spatial body | `StaticBody3D` + `CollisionShape3D` | static `GameObject` + `Collider` |
| Lifecycle hook | `_Ready` / `_PhysicsProcess` | `Start` / `FixedUpdate` |
| Composable validator | `Resource` array on the Interactable | `ScriptableObject` array on the Interactable `MonoBehaviour` |
| Tween | `CreateTween().TweenProperty(...)` | `DOTween` / coroutine / `Animator` |
| HUD layer | `CanvasLayer` | Screen-space-overlay `Canvas` |
| Singleton autoload | `[autoload]` script in `project.godot` | `RuntimeInitializeOnLoadMethod` singleton |
| Object validity check | `GodotObject.IsInstanceValid` + `Node.IsQueuedForDeletion` | Unity's `obj == null` (overloaded) |
| Action input | `InputMap.ActionGetEvents` + `InputGlyphResolver` | `InputAction` + `InputBinding.ToDisplayString` |

The interfaces in `Scripts/Interaction/Core/` (`IInteractable`, `IInteractor`, `InteractionContext`) are pure C# types and translate without changes to a Unity port; only the Godot-typed base classes (`InteractableComponent : Area3D`, `PlayerInteractor : Node`) need swapping for `MonoBehaviour` equivalents.

---

## Game design (escape-room demo)

| Element | Detail |
|---------|--------|
| Genre | First-person walking sim / escape-room sandbox |
| Goal | Pick up the brass key, hold `E` to open the locked south door |
| Pickups | `Brass Key` (puzzle item), `Strange Note` (flavor; description is the hint shown in inventory tooltip) |
| Door | Locked behind `RequiredItemValidator(key_brass)`. Hold `E` for 0.5 s, then swings 90° via `Tween` over 0.6 s, `Environment` layer dropped so the player walks through |
| Win | No formal win condition — door open is the demo terminus |

---

## Project structure

```
06_escape_room_godot/
├── project.godot                                  ; layers, inputs, autoloads
├── 06_escape_room_godot.csproj
├── default_bus_layout.tres
├── addons/ReactiveSO/                             ; shared event-channel / variable / runtime-set library
├── Resources/
│   ├── Events/
│   │   └── on_interaction_context_changed.tres   ; InteractionContextEventChannel
│   ├── Items/
│   │   ├── key_brass.tres                         ; InventoryItem
│   │   └── strange_note.tres                      ; InventoryItem
│   └── Validators/
│       └── key_brass_required.tres                ; RequiredItemValidator
├── Scenes/
│   ├── Main.tscn                                  ; Level + Player + HUD
│   ├── Levels/
│   │   └── TestRoom.tscn
│   ├── Player/
│   │   └── Player.tscn                            ; CharacterBody3D + Head/Camera/RayCast + Interactor
│   ├── UI/
│   │   └── HUD.tscn                               ; Crosshair + InteractionPrompt + Inventory
│   └── Interactables/
│       ├── Pickup.tscn                            ; reusable Area3D pickup
│       └── Door.tscn                              ; reusable hinge door
└── Scripts/
    ├── Interaction/
    │   ├── Core/
    │   │   ├── IInteractable.cs
    │   │   ├── IInteractor.cs
    │   │   ├── InteractionResult.cs
    │   │   ├── InteractionContext.cs              ; [GlobalClass] Resource
    │   │   └── InteractionContextEventChannel.cs  ; [GlobalClass] Resource
    │   ├── Validators/
    │   │   ├── InteractionValidator.cs            ; abstract Resource
    │   │   └── RequiredItemValidator.cs
    │   ├── InteractableComponent.cs               ; Area3D, IInteractable
    │   ├── PickupInteractable.cs
    │   ├── DoorInteractable.cs
    │   └── TriggerVolume.cs                       ; available; not placed in TestRoom
    ├── Inventory/
    │   ├── InventoryItem.cs                       ; [GlobalClass] Resource
    │   └── InventorySystem.cs                     ; Node, autoload
    ├── Player/
    │   ├── FirstPersonController.cs               ; movement + look only
    │   └── PlayerInteractor.cs                    ; interaction; separated from movement
    ├── UI/
    │   ├── InteractionPromptUI.cs
    │   └── InventoryUI.cs
    └── Util/
        └── InputGlyphResolver.cs                  ; static helper
```

---

## Godot 4.6 .NET gotchas (recorded from this build)

These bit during development. Each is a pattern, not a one-off.

1. **`[Export] NodePath` defaults to C# `null`, not an empty `NodePath`.**
   Calling `.IsEmpty` on `null` throws `NullReferenceException`. Always initialise: `[Export] private NodePath _path = new();` and check `path != null && !path.IsEmpty` before use.

2. **`[Export]` typed `Node` references do NOT auto-resolve from `NodePath(...)` in scenes.**
   The C# source generator emits `ConvertTo<T>(value)` in `SetGodotClassPropertyValue`; a `NodePath` `Variant` cannot convert to a `Node` reference, so the field stays `null` even if the scene says `_field = NodePath("…")`. Workaround in this project: keep them as `[Export] NodePath _xxxPath = new()` and resolve manually in `_Ready` with `GetNodeOrNull<T>(path)`. Resource exports (`InteractionContextEventChannel`, `InventoryItem`, …) work fine — only Node-typed exports are affected.

3. **`CollisionShape3D` binds to the NEAREST `CollisionObject3D` ancestor.**
   Nesting `Area3D` inside `StaticBody3D` was observed to bind the inner `Area3D`'s shape to the outer body, leaving the `Area3D` shapeless and undetectable by raycasts. `Scenes/Interactables/Door.tscn` deliberately keeps `Interactable` (Area3D) and `DoorBody` (StaticBody3D) as **siblings** under `Pivot`, never nested.

4. **C# `[Export]` identifier appears in `.tscn` exactly as written.**
   - `[Export] public string PromptText { get; set; }` → `PromptText = "..."`
   - `[Export] private NodePath _headPath = new()` → `_headPath = NodePath("Head")`
   - There is no automatic snake_casing. A mismatch silently fails: the scene override never applies and the property keeps its C# default.

5. **C# arrays of `Resource` exports.**
   `[Export] InteractionValidator[] Validators` serialises as `Validators = Array[Resource]([ExtResource("..."), …])` — the same form as Godot's typed array.

6. **`.tres` resources are cached by load path.**
   Multiple references to `on_interaction_context_changed.tres` resolve to the same instance — that is exactly why Player and HUD can use it for decoupled communication. Set `resource_local_to_scene = true` in the resource header to opt out.

7. **Autoload from a bare `.cs` script.**
   `InventorySystem="*res://Scripts/Inventory/InventorySystem.cs"` instantiates a `Node` and attaches the script. The static `Instance` pattern works because the autoload's `_EnterTree` runs before any scene's `_Ready`.

8. **`QueueFree()` is end-of-frame.**
   The node remains in the tree and the C# wrapper stays valid for the rest of the current frame. After `QueueFree`, check `Node.IsQueuedForDeletion()` (and `GodotObject.IsInstanceValid` for safety) before any further access. `PlayerInteractor.ResolveTarget` does both when re-resolving the raycast hit immediately after a pickup.

---

## Environment

- Godot 4.6.2 (.NET build, install via `brew install --cask godot-mono`)
- .NET SDK 10+
