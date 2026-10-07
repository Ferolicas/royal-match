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
| `tsconfig.scripts.json` | TypeScript config for Node scripts | [X] |
| `vite.config.ts` | Vite + Vitest + React plugin config | [X] |
| `.gitignore` | Git ignore rules | [X] |
| `CLAUDE.md` | Agent instructions | [X] |
| `project-context.xml` | Living project context | [X] |
| `PROJECT-MAP.md` | This file — master index | [X] |
| `eslint.config.mjs` | ESLint flat config | [X] |
| `index.html` | HTML entry point (project root) | [X] |

---

## Context & Docs

| File | Purpose | Status |
|------|---------|--------|
| `CLAUDE.md` | Agent instructions & quick commands | [X] |
| `project-context.xml` | Full project context (rules, architecture, schema) | [X] |
| `PROJECT-MAP.md` | This file — master index | [X] |
| `docs/architecture.md` | Architecture decisions & data flow diagrams | [X] |
| `docs/level-design.md` | Level design philosophy, difficulty curve, objective types | [X] |
| `docs/assets.md` | Asset pipeline, color palette, gem definitions | [X] |
| `docs/gameplay.md` | Match-3 mechanics, special pieces, scoring | [X] |

---

## Entry Points

| File | Purpose | Status |
|------|---------|--------|
| `src/main.tsx` | React entry, mounts App to root | [X] |
| `src/ui/App.tsx` | Root component, router setup, game container | [X] |
| `src/index.css` | Global styles, CSS variables, reset | [X] |

---

## Engine Layer (PlayCanvas)

> Path: `src/engine/` — WebGL rendering bridge. Owns `PC.Application`.

| File | Purpose | Status |
|------|---------|--------|
| `src/engine/PlayCanvasApp.ts` | Initializes & manages the PlayCanvas `PC.Application` lifecycle | [X] |
| `src/engine/GameRenderer.ts` | Converts GameState/Board into renderable gem and obstacle state | [X] |
| `src/engine/EntityManager.ts` | Manages entity lifecycle and cell ID generation | [X] |
| `src/engine/EntityPool.ts` | Generic pool utility; live renderer currently owns its typed pools | [~] |
| `src/engine/CameraController.ts` | Orthographic camera setup, fit-to-screen, screen-to-board | [X] |
| `src/engine/VFXManager.ts` | Particle simulation model; PlayCanvas particle rendering is not wired | [~] |
| `src/engine/AssetLoader.ts` | Procedural material factory, asset manifest builder | [X] |
| `src/engine/types.ts` | Engine-specific type aliases & interfaces | [X] |
| `src/engine/index.ts` | Barrel exports | [X] |

---

## Game Logic Layer

> Path: `src/game/` — Pure logic, no React/PlayCanvas imports.

| File | Purpose | Status |
|------|---------|--------|
| `src/game/GameManager.ts` | Top-level orchestrator: state machine, level flow, scoring | [X] |
| `src/game/GameState.ts` | Game state interface, create(), star/win checks | [X] |
| `src/game/Board.ts` | 2D grid of pieces, swap validation, match processing, gravity, fill | [X] |
| `src/game/Grid.ts` | Grid dimensions, cell indexing, bounds checking, swap | [X] |
| `src/game/Piece.ts` | Gem piece: type, position, special type, state | [X] |
| `src/game/MatchFinder.ts` | Finds all maximal matches of 3+ in the grid | [X] |
| `src/game/DropSystem.ts` | Gravity simulation: fills empty cells from above | [X] |
| `src/game/SpecialAbilities.ts` | Activates special pieces: line clear, bomb, color bomb, rocket | [X] |
| `src/game/Scoring.ts` | Score calculation, combo multipliers, star thresholds | [X] |
| `src/game/GoalChecker.ts` | Checks win conditions, objective progress | [X] |
| `src/game/InputHandler.ts` | Optional pointer normalizer; current React overlay handles input directly | [~] |
| `src/game/AnimationQueue.ts` | Animation command queue; commands exist but visual interpolation is pending | [~] |
| `src/game/index.ts` | Barrel exports | [X] |

---

## Level System

> Path: `src/levels/`

| File | Purpose | Status |
|------|---------|--------|
| `src/levels/LevelGenerator.ts` | Deterministic generator: produces LevelData from a seed | [X] |
| `src/levels/LevelData.ts` | Zod schema + TypeScript interface for level JSON | [X] |
| `src/levels/LevelLoader.ts` | Loads level JSON files & initializes GameState | [X] |
| `src/levels/Difficulty.ts` | Difficulty enum re-export + `getDifficulty`, `getSeed`, `computeMoveLimit` | [X] |
| `src/levels/index.ts` | Barrel exports | [X] |
| `src/levels/levels/` | Generated level JSON files (1000 total, 100 per folder) | [X] |
| &nbsp;&nbsp;`0001-0100/` | Levels 1–100 | [X] |
| &nbsp;&nbsp;`0101-0200/` | Levels 101–200 | [X] |
| &nbsp;&nbsp;`0201-0300/` | Levels 201–300 | [X] |
| &nbsp;&nbsp;`...` | ... through 0901-1000 | [X] |

---

## UI Layer (React)

> Path: `src/ui/`

