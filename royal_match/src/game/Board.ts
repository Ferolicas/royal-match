import type { CellValue } from '../levels/LevelData';
import { CellContent } from '../types/GameState';
import type { Point } from '../types/GameState';
import { Grid, type Cell } from './Grid';
import { Piece } from './Piece';
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

  hasValidMove(): boolean {
    const directions = [[1, 0], [0, 1]] as const;
    for (let row = 0; row < this.config.rows; row++) {
      for (let col = 0; col < this.config.cols; col++) {
        if (!this.getPiece(row, col)) continue;
        for (const [dr, dc] of directions) {
          const nextRow = row + dr;
          const nextCol = col + dc;
          if (!this.getPiece(nextRow, nextCol)) continue;
          this.swapPieces(row, col, nextRow, nextCol);
          const valid = this.findMatches().matches.some((match) =>
            this.matchContains(match, row, col) || this.matchContains(match, nextRow, nextCol)
          );
          this.swapPieces(row, col, nextRow, nextCol);
          if (valid) return true;
        }
      }
    }
    return false;
  }

  private matchContains(match: ReturnType<typeof MatchFinder.findMatches>[number], row: number, col: number): boolean {
    return match.direction === 'horizontal'
      ? row === match.start.row && col >= match.start.col && col <= match.end.col
      : col === match.start.col && row >= match.start.row && row <= match.end.row;
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
    const seen = new Set<string>();

    for (const match of matches) {
      const { start, end, direction } = match;
      if (direction === 'horizontal') {
        for (let c = start.col; c <= end.col; c++) {
          const key = `${start.row},${c}`;
          if (!seen.has(key)) {
            seen.add(key);
            destroyed.push(this.removePieceAt(start.row, c));
          }
        }
      } else {
        for (let r = start.row; r <= end.row; r++) {
          const key = `${r},${start.col}`;
          if (!seen.has(key)) {
            seen.add(key);
            destroyed.push(this.removePieceAt(r, start.col));
          }
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
    this.destroyedThisTurn.push({ row, col });
    return { row, col };
  }

  clearPositions(points: Point[]): Point[] {
    const cleared: Point[] = [];
    const seen = new Set<string>();
    for (const point of points) {
      const key = `${point.row},${point.col}`;
      if (seen.has(key) || !this.getPiece(point.row, point.col)) continue;
      seen.add(key);
      cleared.push(this.removePieceAt(point.row, point.col));
    }
    return cleared;
  }

  setSpecial(row: number, col: number, special: Exclude<SpecialType, SpecialType.NONE>): void {
    const piece = this.getPiece(row, col);
    if (!piece) return;
    piece.special = special;
    const cell = this.grid.getCell(row, col);
    if (cell) cell.specialType = special;
  }

  damageAdjacentObstacles(points: Point[]): Point[] {
    const candidates = new Set<string>();
    for (const point of points) {
      for (const neighbor of this.grid.getNeighbors(point.row, point.col)) {
        candidates.add(`${neighbor.row},${neighbor.col}`);
      }
    }

    const destroyed: Point[] = [];
    for (const key of candidates) {
      const [row, col] = key.split(',').map(Number);
      const cell = this.grid.getCell(row, col);
      if (!cell || !this.isObstacle(cell.content)) continue;
      cell.health = Math.max(0, (cell.health ?? 1) - 1);
      if (cell.health === 0) {
        cell.content = CellContent.EMPTY;
        destroyed.push({ row, col });
      }
    }
    return destroyed;
  }

  private isObstacle(content: CellContent): boolean {
    return content === CellContent.OBSTACLE_BOX ||
      content === CellContent.OBSTACLE_CAGE ||
      content === CellContent.OBSTACLE_ICE ||
      content === CellContent.OBSTACLE_STONE;
  }

  applyGravity(): Point[] {
    const moved: Point[] = [];

    for (let c = 0; c < this.config.cols; c++) {
      let writeRow = this.config.rows - 1;

      for (let r = this.config.rows - 1; r >= 0; r--) {
        const cell = this.grid.getCell(r, c);
        if (cell && cell.content !== CellContent.EMPTY) {
          writeRow = r - 1;
          continue;
        }
        if (this.pieces[r][c] !== null) {
          if (writeRow !== r) {
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
          writeRow--;
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
          if (cell && cell.content !== CellContent.EMPTY) continue;

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
