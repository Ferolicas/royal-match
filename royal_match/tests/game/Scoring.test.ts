import { describe, expect, it } from 'vitest';
import { Scoring } from '../../src/game/Scoring';
import type { Match } from '../../src/types/GameState';

function match(length: number): Match {
  return {
    start: { row: 0, col: 0 },
    end: { row: 0, col: length - 1 },
    direction: 'horizontal',
    color: 'RED',
    length,
  };
}

describe('Scoring contract', () => {
  it.each([
    [3, 60],
    [4, 120],
    [5, 180],
    [6, 200],
  ])('scores a length-%i match as %i points', (length, expected) => {
    expect(Scoring.calculateMatchScore(match(length), 1, false)).toBe(expected);
  });

  it('applies the cascade multiplier once', () => {
    expect(Scoring.calculateCascadeScore([match(3)], 3).points).toBe(180);
  });
});
