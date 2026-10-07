# Level Design — Royal Match Clone

## Philosophy

The 1000-level campaign mirrors Royal Match's progression: early levels
teach mechanics gently; difficulty escalates with **thinner move budgets**,
**larger goal targets**, and **denser obstacles**. Every 3rd level is Hard,
every 6th is Super Hard — a rhythm that creates peaks of tension and
satisfaction on victories.

## Difficulty Rules

| Level Range | Label | Rule |
|-------------|-------|------|
| 1–2 | EASY | Tutorial. Introduce swap, match, clear. No goals beyond score. |
| `% 6 == 0` | SUPER_HARD | Every 6th level (6, 12, 18, ... 996). |
| `% 3 == 0` (else) | HARD | Every 3rd level not divisible by 6 (3, 9, 15, ...). |
| else | NORMAL | Standard progression. |

## How Difficulty Affects Level Params

Each difficulty tier modifies these knobs from the **base values**:

| Param | EASY | NORMAL | HARD | SUPER_HARD |
|-------|------|--------|------|------------|
| Move limit | 18 | 15 | 13 | 11 |
| Goal target multiplier | 0.7× | 1.0× | 1.4× | 2.0× |
| Obstacle density on board | none | light (5–10%) | moderate (15–25%) | heavy (30–45%) |
| Maximum obstacles | none | 2 | 5 | 8 |
| Board holes (void cells) | none | 1 | 3 | 6 |

### Worked Example

- **Level 9 (HARD):** 8×8 board, 13 moves and up to 5 obstacles.
- **Level 12 (SUPER_HARD):** 8×8 board, 11 moves (15 − 4), 3 objectives at 2×
  targets, 35% obstacle density, 8 pre-placed blockers.
- **Level 15 (HARD):** 8×8 board, 13 moves (15 − 2), objectives at 1.4× targets,
  20% obstacles.

## Objective Types

| ID | Name | Description |
|----|------|-------------|
| `collect_color` | Collect Gems | Match gems of a specific color. |
| `reach_score` | Score Target | Achieve at least N points. |
| `remove_blocker` | Clear Blockers | Destroy obstacle cells (boxes, cages). |
| `collect_item` | Collect Items | Schema-reserved; not generated until item entities exist. |
| `drop_down` | Drop Object | Schema-reserved; not generated until drop objects exist. |

Generated levels use only the three implemented objective types. Normal levels
have two objectives; Hard and Super Hard levels have up to three. Targets scale
by difficulty while blocker targets never exceed blockers present on the board.

## Star Thresholds (Scoring)

Stars are awarded based on score relative to a base score for the level:

| Stars | Condition |
|-------|-----------|
| 1 star (bronze) | Score ≥ star1 (1.0× base) |
| 2 stars (silver) | Score ≥ star2 (1.5× base) |
| 3 stars (gold) | Score ≥ star3 (2.2× base) |

Base score is derived from: move limit × avg points-per-move × difficulty
multiplier. Harder levels have higher base scores but require more efficiency.

## Visual Style Progression

- Levels 1–100: **Tutorial Island** — bright pastels, simple shapes, no
  obstacles.
- Levels 101–400: **Forest Realm** — green themes, light obstacles (boxes,
  grass).
- Levels 401–700: **Mountain Realm** — stone/crate obstacles, ice tiles
  (slippery — future).
- Levels 701–1000: **Castle Realm** — heavy blockers (cages, locked tiles),
  complex multi-objective levels.

## Generation Algorithm (Deterministic)

The generator (`scripts/generate-levels.ts`) uses a seeded RNG so that:

```
generateLevel(42) → always produces the same LevelData for level 42
```

Seed derivation: `SEED_BASE = 0x7A4E` (constant). For level N:
`seed = SEED_BASE + N`. This means changing N by 1 changes all board layout,
goals, and obstacles predictably.

Steps:
1. Compute difficulty from level number.
2. Derive RNG seed.
3. Set base params (grid 8×8, moveLimit, objectives count) from difficulty.
4. Scatter a bounded number of obstacles and void cells using seeded random.
5. Pick objective types & targets (weighted, scaling by difficulty).
6. Reject initial matches and regenerate until at least one legal swap exists.
   This is an opening-state guarantee, not a proof that every objective can be
   completed under every sequence of moves.
7. Write JSON with schema validation.
