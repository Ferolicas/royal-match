import { Difficulty } from '../types/Difficulty';

/** Total levels in the campaign. */
export const TOTAL_LEVELS = 1000;

/** Move limit before difficulty adjustment. */
export const BASE_MOVE_LIMIT = 15;
export const MIN_MOVE_LIMIT = 6;

/** Levels to group in a single folder (for file organization). */
export const LEVELS_PER_FOLDER = 100;

/** Seed base constant. Level N seed = SEED_BASE + N. */
export const SEED_BASE = 0x7a4e;

/** Goal target multipliers by difficulty. */
export const GOAL_TARGET_MULTIPLIERS: Record<Difficulty, number> = {
  [Difficulty.EASY]: 0.7,
  [Difficulty.NORMAL]: 1.0,
  [Difficulty.HARD]: 1.4,
  [Difficulty.SUPER_HARD]: 2.0,
};

/** Move budget offsets by difficulty. */
export const MOVE_BUDGET_OFFSETS: Record<Difficulty, number> = {
  [Difficulty.EASY]: +3,
  [Difficulty.NORMAL]: 0,
  [Difficulty.HARD]: -2,
  [Difficulty.SUPER_HARD]: -4,
};

/** Star threshold multipliers relative to base score. */
export const STAR_MULTIPLIERS = {
  star1: 1.0,
  star2: 1.5,
  star3: 2.2,
} as const;
