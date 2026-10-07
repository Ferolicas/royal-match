import { useEffect, useCallback } from 'react';
import { useGameStore } from '../store';
import type { GameStateData } from '../../game/GameState';
import { GameStatus } from '../../game/GameState';
import type { Difficulty } from '../../types/Difficulty';
import type { ObjectiveData } from '../../levels/LevelData';
import type { AnimationCommand } from '../../types/GameState';

/**
 * Hook for subscribing to individual pieces of game state.
 * Uses Zustand's selective subscriptions for performance.
 */
export function useGameState() {
  const state = useGameStore((s) => ({
    levelNumber: s.levelNumber,
    difficulty: s.difficulty,
    status: s.status,
    moveLimit: s.moveLimit,
    movesRemaining: s.movesRemaining,
    score: s.score,
    objectives: s.objectives,
    star1: s.star1,
    star2: s.star2,
    star3: s.star3,
    gridCols: s.gridCols,
    gridRows: s.gridRows,
    isAnimating: s.isAnimating,
    isLevelLoading: s.isLevelLoading,
    currentLevelNumber: s.currentLevelNumber,
  }));

  return state;
}

/**
 * Hook for accessing the game manager and dispatching actions.
 */
export function useGameActions() {
  const trySwap = useGameStore((s) => s.trySwap);
  const loadLevel = useGameStore((s) => s.loadLevel);
  const restartLevel = useGameStore((s) => s.restartLevel);
  const resolveTurn = useGameStore((s) => s.resolveCurrentTurn);
  const getAnimationSteps = useGameStore((s) => s.getAnimationSteps);
  const clearAnimationSteps = useGameStore((s) => s.clearAnimationSteps);

  return {
    trySwap,
    loadLevel,
    restartLevel,
    resolveTurn,
    getAnimationSteps,
    clearAnimationSteps,
  };
}

/**
 * Hook for the full game state object (useful for rendering).
 */
export function useFullGameState(): GameStateData | null {
  return useGameStore((s) => s.gameManager)?.getState() ?? null;
}

/**
 * Hook to check if the game is in a specific status.
 */
export function useGameStatus(): GameStatus {
  return useGameStore((s) => s.status);
}

/**
 * Hook for game over / win state.
 */
export function useGameEndState(): { isWin: boolean; isLose: boolean; stars: number } {
  const status = useGameStatus();
  const score = useGameStore((s) => s.score);
  const star1 = useGameStore((s) => s.star1);
  const star2 = useGameStore((s) => s.star2);
  const star3 = useGameStore((s) => s.star3);

  let stars = 0;
  if (score >= star3) stars = 3;
  else if (score >= star2) stars = 2;
  else if (score >= star1) stars = 1;

  return {
    isWin: status === GameStatus.WIN,
    isLose: status === GameStatus.LOSE,
    stars,
  };
}

/**
 * Hook for animation steps. Returns the current list of queued animations.
 */
export function useAnimationSteps(): { command: AnimationCommand; duration: number }[] {
  return useGameStore((s) => s.getAnimationSteps());
}

/**
 * Hook to determine if a level is unlocked.
 * Level 1 is always unlocked. Level N is unlocked if (N-1) was completed with >= 1 star.
 */
export function useLevelProgress(): Record<number, number> {
  // In a real app, this would come from localStorage or a backend.
  // For now, we simulate progress with level 1 unlocked.
  return { 1: 1 };
}

export function useDifficulty(): Difficulty {
  return useGameStore((s) => s.difficulty);
}

export function useObjectives(): ObjectiveData[] {
  return useGameStore((s) => s.objectives) as unknown as ObjectiveData[];
}
