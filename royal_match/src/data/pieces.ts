import type { GemColor } from './colors';
import type { SpecialType } from '../types/SpecialType';

export interface PieceDefinition {
  readonly type: 'gem';
  readonly color: GemColor;
  readonly baseScore: number;
  readonly specialScore: number;
}

export const PIECE_DEFINITIONS: Record<GemColor, PieceDefinition> = {
  RED: { type: 'gem', color: 'RED', baseScore: 60, specialScore: 250 },
  BLUE: { type: 'gem', color: 'BLUE', baseScore: 60, specialScore: 180 },
  GREEN: { type: 'gem', color: 'GREEN', baseScore: 60, specialScore: 120 },
  YELLOW: { type: 'gem', color: 'YELLOW', baseScore: 60, specialScore: 200 },
  PURPLE: { type: 'gem', color: 'PURPLE', baseScore: 60, specialScore: 180 },
  CYAN: { type: 'gem', color: 'CYAN', baseScore: 60, specialScore: 250 },
};

export const SPECIAL_PIECE_DEFS = {
  LINE: { name: 'Line Clear', score: 120, colorEffect: 'white' },
  BOMB: { name: 'Bomb', score: 180, radius: 1 },
  COLOR_BOMB: { name: 'Color Bomb', score: 250 },
  ROCKET: { name: 'Rocket', score: 200 },
} as const;

export const GEM_TYPES: GemColor[] = ['RED', 'BLUE', 'GREEN', 'YELLOW', 'PURPLE', 'CYAN'];
