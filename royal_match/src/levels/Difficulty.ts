import { Difficulty } from '../types/Difficulty';

export { Difficulty };
export { LEVEL_SCHEMA_VERSION } from './LevelData';

export const TOTAL_LEVELS = 1000;
export const SEED_BASE = 0x7a4e;
export const LEVELS_PER_FOLDER = 100;
export const GRID_COLS = 8;
export const GRID_ROWS = 8;
export const GEM_TYPES_COUNT = 6;

export const DIFFICULTY_MULTIPLIERS: Record<Difficulty, {
  moveBudget: number;
  goalTargetMultiplier: number;
  obstacleDensity: number;
  blockerCount: number;
}> = {
  [Difficulty.EASY]: {
    moveBudget: 3,
    goalTargetMultiplier: 0.7,
    obstacleDensity: 0,
    blockerCount: 0,
  },
  [Difficulty.NORMAL]: {
    moveBudget: 0,
    goalTargetMultiplier: 1.0,
    obstacleDensity: 0.08,
    blockerCount: 2,
  },
  [Difficulty.HARD]: {
    moveBudget: -2,
    goalTargetMultiplier: 1.4,
    obstacleDensity: 0.18,
    blockerCount: 5,
  },
  [Difficulty.SUPER_HARD]: {
    moveBudget: -4,
    goalTargetMultiplier: 2.0,
    obstacleDensity: 0.35,
    blockerCount: 8,
  },
};

export function getDifficulty(levelNumber: number): Difficulty {
  if (levelNumber <= 2) return Difficulty.EASY;
  if (levelNumber % 6 === 0) return Difficulty.SUPER_HARD;
  if (levelNumber % 3 === 0) return Difficulty.HARD;
  return Difficulty.NORMAL;
}

export function getSeed(levelNumber: number): number {
  return SEED_BASE + levelNumber;
}

export const BASE_MOVE_LIMIT = 15;
export const MIN_MOVE_LIMIT = 6;

export function computeMoveLimit(levelNumber: number): number {
  const diff = getDifficulty(levelNumber);
  const multiplier = DIFFICULTY_MULTIPLIERS[diff];
  const moves = BASE_MOVE_LIMIT + multiplier.moveBudget;
  return Math.max(MIN_MOVE_LIMIT, moves);
}

export function getFolderRange(levelNumber: number): string {
  const start = Math.floor((levelNumber - 1) / LEVELS_PER_FOLDER) * LEVELS_PER_FOLDER + 1;
  const end = start + LEVELS_PER_FOLDER - 1;
  return `${String(start).padStart(4, '0')}-${String(end).padStart(4, '0')}`;
}

export function getLevelFilename(levelNumber: number): string {
  return `level-${String(levelNumber).padStart(4, '0')}.json`;
}

export function getLevelPath(levelNumber: number): string {
  return `levels/${getFolderRange(levelNumber)}/${getLevelFilename(levelNumber)}`;
}
