# PROJECT-MAP.md — Royal Match Clone

> **Master file index.** Every source file is listed here with its purpose,
> path, and status. Use the search (`Ctrl+F` / `/`) to navigate to anything.

**Legend:** [X] = done | [~] = in-progress | [ ] = todo

---

## Table of Contents

1. [Root Configuration](#root-configuration)
2. [Context & Docs](#context--docs)
3. [Entry Points](#entry-points)
4. [Engine Layer (PlayCanvas)](#engine-layer)
5. [Game Logic Layer](#game-logic-layer)
6. [Level System](#level-system)
7. [UI Layer (React)](#ui-layer)
8. [Shared Types](#shared-types)
9. [Static Data](#static-data)
10. [Utilities](#utilities)
11. [Scripts](#scripts)
12. [Tests](#tests)
13. [Public Assets](#public-assets)
14. [Status Summary](#status-summary)

---

## Root Configuration

| File | Purpose | Status |
|------|---------|--------|
| `package.json` | Dependencies, scripts | [X] |
| `tsconfig.json` | TypeScript config | [X] |
| `tsconfig.node.json` | Node tsconfig for Vite config | [X] |
| `vite.config.ts` | Vite + Vitest + React plugin config | [X] |
| `.gitignore` | Git ignore rules | [X] |
| `CLAUDE.md` | Agent instructions | [X] |
| `project-context.xml` | Living project context | [X] |
| `eslint.config.mjs` | ESLint flat config | [ ] |

---

## Context & Docs

| File | Purpose | Status |
|------|---------|--------|
| `CLAUDE.md` | Agent instructions & quick commands | [X] |
| `project-context.xml` | Full project context (rules, architecture, schema) | [X] |
| `PROJECT-MAP.md` | This file — master index | [X] |
| `docs/architecture.md` | Architecture decisions & data flow diagrams | [ ] |
| `docs/level-design.md` | Level design philosophy, difficulty curve, objective types | [ ] |
| `docs/assets.md` | Asset pipeline, color palette, gem definitions | [ ] |
| `docs/gameplay.md` | Match-3 mechanics, special pieces, scoring | [ ] |

---

## Entry Points

| File | Purpose | Status |
|------|---------|--------|
| `src/main.tsx` | React entry, mounts App to root | [ ] |
| `src/App.tsx` | Root component, router setup, game canvas container | [ ] |
| `src/index.css` | Global styles, CSS variables, reset | [ ] |

---

## Engine Layer (PlayCanvas)

> Path: `src/engine/` — WebGL rendering bridge. Owns `PC.Application`.

| File | Purpose | Status |
|------|---------|--------|
| `src/engine/PlayCanvasApp.ts` | Initializes & manages the PlayCanvas `PC.Application` lifecycle | [ ] |
| `src/engine/GameRenderer.ts` | Renders GameState to PlayCanvas entities (gems, board, VFX) | [ ] |
| `src/engine/EntityPool.ts` | Object pool for gem entities & particles (perf) | [ ] |
| `src/engine/CameraController.ts` | Orthographic camera setup, fit-to-screen | [ ] |
| `src/engine/VFXManager.ts` | Particle effects: match explosions, sparkles | [ ] |
| `src/engine/AssetLoader.ts` | Loads meshes, materials, textures via PC.Loader | [ ] |
| `src/engine/types.ts` | Engine-specific type aliases & interfaces | [ ] |

---

## Game Logic Layer

> Path: `src/game/` — Pure logic, no React/PlayCanvas imports.

| File | Purpose | Status |
|------|---------|--------|
| `src/game/GameManager.ts` | Top-level orchestrator: state machine, level flow | [ ] |
| `src/game/GameState.ts` | The mutable game state (board, moves, score, goals) | [ ] |
| `src/game/Board.ts` | 2D grid of pieces, swap validation, match processing | [ ] |
| `src/game/Grid.ts` | Grid dimensions, cell indexing, bounds checking | [ ] |
| `src/game/Piece.ts` | Gem piece: type, position, special type, state | [ ] |
| `src/game/MatchFinder.ts` | Finds all matches of 3+ in the grid (horizontal/vertical) | [ ] |
| `src/game/DropSystem.ts` | Gravity simulation: fills empty cells from above | [ ] |
| `src/game/SpecialAbilities.ts` | Activates special pieces: line clear, bomb, color bomb | [ ] |
| `src/game/Scoring.ts` | Score calculation, combo multipliers, star thresholds | [ ] |
| `src/game/GoalChecker.ts` | Checks win conditions, objective progress | [ ] |
| `src/game/InputHandler.ts` | Normalizes touch/mouse input into board actions | [ ] |
| `src/game/AnimationQueue.ts` | Queues animations between logic steps (for rendering) | [ ] |
| `src/game/index.ts` | Barrel exports | [ ] |

---

## Level System

> Path: `src/levels/`

| File | Purpose | Status |
|------|---------|--------|
| `src/levels/LevelGenerator.ts` | Deterministic generator: produces LevelData from a seed | [ ] |
| `src/levels/LevelData.ts` | Zod schema + TypeScript interface for level JSON | [ ] |
| `src/levels/LevelLoader.ts` | Loads level JSON files & initializes GameState | [ ] |
| `src/levels/Difficulty.ts` | Difficulty enum + `getDifficulty(levelNumber)` function | [ ] |
| `src/levels/Objective.ts` | Objective types & validation | [ ] |
| `src/levels/index.ts` | Barrel exports | [ ] |
| `src/levels/levels/` | Generated level JSON files (1000 total, 100 per folder) | [ ] |
| &nbsp;&nbsp;`0001-0100/` | Levels 1–100 | [ ] |
| &nbsp;&nbsp;`0101-0200/` | Levels 101–200 | [ ] |
| &nbsp;&nbsp;`0201-0300/` | Levels 201–300 | [ ] |
| &nbsp;&nbsp;`...` | ... through 0901-1000 | [ ] |

---

## UI Layer (React)

> Path: `src/ui/`

| File | Purpose | Status |
|------|---------|--------|
| `src/ui/App.tsx` | Main router, game container | [ ] |
| `src/ui/hooks/useGameState.ts` | Zustand subscription hooks (moves, score, goals) | [ ] |
| `src/ui/hooks/useGameLoop.ts` | Sets up the game tick / animation frame loop | [ ] |
| `src/ui/components/Button.tsx` | Reusable button component | [ ] |
| `src/ui/components/GemIcon.tsx` | Small gem icon for UI (color prop) | [ ] |
| `src/ui/components/ProgressBar.tsx` | Moves / health / progress bar | [ ] |
| `src/ui/screens/SplashScreen.tsx` | Initial splash | [ ] |
| `src/ui/screens/MainMenu.tsx` | Main menu (play, settings, shop) | [ ] |
| `src/ui/screens/LevelSelect.tsx` | Level grid selector with star progress | [ ] |
| `src/ui/screens/GameScreen.tsx` | Active game screen (canvas + HUD overlay) | [ ] |
| `src/ui/screens/LevelComplete.tsx` | Post-level results (stars earned) | [ ] |
| `src/ui/components/HUD.tsx` | In-game HUD: moves, score, objectives | [ ] |

---

## Shared Types

> Path: `src/types/`

| File | Purpose | Status |
|------|---------|--------|
| `src/types/PieceType.ts` | Gem color/type enum (RED, BLUE, GREEN, YELLOW, PURPLE, CYAN) | [ ] |
| `src/types/SpecialType.ts` | Special piece enum (NONE, LINE, BOMB, COLOR_BOMB, ROCKET) | [ ] |
| `src/types/GameState.ts` | Core state interfaces (swappable for engine) | [ ] |
| `src/types/Difficulty.ts` | Difficulty enum | [ ] |
| `src/types/ObjectiveType.ts` | Objective type enum & union | [ ] |
| `src/types/Animation.ts` | Animation command types (MOVE, SCALE, DESTROY, SPAWN) | [ ] |
| `src/types/index.ts` | Barrel exports | [ ] |

---

## Static Data

> Path: `src/data/`

| File | Purpose | Status |
|------|---------|--------|
| `src/data/pieces.ts` | Piece definitions (color → hex, sprite info, score value) | [ ] |
| `src/data/colors.ts` | Palette definitions (gem colors, UI colors, particle colors) | [ ] |
| `src/data/goals.ts` | Objective type metadata (icon, description templates) | [ ] |
| `src/data/boosts.ts` | Booster definitions (shuffle, rocket, hammer) | [ ] |
| `src/data/boardConfig.ts` | Board dimensions, gem count, match rules | [ ] |
| `src/data/levelConfig.ts` | Level system config (total levels, difficulty multipliers) | [ ] |

---

## Utilities

> Path: `src/utils/`

| File | Purpose | Status |
|------|---------|--------|
| `src/utils/rng.ts` | Deterministic seedable RNG (alea/prng) | [ ] |
| `src/utils/math.ts` | Math helpers (lerp, clamp, euclidean mod) | [ ] |
| `src/utils/array.ts` | Array helpers (shuffle, weighted pick, grid 2D ops) | [ ] |
| `src/utils/debounce.ts` | Debounce & throttle | [ ] |
| `src/utils/deepClone.ts` | Structured clone for state snapshots | [ ] |

---

## Scripts

> Path: `scripts/`

| File | Purpose | Status |
|------|---------|--------|
| `scripts/generate-levels.ts` | Entry script: generates all 1000 level JSON files | [ ] |
| `scripts/validate-levels.ts` | Validates all generated level files against schema | [ ] |
| `scripts/build-assets.ts` | Asset manifest builder (textures → metadata) | [ ] |

---

## Tests

> Path: `tests/`

| File | Purpose | Status |
|------|---------|--------|
| `tests/setup.ts` | Vitest setup (matchMedia, ResizeObserver mocks) | [ ] |
| `tests/game/Board.test.ts` | Board swap & match tests | [ ] |
| `tests/game/MatchFinder.test.ts` | Match detection tests | [ ] |
| `tests/game/DropSystem.test.ts` | Gravity/drop tests | [ ] |
| `tests/game/SpecialAbilities.test.ts` | Special piece effect tests | [ ] |
| `tests/levels/LevelGenerator.test.ts` | Level generation & difficulty tests | [ ] |
| `tests/levels/LevelData.test.ts` | Schema validation tests | [ ] |

---

## Public Assets

> Path: `public/`

| Path | Purpose | Status |
|------|---------|--------|
| `public/index.html` | HTML entry point | [ ] |
| `public/manifest.json` | PWA manifest | [ ] |
| `public/assets/` | Static images, fonts, favicon | [ ] |

---

## Status Summary

| Layer | Files | Done | In Progress | Todo |
|-------|-------|------|-------------|------|
| Root Config | 5 | 5 | 0 | 0 |
| Context & Docs | 7 | 4 | 1 | 2 |
| Engine Layer | 7 | 0 | 0 | 7 |
| Game Logic | 14 | 0 | 0 | 14 |
| Level System | 7 + 10 dirs | 0 | 0 | 17 |
| UI Layer | 12 | 0 | 0 | 12 |
| Shared Types | 7 | 0 | 0 | 7 |
| Static Data | 6 | 0 | 0 | 6 |
| Utilities | 5 | 0 | 0 | 5 |
| Scripts | 3 | 0 | 0 | 3 |
| Tests | 7 | 0 | 0 | 7 |
| **Total** | **~90+** | **9** | **1** | **~81** |
