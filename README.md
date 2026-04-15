# 🧵 Tejiendo Rutas

> **Weaving Paths** — a 2D/3D puzzle-platformer where you restore colour and life to a grey city by weaving coloured threads between nodes of energy, data and social collaboration.

---

## 🎮 Concept (Elevator Pitch)

The player controls a character who must **connect** fragments of a grey city using coloured threads that represent data, energy and social collaboration.  
As circuits are closed the city comes alive — grey buildings bloom with colour, particles burst out, and the social-welfare score rises.

Inspired by *Unravel* and *Mini Metro*, with an isometric low-poly aesthetic.

---

## 🏗 Project Structure

```
Assets/
├── Scripts/
│   ├── PlayerController.cs    — Phase 1: Fluid CharacterController movement
│   ├── ThreadManager.cs       — Phase 2: LineRenderer thread / rope mechanic
│   ├── NodePoint.cs           — Anchor nodes in the city grid
│   ├── CircuitManager.cs      — Phase 3: Circuit detection, particles & colour
│   ├── ColorRestorer.cs       — Per-prop grey-to-colour material transition
│   └── GameManager.cs         — Score (bienestar), HUD, win condition
├── Tests/
│   └── EditMode/
│       └── CircuitManagerTests.cs   — NUnit tests for core game logic
Packages/
└── manifest.json              — URP, Input System, Cinemachine, TextMeshPro
ProjectSettings/
└── ProjectSettings.asset
```

---

## ⚙️ Unity Version & Packages

| Item | Value |
|------|-------|
| Unity | 2022.3 LTS (recommended) |
| Render Pipeline | Universal Render Pipeline (URP) 14 |
| Input System | com.unity.inputsystem 1.7 |
| Cinemachine | 2.9.7 |
| TextMeshPro | 3.0.6 |

---

## 🚀 Implementation Phases

### Phase 1 — Character Controller (`PlayerController.cs`)
- Camera-relative movement using Unity's built-in `CharacterController`
- Smooth acceleration / deceleration via `Vector3.MoveTowards`
- Gravity simulation and single jump
- **E / Fire1** — interact with nearest node (start/complete a thread)
- **R / Fire2** — cancel the current thread

### Phase 2 — Thread Mechanic (`ThreadManager.cs`)
- Limited thread resource (configurable `maxThreadLength`)
- Live preview line rendered via `LineRenderer` while drawing
- Thread colour matches the **NodeType** (Energy = yellow, Data = cyan, Social = magenta)
- On completion a permanent child `LineRenderer` segment is spawned

### Phase 3 — Circuit Events (`CircuitManager.cs`)
- Iterative DFS cycle detection over the connection graph after every new thread
- Fires `OnCircuitClosed` UnityEvent with node positions
- Instantiates a **ParticleSystem** burst at each circuit node
- Starts a coroutine that lerps nearby `ColorRestorer` objects from grey → vivid colour
- Duplicate circuit keys are tracked so the same loop only fires once

---

## 🎨 Visual Style

- Starts in **greyscale** — environment renderers use `MaterialPropertyBlock` to avoid extra material instances
- Colour "blooms" outward from each closed circuit (radius configurable on `CircuitManager`)
- Smooth `Color.Lerp` transition over a configurable duration

---

## 🧪 Tests

Edit-mode NUnit tests live in `Assets/Tests/EditMode/CircuitManagerTests.cs`:

| Test | Verifies |
|------|----------|
| `NodePoint_Connect_RegistersBidirectionalConnection` | Connections register on both nodes |
| `NodePoint_Connect_IgnoresDuplicateConnections` | No duplicate edges |
| `NodePoint_Connect_IgnoresSelfConnection` | Self-loops are rejected |
| `NodePoint_Disconnect_RemovesBidirectionalConnection` | Clean removal |
| `ThreadSegment_StoresNodeReferences` | Data container integrity |
| `CircuitManager_NoCycle_NoEventFired` | Open chain produces no event |
| `CircuitManager_ClosedTriangle_EventFiredOnce` | Triangle circuit fires exactly once |
| `GameManager_ScoreStartsAtZero` | Clean initial state |

Run them via **Window → General → Test Runner → EditMode** inside the Unity Editor.

---

## 🕹 Controls

| Key | Action |
|-----|--------|
| WASD / Arrow keys | Move character |
| Space | Jump |
| E / Left Click | Start or complete a thread connection |
| R / Right Click | Cancel current thread |

---

## 📐 Scene Setup (Quick Start)

1. Create a **3D** Unity project with URP.
2. Add the scripts from `Assets/Scripts/` to the project.
3. Create a **Player** GameObject:
   - `CharacterController` component
   - `PlayerController` script
   - `ThreadManager` script
4. Create **NodePoint** GameObjects (one per city anchor):
   - Collider (Sphere/Box) on each
   - `NodePoint` script — set **Type** and **Node Color**
5. Add a **CircuitManager** GameObject in the scene and link the `ThreadManager`.
6. Add a **GameManager** GameObject; wire up HUD `TextMeshPro` / `Slider` references.
7. Add `ColorRestorer` to any environment mesh that should bloom with colour.
8. Assign a **ParticleSystem** prefab to the `CircuitManager.celebrationParticlePrefab` slot.

---

## 🌆 Concept Art Prompt

> *"Concept art for a video game made in Unity, 3D isometric view, a colorful thread connecting gray buildings in a modern city, stylized art, soft lighting, vibrant colors appearing where the thread touches, inspiration from Monument Valley and Unravel, high resolution."*

---

## 🧠 Narrative

The game is a metaphor for how technology and economics **weave** the future of cities.  
Each thread the player lays down is a data pipeline, an energy grid, or a social programme — and together they transform a lifeless grey cityscape into a vibrant, living community.
