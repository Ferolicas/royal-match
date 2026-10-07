import { describe, it, expect } from 'vitest';
import { SpecialAbilities, type SpecialEffect } from '../../src/game/SpecialAbilities';
import { SpecialType } from '../../src/types/SpecialType';

function createBoard(size: number = 8): (string | null)[][] {
  const board: (string | null)[][] = [];
  for (let r = 0; r < size; r++) {
    const row: (string | null)[] = [];
    for (let c = 0; c < size; c++) {
      row.push(`GEM_RED`);
    }
    board.push(row);
  }
  return board;
}

describe('SpecialAbilities', () => {
  describe('resolveLineClear', () => {
    it('should clear a row when direction is horizontal', () => {
      const board = createBoard();
      const cleared = SpecialAbilities.resolveLineClear(board, { row: 3, col: 3 }, 'horizontal');

      expect(cleared.length).toBe(8); // Full row of 8
      // Check that all cells in row 3 are included
      for (let c = 0; c < 8; c++) {
        expect(cleared.some((p) => p.row === 3 && p.col === c)).toBe(true);
      }
    });

    it('should clear a column when direction is vertical', () => {
      const board = createBoard();
      const cleared = SpecialAbilities.resolveLineClear(board, { row: 3, col: 3 }, 'vertical');

      expect(cleared.length).toBe(8); // Full column of 8
      for (let r = 0; r < 8; r++) {
        expect(cleared.some((p) => p.row === r && p.col === 3)).toBe(true);
      }
    });

    it('should clear both row and column when direction is both', () => {
      const board = createBoard();
      const cleared = SpecialAbilities.resolveLineClear(board, { row: 3, col: 3 }, 'both');

      // Row (8) + Column (8) - 1 (center counted once) = 15
      expect(cleared.length).toBe(15);
    });
  });

  describe('resolveBomb', () => {
    it('should clear a 3x3 area', () => {
      const board = createBoard();
      const cleared = SpecialAbilities.resolveBomb(board, { row: 3, col: 3 }, 1);

      expect(cleared.length).toBe(9); // 3x3 area
    });

    it('should handle edge positions', () => {
      const board = createBoard();
      const cleared = SpecialAbilities.resolveBomb(board, { row: 0, col: 0 }, 1);

      // Only 4 cells in the corner 2x2
      expect(cleared.length).toBe(4);
    });
  });

  describe('resolveColorBomb', () => {
    it('should clear all gems of the specified color', () => {
      const board = createBoard(6);
      // Set some cells to a different color
      board[0][0] = 'GEM_BLUE';
      board[1][1] = 'GEM_BLUE';
      board[2][2] = 'GEM_GREEN';

      const cleared = SpecialAbilities.resolveColorBomb(board, 'RED');

      // 6*6 = 36 total, minus 2 blue and 1 green = 33 red
      expect(cleared.length).toBe(33);
    });
  });

  describe('resolveRocket', () => {
    it('should clear a row when horizontal', () => {
      const board = createBoard();
      const cleared = SpecialAbilities.resolveRocket(board, { row: 4, col: 2 }, 'horizontal');

      expect(cleared.length).toBe(8);
    });

    it('should clear a column when vertical', () => {
      const board = createBoard();
      const cleared = SpecialAbilities.resolveRocket(board, { row: 4, col: 2 }, 'vertical');

      expect(cleared.length).toBe(8);
    });
  });

  describe('matchToSpecial', () => {
    it('should return LINE for match of 4', () => {
      const result = SpecialAbilities.matchToSpecial({ length: 4, shape: 'line' });
      expect(result).toBe(SpecialType.LINE);
    });

    it('should return BOMB for match of 5 with T shape', () => {
      const result = SpecialAbilities.matchToSpecial({ length: 5, shape: 'T' });
      expect(result).toBe(SpecialType.BOMB);
    });

    it('should return COLOR_BOMB for match of 5 with L shape', () => {
      const result = SpecialAbilities.matchToSpecial({ length: 5, shape: 'L' });
      expect(result).toBe(SpecialType.COLOR_BOMB);
    });

    it('should return ROCKET for match of 6', () => {
      const result = SpecialAbilities.matchToSpecial({ length: 6, shape: 'line' });
      expect(result).toBe(SpecialType.ROCKET);
    });

    it('should return NONE for match of 3', () => {
      const result = SpecialAbilities.matchToSpecial({ length: 3, shape: 'line' });
      expect(result).toBe(SpecialType.NONE);
    });
  });

  describe('applySpecialEffect', () => {
    it('should dispatch to resolveLineClear', () => {
      const board = createBoard();
      const effect: SpecialEffect = {
        type: 'line_clear',
        position: { row: 3, col: 3 },
        direction: 'horizontal',
      };
      const cleared = SpecialAbilities.applySpecialEffect(board, effect);
      expect(cleared.length).toBe(8);
    });
  });
});
