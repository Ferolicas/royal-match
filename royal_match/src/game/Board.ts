import type { CellValue } from '../levels/LevelData';
import type { ObjectiveData } from '../levels/LevelData';
import { CellContent } from '../types/GameState';
import type { Point } from '../types/GameState';
import { Grid, type Cell } from './Grid';
import { Piece, type PieceConfig } from './Piece';
import type { PieceColor } from '../types/PieceType';
import { PIECE_TYPES } from '../types/PieceType';
import { MatchFinder } from './MatchFinder';
import { SpecialType } from '../types/SpecialType';

export type BoardCell = string;

export interface BoardConfig {
  rows: number;
  cols: number;
  gemTypes: number;
  minMatch: number;
}

const DEFAULT_CONFIG: BoardConfig = {
  rows: 8,
  cols: 8,
  gemTypes: 6,
  minMatch: 3,
};

export class Board {
  readonly config: BoardConfig;
  readonly grid: Grid;
  readonly pieces: (Piece | null)[][];
  readonly destroyedThisTurn: Point[];

  constructor(config?: Partial<BoardConfig>) {
    this.config = { ...DEFAULT_CONFIG, ...config };
    this.grid = new Grid(this.config.rows, this.config.cols);
    this.pieces = this.createEmptyPieces();
    this.destroyedThisTurn = [];
  }

  private createEmptyPieces(): (Piece | null)[][] {
    return Array.from({ length: this.config.rows }, () =>
      Array(this.config.cols).fill(null)
    );
  }

  initializeFromLayout(layout: CellValue[][]): void {
    for (let r = 0; r < this.config.rows; r++) {
      for (let c = 0; c < this.config.cols; c++) {
        const cellValue = layout[r][c];
        const color = Piece.cellToColor(
          cellValue as CellContent
        );

        if (color) {
          const cell: Cell = { content: CellContent.EMPTY, gemColor: color };
          this.grid.setCell(r, c, cell);
          this.pieces[r][c] = new Piece({ color, special: null, row: r, col: c });
        } else if (
          cellValue === 'BLOCKED' ||
          cellValue === 'VOID'
        ) {
          this.grid.setCell(r, c, {
            content:
              cellValue === 'BLOCKED' ? CellContent.BLOCKED : CellContent.VOID,
          });
        } else if (cellValue === 'OBSTACLE_BOX') {
          this.grid.setCell(r, c, {
            content: CellContent.OBSTACLE_BOX,
            health: 1,
          });
        } else if (cellValue === 'OBSTACLE_CAGE') {
          this.grid.setCell(r, c, {
            content: CellContent.OBSTACLE_CAGE,
            health: 2,
          });
        } else if (cellValue === 'OBSTACLE_ICE') {
          this.grid.setCell(r, c, {
            content: CellContent.OBSTACLE_ICE,
            health: 3,
          });
        } else if (cellValue === 'OBSTACLE_STONE') {
          this.grid.setCell(r, c, {
            content: CellContent.OBSTACLE_STONE,
            health: 1,
          });
        }
      }
    }
  }

  swapPieces(r1: number, c1: number, r2: number, c2: number): void {
    this.grid.swap(r1, c1, r2, c2);
    [this.pieces[r1][c1], this.pieces[r2][c2]] = [
      this.pieces[r2][c2],
      this.pieces[r1][c1],
    ];
    if (this.pieces[r1][c1]) {
      this.pieces[r1][c1]!.setPosition(r1, c1);
    }
    if (this.pieces[r2][c2]) {
      this.pieces[r2][c2]!.setPosition(r2, c2);
    }
  }

  getPiece(row: number, col: number): Piece | null {
    if (!this.grid.isValidPosition(row, col)) return null;
    return this.pieces[row][col];
  }

  setPiece(row: number, col: number, piece: Piece | null): void {
    this.pieces[row][col] = piece;
    if (piece) {
      piece.setPosition(row, col);
      const cell = this.grid.getCell(row, col) ?? { content: CellContent.EMPTY };
      cell.content = CellContent.EMPTY;
      cell.gemColor = piece.color;
      cell.specialType = piece.special;
      this.grid.setCell(row, col, cell);
    }
  }

  findMatches(): {
    matches: ReturnType<typeof MatchFinder.findMatches>;
    specials: ReturnType<typeof MatchFinder.findMatchesWithSpecials>['specialPieces'];
  } {
    const boardStr = this.toSerializedBoard();
    const result = MatchFinder.findMatchesWithSpecials(boardStr);
    return { matches: result.matches, specials: result.specialPieces };
  }

