import type { Point, Match, AnimationCommand } from '../types/GameState';
import { GameStatus, type GameStateData, GameState } from './GameState';
import { Board } from './Board';
import { MatchFinder } from './MatchFinder';
import { Scoring } from './Scoring';
import { SpecialType } from '../types/SpecialType';
import { SpecialAbilities } from './SpecialAbilities';
import { Piece } from './Piece';

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

    // Keep renderer bookkeeping bounded to the current turn.
    this.board.destroyedThisTurn.length = 0;

    const piece1 = this.board.getPiece(r1, c1);
    const piece2 = this.board.getPiece(r2, c2);
    if (!piece1 || !piece2) return false;

    const isAdjacent =
      (Math.abs(r1 - r2) === 1 && c1 === c2) ||
      (Math.abs(c1 - c2) === 1 && r1 === r2);
    if (!isAdjacent) return false;

    // Swap, check for matches
    this.board.swapPieces(r1, c1, r2, c2);

    if (piece1.special || piece2.special) {
      this.state.status = GameStatus.RESOLVING;
      this.state.movesRemaining--;
      this.state.lastMatches = [];
      this.state.scoreAdded = 0;
      this.processSpecialSwap([piece1, piece2]);
      return true;
    }

    const { matches, specials } = this.board.findMatches();

    if (matches.length === 0) {
      // Revert swap
      this.board.swapPieces(r1, c1, r2, c2);
      return false;
    }

    this.state.status = GameStatus.RESOLVING;
    this.state.movesRemaining--;
    this.state.lastMatches = matches;
    this.state.scoreAdded = 0;

    this.processMatches(matches, specials);

    return true;
  }

  private processSpecialSwap(pieces: Piece[]): void {
    this.animationSteps = [];
    const serialized = this.board.toSerializedBoard();
    const affected: Point[] = pieces.map((piece) => piece.position);
    let specialScore = 0;

    for (const piece of pieces) {
      if (!piece.special) continue;
      const other = pieces.find((candidate) => candidate !== piece);
      switch (piece.special) {
        case SpecialType.LINE:
          affected.push(...SpecialAbilities.resolveLineClear(serialized, piece.position, 'both'));
          specialScore += 120;
          break;
        case SpecialType.BOMB:
          affected.push(...SpecialAbilities.resolveBomb(serialized, piece.position));
          specialScore += 180;
          break;
        case SpecialType.COLOR_BOMB:
          affected.push(...SpecialAbilities.resolveColorBomb(serialized, other?.color ?? piece.color));
          specialScore += 250;
          break;
        case SpecialType.ROCKET: {
          const horizontal = other ? other.row === piece.row : true;
          affected.push(...SpecialAbilities.resolveRocket(serialized, piece.position, horizontal ? 'horizontal' : 'vertical'));
          specialScore += 200;
          break;
        }
      }
    }

    const unique = [...new Map(affected.map((point) => [`${point.row},${point.col}`, point])).values()];
    const colorCounts: Record<string, number> = {};
    for (const point of unique) {
      const piece = this.board.getPiece(point.row, point.col);
      if (piece) colorCounts[piece.color] = (colorCounts[piece.color] ?? 0) + 1;
    }

    const destroyed = this.board.clearPositions(unique);
    const blockers = this.board.damageAdjacentObstacles(destroyed);
    this.state.score += specialScore;
    this.state.scoreAdded += specialScore;
    this.updateObjectives(colorCounts, blockers.length, specialScore);
    this.animationSteps.push({
      command: {
        id: this.nextAnimationId(),
        type: 'SPECIAL_ACTIVATE',
        duration: 350,
        delay: 0,
        targets: destroyed.map((point) => `${point.row},${point.col}`),
      },
      duration: 350,
    });

    this.board.applyGravity();
    this.board.fillFromTop();
    const cascade = this.board.findMatches();
    if (cascade.matches.length > 0) {
      this.processMatches(cascade.matches, cascade.specials, 2);
    } else {
      this.checkGameOver();
    }
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

    const matchedPoints = this.getMatchedPoints(matches);
    const allPoints = [...matchedPoints];

    // A special that participates in a match activates before the board drops.
    const serialized = this.board.toSerializedBoard();
    for (const point of matchedPoints) {
      const piece = this.board.getPiece(point.row, point.col);
      if (!piece?.special) continue;
      const direction = matches.find((match) => this.matchContains(match, point))?.direction ?? 'horizontal';
      const effect = piece.special === SpecialType.BOMB
        ? { type: 'bomb' as const, position: point }
        : piece.special === SpecialType.COLOR_BOMB
          ? { type: 'color_bomb' as const, position: point, color: piece.color }
          : piece.special === SpecialType.ROCKET
            ? { type: 'rocket' as const, position: point, direction }
            : { type: 'line_clear' as const, position: point, direction: 'both' as const };
      allPoints.push(...SpecialAbilities.applySpecialEffect(serialized, effect));
    }

    const colorCounts: Record<string, number> = {};
    const uniquePoints = new Map(allPoints.map((point) => [`${point.row},${point.col}`, point]));
    for (const point of uniquePoints.values()) {
      const piece = this.board.getPiece(point.row, point.col);
      if (piece) colorCounts[piece.color] = (colorCounts[piece.color] ?? 0) + 1;
    }

    const destroyed = this.board.clearPositions([...uniquePoints.values()]);
    const destroyedBlockers = this.board.damageAdjacentObstacles(destroyed);

    // Matching four or more leaves a real special piece on the board.
    for (const candidate of specials) {
      const sourceMatch = matches.find((match) =>
        match.color === candidate.color && this.matchContains(match, candidate.position)
      );
      const length = candidate.shape === 'line' ? (sourceMatch?.length ?? 4) : 5;
      const special = SpecialAbilities.matchToSpecial({ length, shape: candidate.shape });
      if (special && special !== SpecialType.NONE) {
        this.board.setPiece(candidate.position.row, candidate.position.col, new Piece({
          color: candidate.color as Piece['color'],
          special,
          row: candidate.position.row,
          col: candidate.position.col,
        }));
      }
    }

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
    this.updateObjectives(colorCounts, destroyedBlockers.length, scoreGained);

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

  private updateObjectives(
    colorCounts: Record<string, number>,
    blockersDestroyed: number,
    scoreDelta: number,
  ): void {
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
          obj.current += blockersDestroyed;
          break;
        default:
          break;
      }
    }
  }

  private getMatchedPoints(matches: Match[]): Point[] {
    const points = new Map<string, Point>();
    for (const match of matches) {
      if (match.direction === 'horizontal') {
        for (let col = match.start.col; col <= match.end.col; col++) {
          points.set(`${match.start.row},${col}`, { row: match.start.row, col });
        }
      } else {
        for (let row = match.start.row; row <= match.end.row; row++) {
          points.set(`${row},${match.start.col}`, { row, col: match.start.col });
        }
      }
    }
    return [...points.values()];
  }

  private matchContains(match: Match, point: Point): boolean {
    return match.direction === 'horizontal'
      ? point.row === match.start.row && point.col >= match.start.col && point.col <= match.end.col
      : point.col === match.start.col && point.row >= match.start.row && point.row <= match.end.row;
  }

  private checkGameOver(): void {
    if (GameState.isLevelComplete(this.state)) {
      this.state.status = GameStatus.WIN;
    } else if (this.state.movesRemaining <= 0 || !this.board.hasValidMove()) {
      this.state.status = GameStatus.LOSE;
    } else {
      this.state.status = GameStatus.WAITING_INPUT;
    }
  }

  getScore(): number {
    return this.state.score;
  }
}
