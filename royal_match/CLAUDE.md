# CLAUDE — Royal Match Clone

> **This is the agent instruction file.** Read this BEFORE doing any work
> in `royal_match/`. It contains project conventions, commands, and
> context references so work is consistent and non-destructive.

## Project At a Glance

A mobile-first match-3 puzzle game (Royal Match style) using the **PlayCanvas**
engine for WebGL rendering + **React** for UI. 1000 levels with escalating
difficulty (every 3rd = Hard, every 6th = Super Hard).

## Quick Commands

```bash
# Start dev server (Vite, port 3000)
npm run dev

# Build for production
npm run build

# Run tests
npm test          # watch mode
npm run test:run  # single run (CI)
npm run test:coverage

# Typecheck
npm run typecheck

# Lint
npm run lint       # fix
npm run lint:check # check only

# Regenerate all 1000 level JSON files (deterministic)
npm run generate:levels
```

## Architecture Rules

1. **Game logic is framework-agnostic.** `src/game/` must NOT import `playcanvas`
   or `react`. It is pure TS classes/functions tested with Vitest.
2. **`src/engine/`** is the PlayCanvas bridge — owns the `PC.Application`,
   scene graph, and entity pooling. Exposes a clean API to render game state.
3. **`src/ui/`** is React. It subscribes to Zustand stores and renders HUD,
   menus, and overlays. It calls into the engine via the `GameRenderer` handle.
4. **Determinism:** Level generation and board logic MUST use `src/utils/rng.ts`
   (seeded RNG). Never `Math.random()`.

## Data Flow

```
LevelGenerator (seed) -> level JSON -> LevelLoader -> GameState (Zustand)
Game loop: Input -> GameManager -> Board -> MatchFinder -> DropSystem -> Scoring
Render: GameState -> GameRenderer (engine) -> PlayCanvas entities
UI: GameState -> React components (subscribe via useStore)
```

## Level Data

- **Generator:** `scripts/generate-levels.ts` (run `npm run generate:levels`)
- **Output:** `src/levels/levels/{start}-{end}/level-{NNNN}.json` (100 per folder)
- **Schema:** `src/levels/LevelData.ts` (Zod-validated)
- **Difficulty:** Level `% 6 == 0` → SUPER_HARD, `% 3 == 0` → HARD,
  levels 1–2 → EASY, else NORMAL.

## Context Files

- `project-context.xml` — **Living context** (what's built, rules, schema,
  progress). Update when scope changes. Commit with `CONTEXT:` prefix.
- `PROJECT-MAP.md` — **File index**. Read this to find any file fast.
- `docs/architecture.md` — Detailed architecture decisions.
- `docs/level-design.md` — Level design philosophy & difficulty curve.

## Commit Convention

```
feat: add match finder algorithm
fix: resolve gravity simulation edge case
docs: update PROJECT-MAP for new components
CONTEXT: mark engine layer complete
test: add board swap validation tests
refactor: extract piece type enum
```

## What's Built vs. Planned

See `<progress>` in `project-context.xml` for current milestone status.
See `PROJECT-MAP.md` for the full file listing with status badges.

---

*When in doubt, read `project-context.xml` first — it is the source of truth for
what the project is and how it works.*
