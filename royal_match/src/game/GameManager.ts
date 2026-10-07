import type { Point, Match, AnimationCommand } from '../types/GameState';
import type { ObjectiveData } from '../levels/LevelData';
import { GameStatus, type GameStateData, GameState } from './GameState';
import { Board, type BoardConfig } from './Board';
import { MatchFinder } from './MatchFinder';
import { Scoring } from './Scoring';
import { SpecialType } from '../types/SpecialType';

export interface AnimationStep {
  command: AnimationCommand;
  duration: number;
}

export class GameManager {
  private state: GameStateData;
  private board: Board;
  private animationSteps: AnimationStep[];
  private animationIdCounter: number;

  constructor(state: GameStateData) {
    this.state = state;
    this.board = new Board({
      rows: state.gridRows,
      cols: state.gridCols,
    });
    this.animationSteps = [];
    this.animationIdCounter = 0;
    this.board.initializeFromLayout(state.boardLayout);
  }

  getState(): GameStateData {
    return this.state;
  }

  getBoard(): Board {
    return this.board;
  }

  getAnimationSteps(): AnimationStep[] {
    return this.animationSteps;
  }

  private nextAnimationId(): string {
    return `anim_${Date.now()}_${this.animationIdCounter++}`;
  }

  trySwap(r1: number, c1: number, r2: number, c2: number): boolean {
    if (this.state.status !== GameStatus.WAITING_INPUT) return false;

    const piece1 = this.board.getPiece(r1, c1);
    const piece2 = this.board.getPiece(r2, c2);
    if (!piece1 || !piece2) return false;

    const isAdjacent =
      (Math.abs(r1 - r2) === 1 && c1 === c2) ||
      (Math.abs(c1 - c2) === 1 && r1 === r2);
    if (!isAdjacent) return false;

    // Swap, check for matches
    this.board.swapPieces(r1, c1, r2, c2);

    const { matches, specials } = this.board.findMatches();

    if (matches.length === 0) {
      // Revert swap
      this.board.swapPieces(r1, c1, r2, c2);
      return false;
    }

    this.state.status = GameStatus.RESOLVING;
    this.state.movesRemaining--;
    this.state.lastMatches = matches;

    this.processMatches(matches, specials);

    return true;
  }

  private processMatches(
    matches: Match[],
    specials: ReturnType<typeof MatchFinder.findMatchesWithSpecials>['specialPieces'],
    cascadeNumber: number = 1
  ): void {
    if (cascadeNumber === 1) {
      this.animationSteps = [];
    }

    // Compute score for this cascade
    const scoreResult = Scoring.calculateCascadeScore(matches, cascadeNumber);
    const scoreGained = scoreResult.points;
    this.state.score += scoreGained;
    this.state.scoreAdded += scoreGained;

    // Remove matched pieces
    const destroyed = this.board.removeMatches(matches);

    this.animationSteps.push({
      command: {
        id: this.nextAnimationId(),
        type: 'DESTROY',
        duration: 300,
        delay: (cascadeNumber - 1) * 600,
        targets: destroyed.map((p) => `${p.row},${p.col}`),
      },
      duration: 300,
    });

    // Update objectives based on destroyed pieces
    this.updateObjectives(destroyed, matches, scoreGained);

    // Apply gravity
    const moved = this.board.applyGravity();
    if (moved.length > 0) {
      this.animationSteps.push({
        command: {
          id: this.nextAnimationId(),
          type: 'MOVE',
          duration: 250,
          delay: (cascadeNumber - 1) * 600 + 300,
          targets: moved.map((p) => `${p.row},${p.col}`),
          params: { fromY: 0 },
        },
        duration: 250,
      });
    }

    // Fill from top
    const spawned = this.board.fillFromTop();
    if (spawned.length > 0) {
      this.animationSteps.push({
        command: {
          id: this.nextAnimationId(),
          type: 'SPAWN',
          duration: 250,
          delay: (cascadeNumber - 1) * 600 + 550,
          targets: spawned.map((p) => `${p.row},${p.col}`),
        },
        duration: 250,
      });
    }

    // Check for cascade
    const newMatches = this.board.findMatches();
    if (newMatches.matches.length > 0) {
      this.processMatches(newMatches.matches, newMatches.specials, cascadeNumber + 1);
    } else {
      // No more cascades, check win/lose
      this.checkGameOver();
    }
  }

  private updateObjectives(destroyed: Point[], matches: Match[], scoreDelta: number): void {
    const colorCounts: Record<string, number> = {};

    for (const match of matches) {
      colorCounts[match.color] = (colorCounts[match.color] || 0) + match.length;
    }

    for (const obj of this.state.objectives) {
      switch (obj.type) {
        case 'collect_color':
          if (obj.color && colorCounts[obj.color]) {
            obj.current += colorCounts[obj.color];
          }
          break;
        case 'reach_score':
          obj.current += scoreDelta;
          break;
        case 'remove_blocker':
          // Count destroyed obstacle cells (approximate via match count)
          obj.current += matches.length;
          break;
        case 'collect_item':
          // Items collected via special piece effects
          obj.current += Math.floor(matches.length / 3);
          break;
        default:
          break;
      }
    }
  }

  private checkGameOver(): void {
    if (GameState.isLevelComplete(this.state)) {
      this.state.status = GameStatus.WIN;
    } else if (this.state.movesRemaining <= 0) {
      this.state.status = GameStatus.LOSE;
    } else {
      this.state.status = GameStatus.WAITING_INPUT;
    }
  }

  getScore(): number {
    return this.state.score;
  }
}
