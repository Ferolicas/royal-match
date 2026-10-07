import type { PieceColor } from '../types/PieceType';
import type { SpecialName } from '../types/SpecialType';
import { CellContent } from '../types/GameState';
import type { Point } from '../types/GameState';

export interface PieceConfig {
  color: PieceColor;
  special: SpecialName | null;
  row: number;
  col: number;
}

export class Piece {
  readonly color: PieceColor;
  special: SpecialName | null;
  row: number;
  col: number;
  isMoving: boolean;
  isInSpecialArea: boolean;
  fallDistance: number;
  collected: boolean;

  constructor(config: PieceConfig) {
    this.color = config.color;
    this.special = config.special;
    this.row = config.row;
    this.col = config.col;
    this.isMoving = false;
    this.isInSpecialArea = false;
    this.fallDistance = 0;
    this.collected = false;
  }

  get position(): Point {
    return { row: this.row, col: this.col };
  }

  setPosition(row: number, col: number): void {
    this.row = row;
    this.col = col;
  }

  isSpecial(): boolean {
    return this.special !== null;
  }

  static colorToCell(color: PieceColor): CellContent {
    return `GEM_${color}` as CellContent;
  }

  static cellToColor(cell: CellContent): PieceColor | null {
    if (cell.startsWith('GEM_')) {
      const color = cell.substring(4);
      return color as PieceColor;
    }
    return null;
  }
}
