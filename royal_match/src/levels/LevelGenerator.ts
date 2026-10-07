import { DeterministicRNG } from '../utils/rng';
import {
  Difficulty,
  getDifficulty,
  getSeed,
  computeMoveLimit,
  LEVELS_PER_FOLDER,
  TOTAL_LEVELS,
  DIFFICULTY_MULTIPLIERS,
  LEVEL_SCHEMA_VERSION,
  GRID_ROWS,
  GRID_COLS,
} from './Difficulty';
import { LevelDataSchema, type LevelData, type CellValue, type ObjectiveData } from './LevelData';
import { GEM_TYPES } from '../data/pieces';
import { ObjectiveType } from '../types/ObjectiveType';

interface ObstacleSpec {
  density: number;
  types: CellValue[];
  blockerCount: number;
}

const OBSTACLE_SPECS: Record<Difficulty, ObstacleSpec> = {
  [Difficulty.EASY]: { density: 0, types: ['BLOCKED'], blockerCount: 0 },
  [Difficulty.NORMAL]: {
    density: 0.08,
    types: ['OBSTACLE_BOX', 'OBSTACLE_CAGE'],
    blockerCount: 2,
  },
  [Difficulty.HARD]: {
    density: 0.18,
    types: ['OBSTACLE_BOX', 'OBSTACLE_CAGE', 'OBSTACLE_ICE'],
    blockerCount: 5,
  },
  [Difficulty.SUPER_HARD]: {
    density: 0.35,
    types: ['OBSTACLE_BOX', 'OBSTACLE_CAGE', 'OBSTACLE_ICE', 'OBSTACLE_STONE'],
    blockerCount: 8,
  },
};

function createEmptyBoard(rows: number, cols: number): CellValue[][] {
  return Array.from({ length: rows }, () =>
    Array.from({ length: cols }, () => 'EMPTY' as CellValue)
  );
}

function placeObstacles(
  board: CellValue[][],
  rng: DeterministicRNG,
  spec: ObstacleSpec,
  rows: number,
  cols: number
): { obstacleCells: number; blockedCount: number } {
  let obstacleCells = 0;
  let blockedCount = 0;

  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      if (board[r][c] !== 'EMPTY') continue;

      if (rng.next() < spec.density && spec.blockerCount > blockedCount) {
        const obstacleType = rng.pickOne(spec.types)!;
        board[r][c] = obstacleType;
        blockedCount++;
        if (obstacleType !== 'BLOCKED') {
          obstacleCells++;
        }
        continue;
      }

      if (rng.next() < 0.03) {
        board[r][c] = 'VOID' as CellValue;
      }
    }
  }

  return { obstacleCells, blockedCount };
}

function fillGems(board: CellValue[][], rng: DeterministicRNG, rows: number, cols: number): void {
  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      if (board[r][c] === 'EMPTY') {
        const color = rng.pickOne(GEM_TYPES)!;
        board[r][c] = `GEM_${color}` as CellValue;
      }
    }
  }
}

function removeAdjacentSameColors(
  board: CellValue[][],
  rng: DeterministicRNG,
  rows: number,
  cols: number
): void {
  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      const cell = board[r][c];
      if (!cell.startsWith('GEM_')) continue;

      const color = cell.substring(4);
      const hasAdjacentSame =
        (r > 0 && board[r - 1][c] === cell) ||
        (r < rows - 1 && board[r + 1][c] === cell) ||
        (c > 0 && board[r][c - 1] === cell) ||
        (c < cols - 1 && board[r][c + 1] === cell);

      if (hasAdjacentSame) {
        const colors = GEM_TYPES.filter((g) => g !== color);
        const newColor = rng.pickOne(colors)!;
        board[r][c] = `GEM_${newColor}` as CellValue;
      }
    }
  }
}

function getValidObjectives(
  difficulty: Difficulty,
  rng: DeterministicRNG
): ObjectiveData[] {
  const objectives: ObjectiveData[] = [];
  const availableTypes: ObjectiveType[] = [
    ObjectiveType.COLLECT_COLOR,
    ObjectiveType.REACH_SCORE,
    ObjectiveType.REMOVE_BLOCKER,
    ObjectiveType.COLLECT_ITEM,
  ];

  const count = rng.nextInRange(1, 3);

  for (let i = 0; i < count; i++) {
    const type = rng.pickOne(availableTypes)!;
    const multiplier = DIFFICULTY_MULTIPLIERS[difficulty].goalTargetMultiplier;

    let objective: ObjectiveData;

    switch (type) {
      case ObjectiveType.COLLECT_COLOR: {
        const color = rng.pickOne(GEM_TYPES)!;
        objectives.push({
          type,
          description: `Collect ${color} gems`,
          target: Math.round(rng.nextInRange(15, 35) * multiplier),
          color,
        });
        break;
      }
      case ObjectiveType.REACH_SCORE:
        objectives.push({
          type,
          description: 'Reach the target score',
          target: Math.round(rng.nextInRange(5000, 15000) * multiplier),
        });
        break;
      case ObjectiveType.REMOVE_BLOCKER:
        objectives.push({
          type,
          description: 'Remove obstacles',
          target: Math.round(rng.nextInRange(3, 12) * multiplier),
        });
        break;
      case ObjectiveType.COLLECT_ITEM: {
        const items = ['key', 'coin', 'gem'];
        const itemId = rng.pickOne(items)!;
        objectives.push({
          type,
          description: `Collect ${itemId}s`,
          target: Math.round(rng.nextInRange(3, 8) * multiplier),
          itemId,
        });
        break;
      }
    }
  }

  return objectives;
}

