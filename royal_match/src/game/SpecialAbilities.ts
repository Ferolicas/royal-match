import type { Point } from '../types/GameState';
import { SpecialType } from '../types/SpecialType';
import { CellContent } from '../types/GameState';

export interface SpecialEffect {
  type: 'line_clear' | 'bomb' | 'color_bomb' | 'rocket';
  position: Point;
  color?: string | null;
  direction?: 'horizontal' | 'vertical' | 'both';
}

export class SpecialAbilities {
  static resolveLineClear(
    board: (string | null)[][],
    position: Point,
    direction: 'horizontal' | 'vertical' | 'both'
  ): Point[] {
    const cleared: Point[] = [];
    const rows = board.length;
    const cols = board[0].length;

    cleared.push(position);

    if (direction === 'both' || direction === 'horizontal') {
      for (let c = 0; c < cols; c++) {
        if (c !== position.col) {
          cleared.push({ row: position.row, col: c });
        }
      }
    }

    if (direction === 'both' || direction === 'vertical') {
      for (let r = 0; r < rows; r++) {
        if (r !== position.row) {
          cleared.push({ row: r, col: position.col });
        }
      }
    }

    return cleared;
  }

  static resolveBomb(
    board: (string | null)[][],
    position: Point,
    radius: number = 1
  ): Point[] {
    const cleared: Point[] = [];
    const rows = board.length;
    const cols = board[0].length;

    for (let dr = -radius; dr <= radius; dr++) {
      for (let dc = -radius; dc <= radius; dc++) {
        const r = position.row + dr;
        const c = position.col + dc;
        if (r >= 0 && r < rows && c >= 0 && c < cols) {
          cleared.push({ row: r, col: c });
        }
      }
    }

    return cleared;
  }

  static resolveColorBomb(
    board: (string | null)[][],
    color: string
  ): Point[] {
    const cleared: Point[] = [];
    const rows = board.length;
    const cols = board[0].length;

    for (let r = 0; r < rows; r++) {
      for (let c = 0; c < cols; c++) {
        const cell = board[r][c];
        if (cell && cell.startsWith(`GEM_${color}`)) {
          cleared.push({ row: r, col: c });
        }
      }
    }

    return cleared;
  }

  static resolveRocket(
    board: (string | null)[][],
    position: Point,
    direction: 'horizontal' | 'vertical'
  ): Point[] {
    const cleared: Point[] = [];
    const rows = board.length;
    const cols = board[0].length;

    cleared.push(position);

    if (direction === 'horizontal') {
      for (let c = 0; c < cols; c++) {
        if (c !== position.col) cleared.push({ row: position.row, col: c });
      }
    } else {
      for (let r = 0; r < rows; r++) {
        if (r !== position.row) cleared.push({ row: r, col: position.col });
      }
    }

    return cleared;
  }

  static applySpecialEffect(
    board: (string | null)[][],
    effect: SpecialEffect
  ): Point[] {
    switch (effect.type) {
      case 'line_clear':
        return SpecialAbilities.resolveLineClear(
          board,
          effect.position,
          effect.direction || 'both'
        );
      case 'bomb':
        return SpecialAbilities.resolveBomb(board, effect.position, 1);
      case 'color_bomb':
        return SpecialAbilities.resolveColorBomb(board, effect.color || '');
       case 'rocket':
          return SpecialAbilities.resolveRocket(
            board,
            effect.position,
            (effect.direction && effect.direction !== 'both' ? effect.direction : 'horizontal')
          );
      default:
        return [];
    }
  }

  static matchToSpecial(
    match: { length: number; shape: string }
  ): SpecialType | null {
    if (match.length >= 6) return SpecialType.ROCKET;
    if (match.length === 5) {
      if (match.shape === 'L') return SpecialType.COLOR_BOMB;
      if (match.shape === 'T') return SpecialType.BOMB;
      return SpecialType.LINE;
    }
    if (match.length === 4) return SpecialType.LINE;
    return SpecialType.NONE;
  }
}
