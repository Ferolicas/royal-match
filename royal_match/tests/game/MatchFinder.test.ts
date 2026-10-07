import { describe, it, expect } from 'vitest';
import { MatchFinder } from '../../src/game/MatchFinder';

describe('MatchFinder', () => {
  describe('findMatches', () => {
    it('should find a horizontal match of 3', () => {
      const board = createCleanBoard(8, 8);
      board[0][0] = 'GEM_RED';
      board[0][1] = 'GEM_RED';
      board[0][2] = 'GEM_RED';

      const matches = MatchFinder.findMatches(board);
      expect(matches.length).toBe(1);
      expect(matches[0].direction).toBe('horizontal');
      expect(matches[0].length).toBe(3);
    });

    it('should find a vertical match of 3', () => {
      const board = createCleanBoard(8, 8);
      board[0][0] = 'GEM_BLUE';
      board[1][0] = 'GEM_BLUE';
      board[2][0] = 'GEM_BLUE';

      const matches = MatchFinder.findMatches(board);
      expect(matches.length).toBe(1);
      expect(matches[0].direction).toBe('vertical');
      expect(matches[0].length).toBe(3);
    });

    it('should find a match of 4 (special)', () => {
      const board = createCleanBoard(8, 8);
      board[0][0] = 'GEM_GREEN';
      board[0][1] = 'GEM_GREEN';
      board[0][2] = 'GEM_GREEN';
      board[0][3] = 'GEM_GREEN';

      const matches = MatchFinder.findMatches(board);
      expect(matches.length).toBe(1);
      expect(matches[0].length).toBe(4);
    });

    it('should find a match of 5', () => {
      const board = createCleanBoard(8, 8);
      board[0][0] = 'GEM_YELLOW';
      board[0][1] = 'GEM_YELLOW';
      board[0][2] = 'GEM_YELLOW';
      board[0][3] = 'GEM_YELLOW';
      board[0][4] = 'GEM_YELLOW';

      const matches = MatchFinder.findMatches(board);
      expect(matches.length).toBe(1);
      expect(matches[0].length).toBe(5);
    });

    it('should find a match of 6 (rocket)', () => {
      const board = createCleanBoard(8, 8);
      board[0][0] = 'GEM_CYAN';
      board[0][1] = 'GEM_CYAN';
      board[0][2] = 'GEM_CYAN';
      board[0][3] = 'GEM_CYAN';
      board[0][4] = 'GEM_CYAN';
      board[0][5] = 'GEM_CYAN';

      const matches = MatchFinder.findMatches(board);
      expect(matches.length).toBe(1);
      expect(matches[0].length).toBe(6);
    });

    it('should find multiple separate matches', () => {
      const board = createCleanBoard(8, 8);
      // Horizontal match
      board[0][0] = 'GEM_RED';
      board[0][1] = 'GEM_RED';
      board[0][2] = 'GEM_RED';
      // Vertical match
      board[3][0] = 'GEM_BLUE';
      board[4][0] = 'GEM_BLUE';
      board[5][0] = 'GEM_BLUE';

      const matches = MatchFinder.findMatches(board);
      expect(matches.length).toBe(2);
    });

    it('should find no matches on a clean board', () => {
      const board = createCleanBoard(8, 8);
      const matches = MatchFinder.findMatches(board);
      expect(matches.length).toBe(0);
    });

    it('should not match non-gem cells', () => {
      const board = [
        ['EMPTY', 'EMPTY', 'EMPTY'],
        ['EMPTY', 'EMPTY', 'EMPTY'],
        ['EMPTY', 'EMPTY', 'EMPTY'],
      ];

      const matches = MatchFinder.findMatches(board);
      expect(matches.length).toBe(0);
    });
  });

  describe('findMatchesWithSpecials', () => {
    it('should detect specials for matches of 4+', () => {
      const board = createCleanBoard(8, 8);
      board[0][0] = 'GEM_RED';
      board[0][1] = 'GEM_RED';
      board[0][2] = 'GEM_RED';
      board[0][3] = 'GEM_RED';

      const result = MatchFinder.findMatchesWithSpecials(board);
      expect(result.specialPieces.length).toBe(1);
    });

    it('classifies intersecting matches as a T shape instead of a straight five', () => {
      const board = createCleanBoard(8, 8);
      board[2][1] = 'GEM_RED';
      board[2][2] = 'GEM_RED';
      board[2][3] = 'GEM_RED';
      board[0][2] = 'GEM_RED';
      board[1][2] = 'GEM_RED';

      const result = MatchFinder.findMatchesWithSpecials(board);

      expect(result.matches).toHaveLength(2);
      expect(result.specialPieces).toEqual(expect.arrayContaining([
        expect.objectContaining({ position: { row: 2, col: 2 }, shape: 'T' }),
      ]));
    });

    it('classifies two intersecting endpoints as an L shape', () => {
      const board = createCleanBoard(8, 8);
      board[2][2] = 'GEM_RED';
      board[2][3] = 'GEM_RED';
      board[2][4] = 'GEM_RED';
      board[3][2] = 'GEM_RED';
      board[4][2] = 'GEM_RED';

      const result = MatchFinder.findMatchesWithSpecials(board);

      expect(result.specialPieces).toEqual(expect.arrayContaining([
        expect.objectContaining({ position: { row: 2, col: 2 }, shape: 'L' }),
      ]));
    });
  });

  describe('hasAnyMatch', () => {
    it('should return true when a match exists', () => {
      const board = createCleanBoard(8, 8);
      board[0][0] = 'GEM_RED';
      board[0][1] = 'GEM_RED';
      board[0][2] = 'GEM_RED';

      expect(MatchFinder.hasAnyMatch(board)).toBe(true);
    });

    it('should return false when no match exists', () => {
      const board = createCleanBoard(8, 8);
      expect(MatchFinder.hasAnyMatch(board)).toBe(false);
    });
  });

  describe('deduplication', () => {
    it('should not duplicate overlapping matches in the same row', () => {
      const board = createCleanBoard(8, 8);
      // Create a long match
      board[0][0] = 'GEM_RED';
      board[0][1] = 'GEM_RED';
      board[0][2] = 'GEM_RED';
      board[0][3] = 'GEM_RED';
      board[0][4] = 'GEM_RED';

      const matches = MatchFinder.findMatches(board);
      // Should find exactly 1 horizontal match of length 5, not multiple
      expect(matches.length).toBe(1);
      expect(matches[0].length).toBe(5);
    });
  });
});

function createCleanBoard(rows: number, cols: number): string[][] {
  const board: string[][] = [];
  for (let r = 0; r < rows; r++) {
    const row: string[] = [];
    for (let c = 0; c < cols; c++) {
      if ((r + c) % 2 === 0) {
        row.push('GEM_BLUE');
      } else {
        row.push('GEM_GREEN');
      }
    }
    board.push(row);
  }
  return board;
}
