import { describe, it, expect } from 'vitest';
import { applyGravity, fillFromTop, type DropConfig } from '../../src/game/DropSystem';

const CONFIG: DropConfig = { cols: 8, rows: 8 };

describe('DropSystem', () => {
  describe('applyGravity', () => {
    it('should drop pieces to fill empty spaces below', () => {
      const grid: (string | null)[][] = [
        ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H'],
        [null, 'B', 'C', 'D', 'E', 'F', 'G', 'H'],
        ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H'],
        ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H'],
        ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H'],
        ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H'],
        ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H'],
        ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H'],
      ];

      const moved = applyGravity(grid, CONFIG);

      // 'A' from row 0 in column 0 should fall to the gap at row 1 (the bottom-most available)
      // Wait, gravity works from bottom up. Let's trace:
      // Column 0: row 7 has 'A', row 6 has 'A', ... row 2 has 'A', row 1 is null, row 0 has 'A'
      // writeRow starts at 7
      // Row 7: 'A' -> matches writeRow (7). writeRow-- → 6
      // Row 6: 'A' -> matches writeRow (6). writeRow-- → 5
      // ... all the way down to row 2: 'A' -> matches writeRow (2). writeRow-- → 1
      // Row 1: null -> skip
      // Row 0: 'A' -> writeRow=1, writeRow !== 0. Move from 0 to 1. writeRow-- → 0
      // So grid[1][0] = 'A', grid[0][0] = null
      expect(grid[0][0]).toBeNull();
      expect(grid[1][0]).toBe('A');
      expect(moved.length).toBe(1);
    });

    it('should handle multiple stacked empty cells', () => {
      const grid: (string | null)[][] = [
        ['A', 'A'],
        [null, 'A'],
        ['A', null],
        ['A', 'A'],
      ];

      const moved = applyGravity(grid, { cols: 2, rows: 4 });

      // Column 0: row 0='A', row 1=null, row 2='A', row 3='A'
      // After gravity: the 'A' at row 0 falls to row 1 (the null space)
      expect(grid[1][0]).toBe('A');
      expect(grid[0][0]).toBeNull();

      // Column 1: row 0='A', row 1='A', row 2=null, row 3='A'
      // No gaps below 'A' at row 0 and 1 (row 3 has 'A')
      // Row 2 is null — but there's nothing above row 2 that can fall
      // So no change for column 1
      expect(moved.length).toBeGreaterThanOrEqual(1);
    });

    it('should not move pieces when there are no gaps', () => {
      const grid: (string | null)[][] = [
        ['A', 'B', 'C'],
        ['D', 'E', 'F'],
      ];

      const moved = applyGravity(grid, { cols: 3, rows: 2 });
      expect(moved.length).toBe(0);
      expect(grid[0][0]).toBe('A');
      expect(grid[1][2]).toBe('F');
    });

    it('should handle all-empty column', () => {
      const grid: (string | null)[][] = [
        [null, 'A'],
        [null, 'A'],
      ];

      const moved = applyGravity(grid, { cols: 2, rows: 2 });
      expect(moved.length).toBe(0);
    });
  });

  describe('fillFromTop', () => {
    it('should fill empty cells with new gems from top', () => {
      const grid: (string | null)[][] = [
        ['A', null],
        ['B', 'A'],
      ];
      const colors = ['RED', 'BLUE', 'GREEN'];

      const spawned = fillFromTop(grid, colors, { cols: 2, rows: 2 });

      expect(spawned.length).toBe(1);
      expect(grid[0][1]).not.toBeNull();
      // Color should cycle through: first color is 'RED'
      expect(grid[0][1]).toBe('RED');
    });

    it('should fill multiple empty cells with cycling colors', () => {
      const grid: (string | null)[][] = [
        [null, null],
        [null, null],
      ];
      const colors = ['GREEN', 'BLUE'];

      const spawned = fillFromTop(grid, colors, { cols: 2, rows: 2 });

      expect(spawned.length).toBe(4);
      // Order: bottom-to-top, left-to-right
      // Column 0: row 1 → GREEN (index 0), row 0 → BLUE (index 1)
      // Column 1: row 1 → GREEN (index 2 % 2 = 0), row 0 → BLUE (index 3 % 2 = 1)
      expect(grid[1][0]).toBe('GREEN');
      expect(grid[0][0]).toBe('BLUE');
      expect(grid[1][1]).toBe('GREEN');
      expect(grid[0][1]).toBe('BLUE');
    });

    it('should not fill non-empty cells', () => {
      const grid: (string | null)[][] = [
        ['A', 'B'],
        ['C', 'D'],
      ];
      const colors = ['RED'];

      const spawned = fillFromTop(grid, colors, { cols: 2, rows: 2 });
      expect(spawned.length).toBe(0);
    });
  });
});
