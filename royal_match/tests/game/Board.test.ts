import { describe, it, expect } from 'vitest';
import { Board, type BoardConfig } from '../../src/game/Board';
import { PieceType } from '../../src/types/PieceType';
import { CellContent } from '../../src/types/GameState';
import type { CellValue } from '../../src/levels/LevelData';

const TEST_CONFIG: BoardConfig = {
  rows: 8,
  cols: 8,
  gemTypes: 6,
  minMatch: 3,
};

function createCleanLayout(): CellValue[][] {
  const layout: CellValue[][] = [];
  for (let r = 0; r < 8; r++) {
    const row: CellValue[] = [];
    for (let c = 0; c < 8; c++) {
      if ((r + c) % 2 === 0) row.push('GEM_BLUE');
      else row.push('GEM_GREEN');
    }
    layout.push(row);
  }
  return layout;
}

function createHorizontalMatchLayout(): CellValue[][] {
  const layout: CellValue[][] = [];
  for (let r = 0; r < 8; r++) {
    const row: CellValue[] = [];
    for (let c = 0; c < 8; c++) {
      if (r === 0 && (c === 0 || c === 1 || c === 2)) {
        row.push('GEM_RED');
      } else if ((r + c) % 2 === 0) {
        row.push('GEM_BLUE');
      } else {
        row.push('GEM_GREEN');
      }
    }
    layout.push(row);
  }
  return layout;
}

function createVerticalMatchLayout(): CellValue[][] {
  const layout: CellValue[][] = [];
  for (let r = 0; r < 8; r++) {
    const row: CellValue[] = [];
    for (let c = 0; c < 8; c++) {
      if (c === 0 && (r === 0 || r === 1 || r === 2)) {
        row.push('GEM_RED');
      } else if ((r + c) % 2 === 0) {
        row.push('GEM_BLUE');
      } else {
        row.push('GEM_GREEN');
      }
    }
    layout.push(row);
  }
  return layout;
}

describe('Board', () => {
  describe('initialization', () => {
    it('should create an empty board with correct dimensions', () => {
      const board = new Board(TEST_CONFIG);
      for (let r = 0; r < 8; r++) {
        for (let c = 0; c < 8; c++) {
          expect(board.getPiece(r, c)).toBeNull();
        }
      }
    });

    it('should initialize from a layout', () => {
      const board = new Board(TEST_CONFIG);
      const layout = createHorizontalMatchLayout();
      board.initializeFromLayout(layout);

      const piece = board.getPiece(0, 0);
      expect(piece).not.toBeNull();
      expect(piece!.color).toBe(PieceType.RED);
    });
  });

  describe('swapping', () => {
    it('should swap two pieces on the board', () => {
      const board = new Board(TEST_CONFIG);
      const layout = createCleanLayout();
      board.initializeFromLayout(layout);

      board.swapPieces(0, 0, 0, 1);

      const piece1 = board.getPiece(0, 0);
      const piece2 = board.getPiece(0, 1);
      expect(piece1!.color).toBe(PieceType.GREEN);
      expect(piece2!.color).toBe(PieceType.BLUE);
    });

    it('should update piece positions after swap', () => {
      const board = new Board(TEST_CONFIG);
      const layout = createCleanLayout();
      board.initializeFromLayout(layout);

      board.swapPieces(0, 0, 0, 1);

      expect(board.getPiece(0, 0)!.row).toBe(0);
      expect(board.getPiece(0, 0)!.col).toBe(0);
      expect(board.getPiece(0, 1)!.row).toBe(0);
      expect(board.getPiece(0, 1)!.col).toBe(1);
    });
  });

  describe('match finding', () => {
    it('should find a horizontal match of 3', () => {
      const board = new Board(TEST_CONFIG);
      const layout = createHorizontalMatchLayout();
      board.initializeFromLayout(layout);

      const result = board.findMatches();
      expect(result.matches.length).toBeGreaterThan(0);
      const match = result.matches.find((m) => m.direction === 'horizontal');
      expect(match).toBeDefined();
      expect(match!.length).toBe(3);
    });

    it('should find a vertical match of 3', () => {
      const board = new Board(TEST_CONFIG);
      const layout = createVerticalMatchLayout();
      board.initializeFromLayout(layout);

      const result = board.findMatches();
      const match = result.matches.find((m) => m.direction === 'vertical');
      expect(match).toBeDefined();
      expect(match!.length).toBe(3);
    });

    it('should find no matches on a clean board', () => {
      const board = new Board(TEST_CONFIG);
      const layout = createCleanLayout();
      board.initializeFromLayout(layout);

      const result = board.findMatches();
      expect(result.matches.length).toBe(0);
    });
  });

  describe('removing matches', () => {
    it('should remove matched pieces', () => {
      const board = new Board(TEST_CONFIG);
      const layout = createHorizontalMatchLayout();
      board.initializeFromLayout(layout);

      const { matches } = board.findMatches();
      const destroyed = board.removeMatches(matches);
      expect(destroyed.length).toBe(3);
      expect(board.getPiece(0, 0)).toBeNull();
      expect(board.getPiece(0, 1)).toBeNull();
      expect(board.getPiece(0, 2)).toBeNull();
    });
  });

  describe('gravity', () => {
    it('should not report movement when only the top row is empty', () => {
      const board = new Board(TEST_CONFIG);
      const layout = createHorizontalMatchLayout();
      board.initializeFromLayout(layout);

      const { matches } = board.findMatches();
      board.removeMatches(matches);

      const moved = board.applyGravity();
      expect(moved).toHaveLength(0);
    });

    it('should drop pieces into playable gaps without crossing obstacles', () => {
      const board = new Board(TEST_CONFIG);
      const layout = createCleanLayout();
      layout[2][0] = 'OBSTACLE_BOX';
      board.initializeFromLayout(layout);
      board.clearPositions([{ row: 3, col: 0 }, { row: 1, col: 1 }]);

      const moved = board.applyGravity();

      expect(board.getPiece(3, 0)).toBeNull();
      expect(board.grid.getCell(2, 0)?.content).toBe(CellContent.OBSTACLE_BOX);
      expect(board.getPiece(1, 1)).not.toBeNull();
      expect(moved.some((point) => point.row === 1 && point.col === 1)).toBe(true);
    });
  });

  describe('fillFromTop', () => {
    it('should spawn new pieces in empty cells', () => {
      const board = new Board(TEST_CONFIG);
      const layout = createCleanLayout();
      board.initializeFromLayout(layout);

      // Remove a piece
      board.pieces[3][3] = null;
      board.grid.setCell(3, 3, { content: CellContent.EMPTY });

      const spawned = board.fillFromTop();
      expect(spawned.length).toBe(1);
      expect(board.getPiece(3, 3)).not.toBeNull();
    });
  });

  describe('cloning', () => {
    it('should create an independent clone', () => {
      const board = new Board(TEST_CONFIG);
      const layout = createHorizontalMatchLayout();
      board.initializeFromLayout(layout);

      const clone = board.clone();
      clone.swapPieces(0, 0, 0, 1);

      // Original should be unchanged
      const originalPiece = board.getPiece(0, 0);
      expect(originalPiece!.color).toBe(PieceType.RED);
    });
  });
});