| File | Purpose | Status |
|------|---------|--------|
| `src/ui/App.tsx` | Main router, game container | [X] |
| `src/ui/store.ts` | Zustand store with game state & actions | [X] |
| `src/ui/hooks/useGameState.ts` | Zustand subscription hooks (moves, score, goals, status) | [X] |
| `src/ui/hooks/useGameLoop.ts` | Reserved animation-frame hook; not used by the current synchronous renderer | [ ] |
| `src/ui/components/Button.tsx` | Reusable button component | [X] |
| `src/ui/components/GemIcon.tsx` | Small gem icon for UI (color prop) | [X] |
| `src/ui/components/ProgressBar.tsx` | Moves / health / progress bar | [X] |
| `src/ui/components/HUD.tsx` | In-game HUD: moves, score, objectives, stars | [X] |
| `src/ui/screens/SplashScreen.tsx` | Initial splash with loading bar | [X] |
| `src/ui/screens/MainMenuScreen.tsx` | Main menu (play and settings) | [X] |
| `src/ui/screens/LevelSelectScreen.tsx` | Level grid selector with star progress | [X] |
| `src/ui/screens/GameScreen.tsx` | Active game screen (canvas + HUD overlay) | [X] |
| `src/ui/screens/LevelCompleteScreen.tsx` | Post-level results (stars earned) | [X] |
| `src/ui/screens/SettingsScreen.tsx` | Persistent sound-effect and vibration preferences | [X] |
| `src/ui/screens/ShopScreen.tsx` | Unrouted future shop placeholder | [ ] |

---

## Shared Types

> Path: `src/types/`

| File | Purpose | Status |
|------|---------|--------|
| `src/types/PieceType.ts` | Gem color/type enum (RED, BLUE, GREEN, YELLOW, PURPLE, CYAN) | [X] |
| `src/types/SpecialType.ts` | Special piece enum (NONE, LINE, BOMB, COLOR_BOMB, ROCKET) | [X] |
| `src/types/GameState.ts` | Core state interfaces (Point, Match, CellContent, AnimationCommand) | [X] |
| `src/types/Difficulty.ts` | Difficulty enum (EASY, NORMAL, HARD, SUPER_HARD) | [X] |
| `src/types/ObjectiveType.ts` | Objective type enum & union interfaces | [X] |
| `src/types/index.ts` | Barrel exports | [X] |

---

## Static Data

> Path: `src/data/`

| File | Purpose | Status |
|------|---------|--------|
| `src/data/colors.ts` | Palette definitions (gem colors, UI colors, particle colors) | [X] |
| `src/data/pieces.ts` | Piece definitions (color → hex, sprite info, score value) | [X] |
| `src/data/goals.ts` | Objective type metadata (icon, description templates) | [X] |
| `src/data/boosts.ts` | Booster definitions (shuffle, hammer, rocket) | [X] |
| `src/data/boardConfig.ts` | Board dimensions, gem count, match rules, animation durations | [X] |
| `src/data/levelConfig.ts` | Level system config (total levels, difficulty multipliers) | [X] |

---

## Utilities

> Path: `src/utils/`

| File | Purpose | Status |
|------|---------|--------|
| `src/utils/rng.ts` | Deterministic seedable RNG (LCG algorithm) | [X] |
| `src/utils/math.ts` | Math helpers (lerp, clamp, euclidean mod, distance) | [X] |
| `src/utils/array.ts` | Array helpers (shuffle, weighted pick, create2DArray, deepClone, debounce) | [X] |

---

## Scripts

> Path: `scripts/`

| File | Purpose | Status |
|------|---------|--------|
| `scripts/generate-levels.ts` | Generates all 1000 level JSON files deterministically | [X] |
| `scripts/validate-levels.ts` | Validates all generated level files against schema | [X] |
| `scripts/build-assets.ts` | Asset manifest builder (textures → metadata) | [X] |

---

## Tests

> Path: `tests/`

| File | Purpose | Status |
|------|---------|--------|
| `tests/setup.ts` | Vitest setup (matchMedia, ResizeObserver, localStorage mocks) | [X] |
| `tests/game/Board.test.ts` | Board init, swap, match, gravity, fill, clone tests | [X] |
| `tests/game/MatchFinder.test.ts` | Match detection (horizontal, vertical, 4+, multi, dedup) | [X] |
| `tests/game/DropSystem.test.ts` | Gravity/drop/fill tests | [X] |
| `tests/game/SpecialAbilities.test.ts` | Special piece effect tests | [X] |
| `tests/game/GameManager.test.ts` | Turns, terminal states, invalid and special swaps | [X] |
| `tests/game/Scoring.test.ts` | Match-size, cascade and star-rating regressions | [X] |
| `tests/levels/LevelGenerator.test.ts` | Level generation & difficulty tests | [X] |
| `tests/levels/LevelData.test.ts` | Schema validation tests | [X] |

---

## Public Assets

> Path: `public/`

| Path | Purpose | Status |
|------|---------|--------|
| `public/manifest.json` | PWA manifest | [X] |
| `public/assets/` | Static images, fonts, favicon | [X] |
| &nbsp;&nbsp;`favicon.svg` | Gem grid favicon | [X] |

---

## Status Summary

| Layer | Files | Done | In Progress | Todo |
|-------|-------|------|-------------|------|
| Root Config | 11 | 11 | 0 | 0 |
| Context & Docs | 7 | 7 | 0 | 0 |
| Entry Points | 3 | 3 | 0 | 0 |
| Engine Layer | 9 | 7 | 2 | 0 |
| Game Logic | 13 | 11 | 2 | 0 |
| Level System | 6+10 dirs | 6+10 | 0 | 0 |
| UI Layer | 15 | 13 | 0 | 2 |
| Shared Types | 6 | 6 | 0 | 0 |
| Static Data | 6 | 6 | 0 | 0 |
| Utilities | 3 | 3 | 0 | 0 |
| Scripts | 3 | 3 | 0 | 0 |
| Tests | 9 | 9 | 0 | 0 |
| Public Assets | 2 | 2 | 0 | 0 |
| **Total** | **~85** | **79** | **4** | **2** |
