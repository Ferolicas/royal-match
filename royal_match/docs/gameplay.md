# Gameplay — Royal Match Clone

## Core Loop

```
1. Player selects a gem (touch/click).
2. Player selects an adjacent gem (tap/click).
3. Game validates: does the swap create a match-3+?
   → If NO: swap is rejected and no move is consumed.
   → If YES: resolve matches, apply gravity, cascade.
4. Score is calculated, goals are checked.
5. If no moves remain but goals met → LEVEL COMPLETE.
   If no moves remain and goals not met → LEVEL FAILED.
   Else → player's next turn.
```

## Match-3 Mechanics

### Standard Matches

- **Match-3**: Score 60 points. Gem color cleared.
- **Match-4 (line)**: Creates a **Line Clear** special (clears a row/column).
- **Match-5 (T)**: Creates a **Bomb** special (clears 3×3 area).
- **Match-5 (L)**: Creates a **Color Bomb** (clears all of one color).
- **Match-6+ (line of 6)**: Creates a **Rocket** (clears entire row/column).

### Matching Shapes

The `MatchFinder` detects matches by scanning each row and column for
consecutive gems of the same color. A "match" object records:

```ts
{ start: {row, col}, end: {row, col}, length: number, direction: 'H' | 'V' }
```

### Special Piece Creation Rules

| Match Pattern | Special Piece |
|--------------|---------------|
| 4 in a row (straight line) | Line Clear |
| 5 in a T-shape | Bomb |
| 5 in an L-shape (4+1 or 1+4) | Color Bomb |
| 6 in a straight line | Rocket |
| 5+ in a straight line (non-T/L) | Line Clear (upgraded) |

### Special Piece Effects

- **Line Clear**: Clears all gems in a row AND column through the placement
  cell (cross pattern). Score: 120.
- **Bomb**: Clears a 3×3 area centered on placement. Score: 180. If swapped,
  detonates instead of moving.
- **Color Bomb**: Clears ALL gems of the color it's swapped with. Score: 250.
  If swapped with empty, clears a random color.
- **Rocket**: Clears the entire row OR column (chosen on placement). Score: 200.

### Cascading

After matches are cleared, gravity pulls all gems down. If new matches form,
they're resolved in the same cascade with increasing score multipliers:

```
Cascade 1: ×1.0
Cascade 2: ×2.0
Cascade 3: ×3.0
Cascade N: ×N.0
```

### Swap Validation

A swap is only valid if **at least one side** of the resulting board has a
match-3+ of the two swapped gems. "Shuffle" boosters can be used when no
valid moves exist.

## Objectives & Win Conditions

### Standard Objectives

1. **Collect Color** (e.g., "Collect 20 RED gems") — accumulate by matching
   that color anywhere on the board.
2. **Reach Score** (e.g., "Score 5000 points") — earn via matches & cascades.
3. **Remove Blocker** (e.g., "Remove 5 boxes") — destroy obstacle cells
   (usually requires special pieces or matching adjacent to them).
4. **Collect Item** — reserved in the schema, not used by generated levels.
5. **Drop Down** — reserved in the schema, not used by generated levels.

### Victory

Level is **completed** when ALL objectives reach their target. Stars awarded:
- 1 star: minimum score threshold met.
- 2 stars: 1.5× threshold.
- 3 stars: 2.2× threshold.

### Defeat

Level is **failed** when:
- Moves reach 0 AND objectives not all met.
- No valid moves exist after resolution and objectives are not met.

## Progression

- Complete a level with ≥1 star to unlock the next.
- 3 stars on a level = "Mastered" — grants bonus rewards.
- World map shows progress: `O` (unlocked), `★` (1 star), `★★` (2 stars),
  `★★★` (3 stars, mastered).

## Move Counter & Scoring

```
Match-3 score  = 60 × cascade_number
Match-4 line   = 120 × cascade_number
Bomb (5)       = 180 × cascade_number
Color Bomb     = 250 × cascade_number
Rocket         = 200 × cascade_number
```

Reward metadata exists in the level files, but there is currently no wallet,
booster economy, or shop in the runtime UI.
