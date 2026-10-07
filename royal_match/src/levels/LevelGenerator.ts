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
  voidCount: number;
}

const OBSTACLE_SPECS: Record<Difficulty, ObstacleSpec> = {
  [Difficulty.EASY]: { density: 0, types: ['BLOCKED'], blockerCount: 0, voidCount: 0 },
  [Difficulty.NORMAL]: {
    density: 0.08,
    types: ['OBSTACLE_BOX', 'OBSTACLE_CAGE'],
    blockerCount: 2,
    voidCount: 1,
  },
  [Difficulty.HARD]: {
    density: 0.18,
    types: ['OBSTACLE_BOX', 'OBSTACLE_CAGE', 'OBSTACLE_ICE'],
    blockerCount: 5,
    voidCount: 3,
  },
  [Difficulty.SUPER_HARD]: {
    density: 0.35,
    types: ['OBSTACLE_BOX', 'OBSTACLE_CAGE', 'OBSTACLE_ICE', 'OBSTACLE_STONE'],
    blockerCount: 8,
    voidCount: 6,
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

    }
  }

  const emptyCells: { row: number; col: number }[] = [];
  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      if (board[r][c] === 'EMPTY') emptyCells.push({ row: r, col: c });
    }
  }
  for (const { row, col } of rng.shuffle(emptyCells).slice(0, spec.voidCount)) {
    board[row][col] = 'VOID';
  }

  return { obstacleCells, blockedCount };
}

function hasMatchAt(board: CellValue[][], row: number, col: number): boolean {
  const value = board[row]?.[col];
  if (!value?.startsWith('GEM_')) return false;

  let horizontal = 1;
  for (let c = col - 1; c >= 0 && board[row][c] === value; c--) horizontal++;
  for (let c = col + 1; c < board[row].length && board[row][c] === value; c++) horizontal++;

  let vertical = 1;
  for (let r = row - 1; r >= 0 && board[r][col] === value; r--) vertical++;
  for (let r = row + 1; r < board.length && board[r][col] === value; r++) vertical++;

  return horizontal >= 3 || vertical >= 3;
}

function hasValidMove(board: CellValue[][]): boolean {
  const directions = [[1, 0], [0, 1]] as const;
  const rows = board.length;
  const cols = board[0]?.length ?? 0;
  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      if (!board[r][c].startsWith('GEM_')) continue;
      for (const [dr, dc] of directions) {
        const nr = r + dr;
        const nc = c + dc;
        if (!board[nr]?.[nc]?.startsWith('GEM_')) continue;
        [board[r][c], board[nr][nc]] = [board[nr][nc], board[r][c]];
        const createsMatch = hasMatchAt(board, r, c) || hasMatchAt(board, nr, nc);
        [board[r][c], board[nr][nc]] = [board[nr][nc], board[r][c]];
        if (createsMatch) return true;
      }
    }
  }
  return false;
}

function fillGems(board: CellValue[][], rng: DeterministicRNG, rows: number, cols: number): void {
  const gemCells: { row: number; col: number }[] = [];
  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      if (board[r][c] === 'EMPTY') gemCells.push({ row: r, col: c });
    }
  }

  for (let attempt = 0; attempt < 100; attempt++) {
    for (const { row, col } of gemCells) {
      const candidates = rng.shuffle([...GEM_TYPES]);
      const color = candidates.find((candidate) => {
        const value = `GEM_${candidate}`;
        return !(
          (col >= 2 && board[row][col - 1] === value && board[row][col - 2] === value) ||
          (row >= 2 && board[row - 1][col] === value && board[row - 2][col] === value)
        );
      }) ?? candidates[0];
      board[row][col] = `GEM_${color}` as CellValue;
    }
    if (hasValidMove(board)) return;
    for (const { row, col } of gemCells) board[row][col] = 'EMPTY';
  }

  throw new Error('Unable to generate a board with at least one valid move');
}

function getValidObjectives(
  levelNumber: number,
  difficulty: Difficulty,
  rng: DeterministicRNG,
  obstacleCount: number,
  moveLimit: number,
): ObjectiveData[] {
  if (levelNumber === 1) {
    return [{ type: ObjectiveType.REACH_SCORE, description: 'Reach the target score', target: 300 }];
  }
  if (levelNumber === 2) {
    return [{ type: ObjectiveType.COLLECT_COLOR, description: 'Collect RED gems', target: 6, color: 'RED' }];
  }

  const objectives: ObjectiveData[] = [];
  const availableTypes = rng.shuffle([
    ObjectiveType.COLLECT_COLOR,
    ObjectiveType.REACH_SCORE,
    ...(obstacleCount > 0 ? [ObjectiveType.REMOVE_BLOCKER] : []),
  ]);

  const desiredCount = difficulty === Difficulty.NORMAL ? 2 : 3;
  const count = Math.min(desiredCount, availableTypes.length);

  for (let i = 0; i < count; i++) {
    const type = availableTypes[i];
    const multiplier = DIFFICULTY_MULTIPLIERS[difficulty].goalTargetMultiplier;

    switch (type) {
      case ObjectiveType.COLLECT_COLOR: {
        const color = rng.pickOne(GEM_TYPES)!;
        objectives.push({
          type,
          description: `Collect ${color} gems`,
          target: Math.max(4, Math.round(rng.nextInRange(5, 9) * multiplier)),
          color,
        });
        break;
      }
      case ObjectiveType.REACH_SCORE:
        objectives.push({
          type,
          description: 'Reach the target score',
          target: Math.round(moveLimit * rng.nextInRange(35, 55) * multiplier),
        });
        break;
      case ObjectiveType.REMOVE_BLOCKER:
        objectives.push({
          type,
          description: 'Remove obstacles',
          target: rng.nextInRange(1, obstacleCount),
        });
        break;
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

  const { obstacleCells } = placeObstacles(board, rng, spec, GRID_ROWS, GRID_COLS);
  fillGems(board, rng, GRID_ROWS, GRID_COLS);

  const objectives = getValidObjectives(levelNumber, difficulty, rng, obstacleCells, moveLimit);

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
