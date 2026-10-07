import type { Match } from '../types/GameState';
import { Difficulty } from '../types/Difficulty';
import type { ObjectiveData, RuntimeObjective, CellValue } from '../levels/LevelData';

export enum GameStatus {
  IDLE = 'IDLE',
  PLAYING = 'PLAYING',
  WAITING_INPUT = 'WAITING_INPUT',
  RESOLVING = 'RESOLVING',
  WIN = 'WIN',
  LOSE = 'LOSE',
  PAUSED = 'PAUSED',
}

export interface GameStateData {
  levelNumber: number;
  difficulty: Difficulty;
  status: GameStatus;
  moveLimit: number;
  movesRemaining: number;
  score: number;
  objectives: RuntimeObjective[];
  star1: number;
  star2: number;
  star3: number;
  boardLayout: CellValue[][];
  gridCols: number;
  gridRows: number;
  lastMatches: Match[];
  scoreAdded: number;
  isAnimating: boolean;
}

export class GameState {
  static create(levelData: {
    levelNumber: number;
    difficulty: Difficulty;
    moveLimit: number;
    scoreStar1: number;
    scoreStar2: number;
    scoreStar3: number;
    objectives: ObjectiveData[];
    boardLayout: CellValue[][];
    grid: { cols: number; rows: number };
  }): GameStateData {
    return {
      levelNumber: levelData.levelNumber,
      difficulty: levelData.difficulty,
      status: GameStatus.WAITING_INPUT,
      moveLimit: levelData.moveLimit,
      movesRemaining: levelData.moveLimit,
      score: 0,
      objectives: levelData.objectives.map((o) => ({ ...o, current: 0 })),
      star1: levelData.scoreStar1,
      star2: levelData.scoreStar2,
      star3: levelData.scoreStar3,
      boardLayout: levelData.boardLayout,
      gridCols: levelData.grid.cols,
      gridRows: levelData.grid.rows,
      lastMatches: [],
      scoreAdded: 0,
      isAnimating: false,
    };
  }

  static getStarsEarned(state: GameStateData): number {
    if (state.score >= state.star3) return 3;
    if (state.score >= state.star2) return 2;
    if (state.score >= state.star1) return 1;
    return state.status === GameStatus.WIN ? 1 : 0;
  }

  static isLevelComplete(state: GameStateData): boolean {
    return state.objectives.every((obj) => obj.current >= obj.target);
  }
}
