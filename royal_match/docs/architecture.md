# Architecture — Royal Match Clone

## Overview

The project is split into **three decoupled layers** to enable testable
game logic, swappable rendering, and a React UI layer.

```
┌─────────────────────────────────────────────────────────┐
│                    PRESENTATION (React)                  │
│  src/ui/ — screens, HUD, menus, state hooks (Zustand)   │
│     │                                                     │
│  Subscribes to GameState via Zustand store                │
└──────┬───────────────────────────────────────────────────┘
       │ (state mutations)
       ▼
┌─────────────────────────────────────────────────────────┐
│                    GAME LOGIC                           │
│  src/game/ — pure TS, no engine/React imports            │
│  Board, Grid, Piece, MatchFinder, DropSystem, Scoring    │
│  GameManager (state machine), InputHandler               │
└──────┬───────────────────────────────────────────────────┘
       │ (read-only state → render instructions)
       ▼
┌─────────────────────────────────────────────────────────┐
│                    RENDERING (PlayCanvas)                 │
│  src/engine/ — WebGL via PlayCanvas engine                │
│  PlayCanvasApp, GameRenderer, EntityPool, VFXManager     │
└─────────────────────────────────────────────────────────┘
```

## Why Three Layers?

1. **Testability** — The match-3 core (`src/game/`) runs in pure Node with
   Vitest, no WebGL browser required. All algorithms are unit-tested.
2. **Swappability** — The rendering engine could be swapped (e.g., PixiJS)
   without touching game logic. The `GameRenderer` interface defines the
   contract.
3. **Separation of concerns** — UI designers work in React; engine devs
   work in PlayCanvas; game designers work in pure logic.

## Data Flow (Game Tick)

```
Frame N:
  Input (touch/click) ──► React board overlay
                              │
                              ▼
                          GameManager.trySwap()
                              │
                              ▼
                        GameState (mutated)
                              │
                              ▼
              GameState ← Scoring, GoalChecker
                              │
                              ▼
              GameRenderer.render(GameState)  ──► PlayCanvas entities
                              │
                              ▼
              React UI re-renders (via Zustand subscription)
```

## State Management

- **Zustand** store holds the **GameState** (board, moves, score, goals,
  level number, status).
- React components subscribe to slices of the store.
- The PlayCanvas renderer reads the same GameState (passed via reference)
  each frame.
- **No two-way binding** between React and PlayCanvas — React owns UI,
  PlayCanvas owns canvas. Communication is unidirectional:
  UI actions → GameState mutation → Renderer reads.

## Rendering Strategy

- PlayCanvas uses an **orthographic camera** looking down the Z axis
  (top-down 2D board). Gem "gems" are simple cube/box primitives with
  colored materials + emissive glow.
- **Entity pooling**: Gem entities are created once at startup and pooled.
  Each frame, the renderer syncs entity positions/visibility/visibility
  to match the GameState Board.
- Game logic emits animation commands, but the current renderer synchronizes
  the resolved board immediately. Interpolated movement and particle rendering
  are explicitly pending presentation work.

## Level Storage & Loading

- 1000 levels are **generated at build/dev time** by `scripts/generate-levels.ts`.
- The generator is **deterministic** (seeded RNG) so any model can regenerate
  the exact same levels.
- Levels are stored as JSON: `src/levels/levels/{start}-{end}/level-{NNNN}.json`.
- At runtime, `LevelLoader` uses Vite's lazy `import.meta.glob`: all 1000 paths
  are known at build time, but only the selected level chunk is downloaded.

## Testing Strategy

| Scope | Tool | What |
|-------|------|------|
| Unit (logic) | Vitest | Board, MatchFinder, DropSystem, Scoring, LevelGenerator |
| Schema | Zod | LevelData JSON validation |
| Integration | Vitest | GameManager turns, terminal states and special swaps |
| E2E audit | Playwright/Chrome | Routes, WebGL, network, controls, win/loss and responsive |

Unit tests run in Node (jsdom) — **no browser needed** for game logic.
