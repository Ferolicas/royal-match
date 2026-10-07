import type { Match } from '../types/GameState';
import { SpecialType } from '../types/SpecialType';
import { CELL_MATCH_SCORES } from '../data/boardConfig';
import type { GemColor } from '../data/colors';

export interface ScoreResult {
  points: number;
  combo?: number;
  specialBonus: boolean;
}

export class Scoring {
  static calculateMatchScore(
    match: Match,
    cascadeNumber: number,
    specialsInvolved: boolean
  ): number {
    const baseScore = CELL_MATCH_SCORES.get(match.color as GemColor) ?? 60;
    let points = baseScore * match.length * cascadeNumber;

    if (specialsInvolved) {
      points = Math.round(points * 1.5);
    }

    return points;
  }

  static calculateCascadeScore(
    matches: Match[],
    cascadeNumber: number
  ): { points: number; specialsInvolved: boolean } {
    let totalPoints = 0;
    let specialsInvolved = false;

    for (const match of matches) {
      const score = Scoring.calculateMatchScore(match, cascadeNumber, false);
      totalPoints += score;
    }

    return { points: totalPoints, specialsInvolved };
  }

  static computeStarRating(
    score: number,
    star1: number,
    star2: number,
    star3: number
  ): 0 | 1 | 2 | 3 {
    if (score >= star3) return 3;
    if (score >= star2) return 2;
    if (score >= star1) return 1;
    return 0;
  }

  static getComboDescription(cascade: number): string {
    const combos = ['', 'Double!', 'Triple!', 'Quadruple!', 'Omega!'];
    return combos[Math.min(cascade - 1, 4)] || 'Mega!';
  }
}
