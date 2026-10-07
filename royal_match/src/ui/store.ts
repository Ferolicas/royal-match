import { create } from 'zustand';
import { subscribeWithSelector } from 'zustand/middleware';
import type { GameStateData } from '../game/GameState';
import { GameStatus, GameState } from '../game/GameState';
import { GameManager } from '../game/GameManager';
import { LevelLoader } from '../levels/LevelLoader';
import { getDifficulty } from '../levels/Difficulty';
import type { AnimationCommand } from '../types/GameState';

export interface GameStore extends GameStateData {
  // Game manager reference
  gameManager: GameManager | null;
  levelLoader: LevelLoader | null;
  currentLevelNumber: number;
  isLevelLoading: boolean;

  // Actions
  loadLevel: (levelNumber: number) => Promise<void>;
  trySwap: (r1: number, c1: number, r2: number, c2: number) => boolean;
  resolveCurrentTurn: () => void;
  restartLevel: () => void;
  setAnimating: (animating: boolean) => void;
  getAnimationSteps: () => { command: AnimationCommand; duration: number }[];
  clearAnimationSteps: () => void;
}

// Level loader instance
const levelLoader = new LevelLoader();

export const useGameStore = create<GameStore>()(
  subscribeWithSelector((set, get) => ({
    // GameStateData defaults
    levelNumber: 0,
    difficulty: getDifficulty(1),
    status: GameStatus.IDLE,
    moveLimit: 0,
    movesRemaining: 0,
    score: 0,
    objectives: [],
    star1: 0,
    star2: 0,
    star3: 0,
    boardLayout: [],
    gridCols: 8,
    gridRows: 8,
    lastMatches: [],
    scoreAdded: 0,
    isAnimating: false,

    // Game manager
    gameManager: null,
    levelLoader: levelLoader,
    currentLevelNumber: 0,
    isLevelLoading: false,

    // Actions
    loadLevel: async (levelNumber: number) => {
      set({ isLevelLoading: true });

      if (!Number.isInteger(levelNumber) || levelNumber < 1 || levelNumber > 1000) {
        console.error(`Invalid level number: ${levelNumber}`);
        set({ isLevelLoading: false });
        return;
      }

      try {
        const levelData = await levelLoader.loadLevel(levelNumber);

        // Create game state from level data
        const state = GameState.create({
          levelNumber: levelData.levelNumber,
          difficulty: levelData.difficulty,
          moveLimit: levelData.moveLimit,
          scoreStar1: levelData.scoreStar1,
          scoreStar2: levelData.scoreStar2,
          scoreStar3: levelData.scoreStar3,
          objectives: levelData.objectives,
          boardLayout: levelData.boardLayout,
          grid: levelData.grid,
        });

        const manager = new GameManager(state);

        set({
          ...state,
          gameManager: manager,
          currentLevelNumber: levelNumber,
          isLevelLoading: false,
        });
      } catch (err) {
        console.error('Failed to load level:', err);
        set({ isLevelLoading: false });
      }
    },

    trySwap: (r1, c1, r2, c2) => {
      const manager = get().gameManager;
      if (!manager) return false;
      const result = manager.trySwap(r1, c1, r2, c2);
      if (result) {
        const state = manager.getState();
        set({
          ...state,
          lastMatches: state.lastMatches,
          scoreAdded: state.scoreAdded,
        });
      }
      return result;
    },

    resolveCurrentTurn: () => {
      const manager = get().gameManager;
      if (!manager) return;

      set({ isAnimating: true });

      setTimeout(() => {
        const newState = manager.getState();
        set({
          ...newState,
          isAnimating: false,
        });
      }, 1000);
    },

    restartLevel: () => {
      const levelNumber = get().currentLevelNumber;
      if (levelNumber > 0) {
        get().loadLevel(levelNumber);
      }
    },

    setAnimating: (animating: boolean) => {
      set({ isAnimating: animating });
    },

    getAnimationSteps: () => {
      const manager = get().gameManager;
      if (!manager) return [];
      return manager.getAnimationSteps();
    },

    clearAnimationSteps: () => {
      // Animation steps are managed by GameManager internally
    },
  }))
);

declare global {
  interface Window {
    __ROYAL_MATCH_STORE__?: typeof useGameStore;
  }
}

if (import.meta.env.DEV && typeof window !== 'undefined') {
  window.__ROYAL_MATCH_STORE__ = useGameStore;
}
