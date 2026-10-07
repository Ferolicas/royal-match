import type { GemColor } from './colors';

/** Base score per gem matched (before cascade multiplier). */
export const CELL_MATCH_SCORES: Map<GemColor, number> = new Map([
  ['RED', 60],
  ['BLUE', 60],
  ['GREEN', 60],
  ['YELLOW', 60],
  ['PURPLE', 60],
  ['CYAN', 60],
]);

export interface BoardConfigData {
  rows: number;
  cols: number;
  gemTypes: number;
  minMatch: number;
  baseMatchScore: number;
  cascadeMultiplier: boolean;
}

export const BOARD_CONFIG: BoardConfigData = {
  rows: 8,
  cols: 8,
  gemTypes: 6,
  minMatch: 3,
  baseMatchScore: 60,
  cascadeMultiplier: true,
};

export const SWAP_ANIMATION_DURATION = 250;
export const MATCH_DESTROY_DURATION = 300;
export const GRAVITY_DURATION = 250;
export const SPAWN_DURATION = 250;
