# 07_persistence_in_godot — Persistence Sandbox

Phase 8 of Sandbox_003. Single-screen 2D sandbox demonstrating robust save / settings persistence
informed by 2024-2026 indie best practice and Godot 4.6 source-level constraints.

---

## Why no encryption?

A research spike across shipped 2024-2026 indie titles (Balatro, Animal Well, UFO 50, Pacific Drive,
Brotato, Halls of Torment) found zero adoption of save-data encryption -- all of them ship with save
editors freely circulating, and none have been commercially harmed. The industry consensus for solo
single-player games is:

| Concern | Industry Decision | Notes |
|---------|-------------------|-------|
| Encryption | Skip | Cost without benefit; the key always ships inside the binary |
| Integrity check | Mandatory | CRC32 / xxHash detects accidental truncation and bit rot |
| Atomic write | Mandatory | Crash mid-write must not corrupt prior state |
| Multi-generation backup | Mandatory | Rotated AFTER successful write, never before |
| Fail-soft on corruption | Preferred | Animal Well's mantis-instead-of-error-screen pattern |

This sandbox follows that consensus.

---

## Why no `OS.SetUseFileAccessSaveAndSwap`?

The API does not exist. Godot's atomic-write helper (`FileAccess::set_backup_save`) is C++-only,
unbound to scripting, and disabled by default in shipped runtimes. Issue #98360 confirms file
corruption on power loss (Ubuntu ~25%, Windows ~100%). PR #98361 (fsync addition) is unmerged
in Godot 4.6. The only safe path is **manual `temp -> rename`** in user code.

---

## Architecture

### Layered Design

```text
UI Layer (Godot)
    MainScreen.cs (single CanvasLayer, all controls built in _Ready)
        |
        v
Godot IO Layer (Humble Object Shells)
    SaveSystem.cs        SettingsSystem.cs
    (FileAccess +        (ConfigFile)
     DirAccess.Rename)
        |
        v
Pure C# Layer (zero Godot dependencies, fully unit-testable)
    SaveSerializer       SaveDataMigrator       Crc32
    SaveData (POCO)      SettingsData (POCO)
```

### Save Flow (write)

```text
1. Serialize POCO -> byte[] payload
2. Build packet: [Magic][Version][PayloadLen][Payload][CRC32]
3. Open user://save.dat.tmp, write packet, close
4. On success:
     bak1 := bak0       (rotate older generation)
     bak0 := main       (rotate current)
     main := tmp        (DirAccess.Rename, atomic on POSIX/NTFS)
5. On any failure: leave existing main / bak0 / bak1 untouched
```

Backups are rotated AFTER the new file is durably on disk, so a crash at any step
leaves at least one valid generation behind.

### Load Flow (read)

```text
try main:
    open, read, verify magic + version + CRC32
    return on success
try bak0:
    same verification
    return on success
try bak1:
    same verification
    return on success
fail-soft:
    return SaveData.CreateDefault() and surface a non-blocking warning
```

No exception is allowed to escape into the UI. Corruption is a normal flow path.

---

## Persistence APIs

### Godot APIs (used by the IO layer)

| API | Purpose |
|-----|---------|
| `FileAccess.Open(path, ModeFlags.Write)` | Write `.tmp` file |
| `FileAccess.Open(path, ModeFlags.Read)` | Read main / backup files |
| `FileAccess.StoreBuffer(byte[])` | Raw byte write, no Variant surface |
| `FileAccess.GetBuffer(int)` | Raw byte read, no Variant surface |
| `DirAccess.Rename(from, to)` | Atomic rename for the swap step |
| `DirAccess.RemoveAbsolute(path)` | Cleanup of stale `.tmp` on next launch |
| `ConfigFile.SetValue(section, key, value)` | Settings write |
| `ConfigFile.GetValue(section, key, default)` | Settings read with default fallback |
| `ConfigFile.Save(path)` / `Load(path)` | Settings file round-trip (plain INI) |

`StoreVar` / `GetVar` are deliberately avoided. Variant binary is convenient but reintroduces
type ambiguity and a parser surface that we control nothing about; raw `byte[]` keeps the format
fully owned by `SaveSerializer`.

### Pure C# Helpers (used by the IO layer, fully unit-testable)

| Class | Responsibility |
|-------|----------------|
| `SaveSerializer` | Encode / decode POCO <-> packet bytes via `Stream`; no `FileAccess` reference |
| `SaveDataMigrator` | Walk `save_version` from N to current via stepwise pure transforms |
| `Crc32` | IEEE 802.3 polynomial; deterministic, allocation-free |
| `SaveData` | POCO carrying `save_version`, timestamp, sequence number, gameplay state |
| `SettingsData` | POCO with `CreateDefault`, `IsValid`, `Clamped` |

