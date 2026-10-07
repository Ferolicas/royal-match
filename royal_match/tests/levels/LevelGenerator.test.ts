import { describe, it, expect } from 'vitest';
import { generateLevel } from '../../src/levels/LevelGenerator';
import { getDifficulty, computeMoveLimit, getSeed } from '../../src/levels/Difficulty';
import { Difficulty } from '../../src/types/Difficulty';
import { LevelDataSchema } from '../../src/levels/LevelData';
import { DeterministicRNG } from '../../src/utils/rng';

describe('LevelGenerator', () => {
  describe('getDifficulty', () => {
    it('should return EASY for levels 1 and 2', () => {
      expect(getDifficulty(1)).toBe(Difficulty.EASY);
      expect(getDifficulty(2)).toBe(Difficulty.EASY);
    });

    it('should return SUPER_HARD for every 6th level', () => {
      expect(getDifficulty(6)).toBe(Difficulty.SUPER_HARD);
      expect(getDifficulty(12)).toBe(Difficulty.SUPER_HARD);
      expect(getDifficulty(18)).toBe(Difficulty.SUPER_HARD);
    });

    it('should return HARD for every 3rd level (not 6th)', () => {
      expect(getDifficulty(3)).toBe(Difficulty.HARD);
      expect(getDifficulty(9)).toBe(Difficulty.HARD);
      expect(getDifficulty(15)).toBe(Difficulty.HARD);
    });

    it('should return NORMAL for other levels', () => {
      expect(getDifficulty(4)).toBe(Difficulty.NORMAL);
      expect(getDifficulty(5)).toBe(Difficulty.NORMAL);
      expect(getDifficulty(100)).toBe(Difficulty.NORMAL);
    });
  });

  describe('computeMoveLimit', () => {
    it('should add 3 moves for EASY levels', () => {
      expect(computeMoveLimit(1)).toBe(18); // 15 + 3
      expect(computeMoveLimit(2)).toBe(18);
    });

    it('should subtract 2 moves for HARD levels', () => {
      expect(computeMoveLimit(3)).toBe(13); // 15 - 2
    });

    it('should subtract 4 moves for SUPER_HARD levels', () => {
      expect(computeMoveLimit(6)).toBe(11); // 15 - 4
    });

    it('should not go below MIN_MOVE_LIMIT', () => {
      expect(computeMoveLimit(996)).toBe(11); // 15 - 4 = 11, still above min
    });
  });

  describe('getSeed', () => {
    it('should produce deterministic seeds', () => {
      const seed1 = getSeed(1);
      const seed2 = getSeed(1);
      expect(seed1).toBe(seed2);
    });

    it('should produce different seeds for different levels', () => {
      expect(getSeed(1)).not.toBe(getSeed(2));
    });
  });

  describe('generateLevel', () => {
    it('should produce valid level data for level 1', () => {
      const level = generateLevel(1);
      const result = LevelDataSchema.safeParse(level);
      expect(result.success).toBe(true);
    });

    it('should produce valid level data for level 100', () => {
      const level = generateLevel(100);
      const result = LevelDataSchema.safeParse(level);
      expect(result.success).toBe(true);
    });

    it('should produce valid level data for a SUPER_HARD level', () => {
      const level = generateLevel(600);
      const result = LevelDataSchema.safeParse(level);
      expect(result.success).toBe(true);
    });

    it('should produce deterministic output for the same level', () => {
      const level1 = generateLevel(42);
      const level2 = generateLevel(42);

      expect(level1).toEqual(level2);
    });

    it('should have the correct level number', () => {
      const level = generateLevel(7);
      expect(level.levelNumber).toBe(7);
    });

    it('should have correct difficulty', () => {
      expect(generateLevel(1).difficulty).toBe(Difficulty.EASY);
      expect(generateLevel(6).difficulty).toBe(Difficulty.SUPER_HARD);
      expect(generateLevel(9).difficulty).toBe(Difficulty.HARD);
      expect(generateLevel(10).difficulty).toBe(Difficulty.NORMAL);
    });

    it('should have 8x8 grid', () => {
      const level = generateLevel(1);
      expect(level.grid.rows).toBe(8);
      expect(level.grid.cols).toBe(8);
    });

    it('should have 1-5 objectives', () => {
      for (let i = 1; i <= 10; i++) {
        const level = generateLevel(i);
        expect(level.objectives.length).toBeGreaterThanOrEqual(1);
        expect(level.objectives.length).toBeLessThanOrEqual(5);
      }
    });

    it('should have valid board layout dimensions', () => {
      const level = generateLevel(50);
      expect(level.boardLayout.length).toBe(8);
      for (const row of level.boardLayout) {
        expect(row.length).toBe(8);
      }
    });

    it('should have non-empty star thresholds', () => {
      const level = generateLevel(1);
      expect(level.scoreStar1).toBeGreaterThan(0);
      expect(level.scoreStar2).toBeGreaterThan(level.scoreStar1);
      expect(level.scoreStar3).toBeGreaterThan(level.scoreStar2);
    });

    it('should have rewards', () => {
      const level = generateLevel(1);
      expect(level.rewards).toBeDefined();
      expect(level.rewards!.coins).toBeGreaterThanOrEqual(0);
      expect(level.rewards!.stars).toBeGreaterThanOrEqual(1);
    });
  });

  describe('deterministic generation', () => {
    it('should generate different content for different levels', () => {
      const level1 = generateLevel(1);
      const level2 = generateLevel(2);
      expect(level1.boardLayout).not.toEqual(level2.boardLayout);
    });

    it('should be reproducible with the same RNG seed', () => {
      const rng1 = new DeterministicRNG(getSeed(50));
      const rng2 = new DeterministicRNG(getSeed(50));
      expect(rng1.next()).toBe(rng2.next());
      expect(rng1.next()).toBe(rng2.next());
    });
  });
});
