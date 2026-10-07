import { describe, expect, it } from 'vitest';
import { GameManager } from '../../src/game/GameManager';
import { GameState, GameStatus } from '../../src/game/GameState';
import { generateLevel } from '../../src/levels/LevelGenerator';
import type { Point } from '../../src/types/GameState';
import { SpecialType } from '../../src/types/SpecialType';

function findValidSwap(manager: GameManager): [Point, Point] {
  const board = manager.getBoard();
  for (let row = 0; row < board.config.rows; row++) {
    for (let col = 0; col < board.config.cols; col++) {
      for (const [dr, dc] of [[1, 0], [0, 1]] as const) {
        const next = { row: row + dr, col: col + dc };
        if (!board.getPiece(row, col) || !board.getPiece(next.row, next.col)) continue;
        board.swapPieces(row, col, next.row, next.col);
        const hasMatch = board.findMatches().matches.length > 0;
        board.swapPieces(row, col, next.row, next.col);
        if (hasMatch) return [{ row, col }, next];
      }
    }
  }
  throw new Error('Expected generated level to contain a valid swap');
}

function createManager(target: number, moves: number): GameManager {
  const level = generateLevel(1);
  const state = GameState.create({
    levelNumber: level.levelNumber,
    difficulty: level.difficulty,
    moveLimit: moves,
    scoreStar1: level.scoreStar1,
    scoreStar2: level.scoreStar2,
    scoreStar3: level.scoreStar3,
    objectives: [{ type: 'reach_score', description: 'Score', target }],
    boardLayout: level.boardLayout,
    grid: level.grid,
  });
  return new GameManager(state);
}

describe('GameManager end-to-end turns', () => {
  it('consumes one move, scores the match and reaches WIN', () => {
    const manager = createManager(1, 3);
    const [from, to] = findValidSwap(manager);

    expect(manager.trySwap(from.row, from.col, to.row, to.col)).toBe(true);
    expect(manager.getState().movesRemaining).toBe(2);
    expect(manager.getState().score).toBeGreaterThan(0);
    expect(manager.getState().status).toBe(GameStatus.WIN);
    expect(GameState.getStarsEarned(manager.getState())).toBeGreaterThanOrEqual(1);
  });

  it('reaches LOSE after the last move when the goal is unmet', () => {
    const manager = createManager(1_000_000, 1);
    const [from, to] = findValidSwap(manager);

    expect(manager.trySwap(from.row, from.col, to.row, to.col)).toBe(true);
    expect(manager.getState().movesRemaining).toBe(0);
    expect(manager.getState().status).toBe(GameStatus.LOSE);
  });

  it('rejects non-adjacent input without consuming a move', () => {
    const manager = createManager(1_000_000, 3);

    expect(manager.trySwap(0, 0, 2, 0)).toBe(false);
    expect(manager.getState().movesRemaining).toBe(3);
    expect(manager.getState().status).toBe(GameStatus.WAITING_INPUT);
  });

  it('activates a special when it is swapped without requiring a normal match', () => {
    const manager = createManager(1_000_000, 3);
    manager.getBoard().setSpecial(0, 0, SpecialType.BOMB);

    expect(manager.trySwap(0, 0, 0, 1)).toBe(true);
    expect(manager.getState().movesRemaining).toBe(2);
    expect(manager.getState().score).toBeGreaterThanOrEqual(180);
  });

  it('discards destroyed-cell render bookkeeping from earlier turns', () => {
    const manager = createManager(1_000_000, 3);
    manager.getBoard().destroyedThisTurn.push({ row: 99, col: 99 });
    const [from, to] = findValidSwap(manager);

    expect(manager.trySwap(from.row, from.col, to.row, to.col)).toBe(true);
    expect(manager.getBoard().destroyedThisTurn).not.toContainEqual({ row: 99, col: 99 });
  });
});