function computeStarThresholds(
  moveLimit: number,
  objectives: ObjectiveData[],
  difficulty: Difficulty
): { star1: number; star2: number; star3: number } {
  const avgPointsPerMove = 80;
  const baseScore = moveLimit * avgPointsPerMove;
  const difficultyBonus = {
    [Difficulty.EASY]: 0.7,
    [Difficulty.NORMAL]: 1.0,
    [Difficulty.HARD]: 1.3,
    [Difficulty.SUPER_HARD]: 1.8,
  }[difficulty];

  const adjusted = Math.round(baseScore * difficultyBonus);
  return {
    star1: adjusted,
    star2: Math.round(adjusted * 1.5),
    star3: Math.round(adjusted * 2.2),
  };
}

function getWorldThematicName(levelNumber: number): string {
  if (levelNumber <= 100) return 'Tutorial Island';
  if (levelNumber <= 400) return 'Forest Realm';
  if (levelNumber <= 700) return 'Mountain Realm';
  return 'Castle Realm';
}

export function generateLevel(levelNumber: number): LevelData {
  const seed = getSeed(levelNumber);
  const rng = new DeterministicRNG(seed);
  rng.reset();

  const difficulty = getDifficulty(levelNumber);
  const moveLimit = computeMoveLimit(levelNumber);
  const spec = OBSTACLE_SPECS[difficulty];

  const board = createEmptyBoard(GRID_ROWS, GRID_COLS);

  placeObstacles(board, rng, spec, GRID_ROWS, GRID_COLS);
  fillGems(board, rng, GRID_ROWS, GRID_COLS);
  removeAdjacentSameColors(board, rng, GRID_ROWS, GRID_COLS);

  const objectives = getValidObjectives(difficulty, rng);

  const stars = computeStarThresholds(moveLimit, objectives, difficulty);

  const prefixes = ['', '', '', 'The', 'A', 'Mystic', 'Hidden', 'Ancient'];
  const prefix = prefixes[levelNumber % prefixes.length];
  const themedNames: Record<Difficulty, string[]> = {
    [Difficulty.EASY]: ['Fresh Start', 'First Steps', 'Gem Gathering', 'Simple Swap'],
    [Difficulty.NORMAL]: ['Clearing Skies', 'Gem Cascade', 'Blocked Path', 'Treasure Hunt'],
    [Difficulty.HARD]: ['Mountain Peak', 'Heavy Blocks', 'Color Storm', 'Tough Challenge'],
    [Difficulty.SUPER_HARD]: ['Boss Level', 'Final Push', 'Ultimate Test', 'Last Stand'],
  };

  const namePool = themedNames[difficulty];
  const realmName = getWorldThematicName(levelNumber);
  const title = prefix
    ? `${prefix} ${namePool[(levelNumber - 1) % namePool.length]}`
    : `${namePool[(levelNumber - 1) % namePool.length]} — ${realmName}`;

  const levelData: LevelData = {
    levelNumber,
    difficulty,
    title,
    description: `Difficulty: ${difficulty} | Realm: ${getWorldThematicName(levelNumber)}`,
    grid: { cols: GRID_COLS, rows: GRID_ROWS },
    moveLimit,
    scoreStar1: stars.star1,
    scoreStar2: stars.star2,
    scoreStar3: stars.star3,
    objectives,
    boardLayout: board,
    seed,
    version: LEVEL_SCHEMA_VERSION,
    rewards: {
      coins: difficulty === Difficulty.SUPER_HARD ? 30
              : difficulty === Difficulty.HARD ? 20
              : difficulty === Difficulty.EASY ? 5 : 10,
      stars: difficulty === Difficulty.SUPER_HARD ? 3
             : difficulty === Difficulty.HARD ? 2
             : 1,
      unlocks: levelNumber % 50 === 0
        ? [`world_${Math.floor(levelNumber / 50) + 1}`]
        : undefined,
    },
  };

  const result = LevelDataSchema.safeParse(levelData);
  if (!result.success) {
    console.error(`Level ${levelNumber} failed schema validation:`, result.error.format());
    throw new Error(`Level ${levelNumber} schema validation failed`);
  }

  return levelData;
}

export function generateAllLevels(): LevelData[] {
  const levels: LevelData[] = [];
  for (let i = 1; i <= TOTAL_LEVELS; i++) {
    levels.push(generateLevel(i));
  }
  return levels;
}

export function getLevelRange(levelNumber: number): string {
  const start = Math.floor((levelNumber - 1) / LEVELS_PER_FOLDER) * LEVELS_PER_FOLDER + 1;
  const end = Math.min(start + LEVELS_PER_FOLDER - 1, TOTAL_LEVELS);
  return `${String(start).padStart(4, '0')}-${String(end).padStart(4, '0')}`;
}