  toSerializedBoard(): string[][] {
    const result: string[][] = [];
    for (let r = 0; r < this.config.rows; r++) {
      const row: string[] = [];
      for (let c = 0; c < this.config.cols; c++) {
        const cell = this.grid.getCell(r, c);
        if (!cell) {
          row.push(CellContent.EMPTY);
          continue;
        }
        if (cell.gemColor) {
          row.push(`GEM_${cell.gemColor}` as string);
        } else {
          row.push(cell.content);
        }
      }
      result.push(row);
    }
    return result;
  }

  removeMatches(matches: ReturnType<typeof MatchFinder.findMatches>): Point[] {
    const destroyed: Point[] = [];

    for (const match of matches) {
      const { start, end, direction } = match;
      if (direction === 'horizontal') {
        for (let c = start.col; c <= end.col; c++) {
          destroyed.push(this.removePieceAt(start.row, c));
        }
      } else {
        for (let r = start.row; r <= end.row; r++) {
          destroyed.push(this.removePieceAt(r, start.col));
        }
      }
    }

    return destroyed;
  }

  private removePieceAt(row: number, col: number): Point {
    this.pieces[row][col] = null;
    const cell = this.grid.getCell(row, col) ?? { content: CellContent.EMPTY };
    cell.content = CellContent.EMPTY;
    cell.gemColor = undefined;
    cell.specialType = null;
    this.grid.setCell(row, col, cell);
    return { row, col };
  }

  applyGravity(): Point[] {
    const moved: Point[] = [];

    for (let c = 0; c < this.config.cols; c++) {
      let writeRow = -1;

      for (let r = this.config.rows - 1; r >= 0; r--) {
        const cell = this.grid.getCell(r, c);
        if (this.pieces[r][c] !== null) {
          if (writeRow >= 0) {
            // Move piece from r to writeRow
            const piece = this.pieces[r][c]!;
            this.pieces[writeRow][c] = piece;
            this.pieces[r][c] = null;
            piece.setPosition(writeRow, c);

            const cell = this.grid.getCell(r, c) ?? { content: CellContent.EMPTY };
            cell.content = CellContent.EMPTY;
            cell.gemColor = undefined;
            this.grid.setCell(r, c, cell);

            const targetCell = this.grid.getCell(writeRow, c) ?? { content: CellContent.EMPTY };
            targetCell.content = CellContent.EMPTY;
            targetCell.gemColor = piece.color;
            targetCell.specialType = piece.special;
            this.grid.setCell(writeRow, c, targetCell);

            moved.push({ row: writeRow, col: c });
          }
          writeRow = writeRow >= 0 ? writeRow - 1 : r - 1;
        } else if (writeRow >= 0) {
          // Empty space, continue looking
        } else {
          writeRow = r - 1;
        }
      }
    }

    return moved;
  }

  fillFromTop(): Point[] {
    const spawned: Point[] = [];

    for (let c = 0; c < this.config.cols; c++) {
      for (let r = this.config.rows - 1; r >= 0; r--) {
        if (this.pieces[r][c] === null) {
          const cell = this.grid.getCell(r, c);
          if (cell && cell.content === CellContent.BLOCKED) continue;
          if (cell && cell.content === CellContent.VOID) continue;

          const color = this.randomColor();
          const piece = new Piece({
            color,
            special: null,
            row: r,
            col: c,
          });
          this.pieces[r][c] = piece;

          const newCell = this.grid.getCell(r, c) ?? { content: CellContent.EMPTY };
          newCell.content = CellContent.EMPTY;
          newCell.gemColor = color;
          newCell.specialType = null;
          this.grid.setCell(r, c, newCell);

          spawned.push({ row: r, col: c });
        }
      }
    }

    return spawned;
  }

  /**
   * The `colors` array is used for the non-deterministic color picker.
   */
  private colors: PieceColor[] = [...PIECE_TYPES];

  private nextColorIndex = 0;

  randomColor(): PieceColor {
    const color = this.colors[this.nextColorIndex % this.colors.length];
    this.nextColorIndex = (this.nextColorIndex + 1) % this.colors.length;
    return color;
  }

  clone(): Board {
    const newBoard = new Board(this.config);
    for (let r = 0; r < this.config.rows; r++) {
      for (let c = 0; c < this.config.cols; c++) {
        const piece = this.pieces[r][c];
        if (piece) {
          newBoard.pieces[r][c] = new Piece({
            color: piece.color,
            special: piece.special,
            row: r,
            col: c,
          });
        }
        newBoard.grid.setCell(r, c, { ...this.grid.getCell(r, c)! });
      }
    }
    return newBoard;
  }
}
