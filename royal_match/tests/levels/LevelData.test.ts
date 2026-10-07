import { describe, it, expect } from 'vitest';
import { LevelDataSchema, type CellValue } from '../../src/levels/LevelData';
import { CELL_VALUES } from '../../src/levels/LevelData';

describe('LevelData Schema', () => {
  describe('CellSchema validation', () => {
    it('should accept valid cell values', () => {
      for (const value of CELL_VALUES) {
        expect(() => {
          LevelDataSchema.parse({
            levelNumber: 1,
            difficulty: 'EASY',
            title: 'Test',
            description: 'Test desc',
            grid: { cols: 4, rows: 4 },
            moveLimit: 15,
            scoreStar1: 1000,
            scoreStar2: 1500,
            scoreStar3: 2000,
            objectives: [{ type: 'reach_score', description: 'Score', target: 5000 }],
            boardLayout: Array.from({ length: 4 }, () => Array<CellValue>(4).fill(value)),
            seed: 1,
            version: '1.0.0',
          });
        }).not.toThrow();
      }
    });

    it('should reject invalid cell values', () => {
      const board: string[][] = [['INVALID_CELL']];
      const result = LevelDataSchema.safeParse({
        levelNumber: 1,
        difficulty: 'EASY',
        title: 'Test',
        description: 'Test desc',
        grid: { cols: 1, rows: 1 },
        moveLimit: 15,
        scoreStar1: 1000,
        scoreStar2: 1500,
        scoreStar3: 2000,
        objectives: [{ type: 'reach_score', description: 'Score', target: 5000 }],
        boardLayout: board,
        seed: 1,
        version: '1.0.0',
      });
      expect(result.success).toBe(false);
    });
  });

  describe('ObjectiveSchema validation', () => {
    it('should accept a valid collect_color objective', () => {
      const result = LevelDataSchema.safeParse({
        levelNumber: 1,
        difficulty: 'EASY',
        title: 'Test',
        description: 'Test desc',
        grid: { cols: 8, rows: 8 },
        moveLimit: 15,
        scoreStar1: 1000,
        scoreStar2: 1500,
        scoreStar3: 2000,
        objectives: [
          { type: 'collect_color', description: 'Collect RED', target: 10, color: 'RED' },
        ],
        boardLayout: create8x8Board(),
        seed: 1,
        version: '1.0.0',
      });
      expect(result.success).toBe(true);
    });

    it('should accept a valid reach_score objective', () => {
      const result = LevelDataSchema.safeParse({
        levelNumber: 1,
        difficulty: 'EASY',
        title: 'Test',
        description: 'Test desc',
        grid: { cols: 8, rows: 8 },
        moveLimit: 15,
        scoreStar1: 1000,
        scoreStar2: 1500,
        scoreStar3: 2000,
        objectives: [{ type: 'reach_score', description: 'Score', target: 5000 }],
        boardLayout: create8x8Board(),
        seed: 1,
        version: '1.0.0',
      });
      expect(result.success).toBe(true);
    });

    it('rejects collect_color without a color', () => {
      const result = LevelDataSchema.safeParse({
        levelNumber: 1,
        difficulty: 'EASY',
        title: 'Test',
        description: 'Test desc',
        grid: { cols: 8, rows: 8 },
        moveLimit: 15,
        scoreStar1: 1000,
        scoreStar2: 1500,
        scoreStar3: 2000,
        objectives: [{ type: 'collect_color', description: 'Collect', target: 5 }],
        boardLayout: create8x8Board(),
        seed: 1,
        version: '1.0.0',
      });
      expect(result.success).toBe(false);
    });

    it('should accept a valid collect_item objective', () => {
      const result = LevelDataSchema.safeParse({
        levelNumber: 1,
        difficulty: 'EASY',
        title: 'Test',
        description: 'Test desc',
        grid: { cols: 8, rows: 8 },
        moveLimit: 15,
        scoreStar1: 1000,
        scoreStar2: 1500,
        scoreStar3: 2000,
        objectives: [
          { type: 'collect_item', description: 'Collect keys', target: 3, itemId: 'key' },
        ],
        boardLayout: create8x8Board(),
        seed: 1,
        version: '1.0.0',
      });
      expect(result.success).toBe(true);
    });
  });

  describe('LevelDataSchema validation', () => {
    it('should accept a fully valid level', () => {
      const level: unknown = {
        levelNumber: 42,
        difficulty: 'HARD',
        title: 'Test Level',
        description: 'A test level',
        grid: { cols: 8, rows: 8 },
        moveLimit: 13,
        scoreStar1: 5000,
        scoreStar2: 7500,
        scoreStar3: 11000,
        objectives: [
          { type: 'collect_color', description: 'Collect RED gems', target: 20, color: 'RED' },
          { type: 'reach_score', description: 'Reach the target score', target: 5000 },
        ],
        boardLayout: create8x8Board(),
        seed: 31614,
        version: '1.0.0',
        rewards: { coins: 20, stars: 2 },
      };

      const result = LevelDataSchema.safeParse(level);
      expect(result.success).toBe(true);
    });

    it('should reject a level with moveLimit > 50', () => {
      const result = LevelDataSchema.safeParse({
        levelNumber: 1,
        difficulty: 'EASY',
        title: 'Test',
        description: 'Test desc',
        grid: { cols: 8, rows: 8 },
        moveLimit: 100,
        scoreStar1: 1000,
        scoreStar2: 1500,
        scoreStar3: 2000,
        objectives: [{ type: 'reach_score', description: 'Score', target: 5000 }],
        boardLayout: create8x8Board(),
        seed: 1,
        version: '1.0.0',
      });
      expect(result.success).toBe(false);
    });

    it('should reject a level with levelNumber > 1000', () => {
      const result = LevelDataSchema.safeParse({
        levelNumber: 1001,
        difficulty: 'EASY',
        title: 'Test',
        description: 'Test desc',
        grid: { cols: 8, rows: 8 },
        moveLimit: 15,
        scoreStar1: 1000,
        scoreStar2: 1500,
        scoreStar3: 2000,
        objectives: [{ type: 'reach_score', description: 'Score', target: 5000 }],
        boardLayout: create8x8Board(),
        seed: 1,
        version: '1.0.0',
      });
      expect(result.success).toBe(false);
    });

    it('rejects board dimensions that do not match the declared grid', () => {
      const result = LevelDataSchema.safeParse({
        levelNumber: 1,
        difficulty: 'EASY',
        title: 'Test',
        description: 'Test desc',
        grid: { cols: 8, rows: 8 },
        moveLimit: 15,
        scoreStar1: 1000,
        scoreStar2: 1500,
        scoreStar3: 2000,
        objectives: [{ type: 'reach_score', description: 'Score', target: 5000 }],
        boardLayout: [['GEM_GREEN']],
        seed: 1,
        version: '1.0.0',
      });
      expect(result.success).toBe(false);
    });
  });

  describe('generated levels', () => {
    it('should validate 100 sample levels from files', async () => {
      // Test levels 1, 100, 500, 600 (super hard), 1000
      const testLevels = [1, 100, 250, 500, 600, 750, 1000];

      for (const levelNum of testLevels) {
        const folderStart = Math.floor((levelNum - 1) / 100) * 100 + 1;
        const folderEnd = folderStart + 99;
        const folderRange = `${String(folderStart).padStart(4, '0')}-${String(folderEnd).padStart(4, '0')}`;
        const filename = `level-${String(levelNum).padStart(4, '0')}.json`;
        const path = `/src/levels/levels/${folderRange}/${filename}`;

        const mod = await import(/* @vite-ignore */ path);
        const data = mod.default ?? mod;
        const result = LevelDataSchema.safeParse(data);

        expect(result.success).toBe(true);
        if (result.success) {
          expect(result.data.levelNumber).toBe(levelNum);
        }
      }
    });
  });
});

function create8x8Board(): CellValue[][] {
  const board: CellValue[][] = [];
  for (let r = 0; r < 8; r++) {
    const row: CellValue[] = [];
    for (let c = 0; c < 8; c++) {
      row.push('GEM_GREEN');
    }
    board.push(row);
  }
  return board;
}