The Stream-based serializer is the key testability lever: tests feed `MemoryStream` directly,
bypassing Godot entirely.

---

## Save File Format

```text
Offset  Size   Field
------  -----  -----
0       4      Magic   "SBNX" (ASCII)
4       4      Version uint32, little-endian, current schema version
8       4      Length  uint32, little-endian, payload byte count
12      N      Payload N bytes, self-describing binary dictionary
12+N    4      CRC32   uint32, little-endian, IEEE polynomial over [0..12+N)
```

The payload is a hand-rolled binary dictionary serializer:

- Length-prefixed UTF-8 strings
- Tagged primitives (int32, int64, float, bool, string, byte[])
- No Variant, no JSON, no `str2var`, no arbitrary-code-execution surface

CRC32 covers everything before the CRC field itself, so truncation, bit flip, or partial write
all surface as integrity failure rather than silent data loss.

---

## UI

A single `MainScreen.cs` builds the entire UI in `_Ready()`. No `.tscn` authoring beyond the
empty `Main.tscn` entry point.

| Section | Controls |
|---------|----------|
| State | Money / Day spinboxes, inventory list (item id x count) with add / remove |
| Settings | BGM volume slider, SFX volume slider, display mode dropdown (Windowed / Borderless / Fullscreen) |
| Actions (Game) | Save, Load, Delete, Force-Corrupt (debug) |
| Actions (Settings) | Save, Load, Reset to Default |
| Actions (Global) | Reset All, Quit |
| Status | Last operation result, color-coded (OK / warning / error / info) |

The all-in-one screen keeps the demo focused on persistence behavior; layout fidelity is not the
point.

---

## Test Coverage

xUnit suite over the pure C# layer. Godot-facing shells (`SaveSystem`, `SettingsSystem`) are
exercised manually via the running scene per project policy.

| Test Class | Coverage |
|------------|----------|
| `SaveSerializerTests` | Round-trip via `MemoryStream`; CRC32 mismatch detection; truncation detection; magic / version mismatch detection; empty payload edge case |
| `SaveDataMigratorTests` | UpToDate / Migrated / FromFuture / Invalid status; stepwise version walk; input non-mutation |
| `Crc32Tests` | Known-vector parity (`"123456789"` -> `0xCBF43926`); empty input; incremental update equivalence |
| `SettingsDataTests` | `CreateDefault` values; `IsValid` for out-of-range; `Clamped` behavior; instance identity |

`SaveSerializer` is built around `Stream` precisely so its tests never touch `FileAccess`. This is
the Humble Object pattern at the persistence boundary.

---

## Project Structure

```text
07_persistence_in_godot/
|-- Scripts/
|   |-- Persistence/    SaveSystem, SaveData, SaveSerializer, SaveDataMigrator, Crc32
|   |-- Settings/       SettingsSystem, SettingsData
|   `-- UI/             MainScreen
|-- Scenes/             Main.tscn (single entry point)
|-- Tests/              SaveSerializerTests, SaveDataMigratorTests, Crc32Tests, SettingsDataTests
|-- 07_persistence_in_godot.csproj
`-- project.godot
```

---

## Build & Verify

```bash
dotnet build 07_persistence_in_godot.csproj
dotnet test  Tests/Tests.csproj
```

Editor launch (`godot` / `godot --headless`) is intentionally not part of verification, per
Sandbox_003 policy. Runtime behavior is confirmed by hand inside the running game; static
correctness is enforced by `dotnet build` plus the xUnit suite.

---

## Out of Scope

- Encryption -- 2024-2026 indie consensus says skip for solo single-player; the key would ship inside the binary anyway
- Multiple save slots -- single slot keeps the demo focused on the persistence pattern itself
- Cloud sync -- out of scope for a local sandbox; `SaveData` carries a timestamp and sequence number so a future cloud-conflict resolver can be added without format change
- HMAC / AEAD -- overkill given the threat model (offline single-player, no server, no leaderboard)

---

## Research Trail

This implementation follows two prior research spikes:

1. **Indie save protection in 2024-2026 commercial titles** (Balatro, Animal Well, UFO 50, Pacific Drive, Brotato, Halls of Torment). None encrypt, all rely on integrity + atomic write + backup + fail-soft.
2. **Godot 4.6 C# encryption / atomic-write API audit against engine source.** `FileAccess::set_backup_save` is unbound to scripting and off by default; issue #98360 documents real-world corruption rates; PR #98361 is unmerged.

The "encryption is skipped" decision is grounded in (1); the "manual temp + rename" implementation
is forced by (2).

---

## Status

Learning sandbox. Not a shipping product.
