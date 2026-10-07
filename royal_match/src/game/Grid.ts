import { CellContent, type Point } from '../types/GameState';
import type { PieceColor } from '../types/PieceType';

export interface Cell {
  content: CellContent;
  gemColor?: PieceColor;
  specialType?: string | null;
  hasItem?: boolean;
  itemId?: string | null;
  health?: number;
}

export class Grid {
  readonly rows: number;
  readonly cols: number;
  cells: Cell[][];

  constructor(rows: number, cols: number) {
    this.rows = rows;
    this.cols = cols;
    this.cells = this.createEmptyGrid();
  }

  private createEmptyGrid(): Cell[][] {
    return Array.from({ length: this.rows }, () =>
      Array.from({ length: this.cols }, () => ({ content: CellContent.EMPTY }))
    );
  }

  isValidPosition(row: number, col: number): boolean {
    return row >= 0 && row < this.rows && col >= 0 && col < this.cols;
  }

  getCell(row: number, col: number): Cell | null {
    if (!this.isValidPosition(row, col)) return null;
    return this.cells[row][col];
  }

  setCell(row: number, col: number, cell: Cell): void {
    if (!this.isValidPosition(row, col)) {
      throw new Error(`setCell out of bounds: ${row}, ${col}`);
    }
    this.cells[row][col] = cell;
  }

  isCellEmpty(row: number, col: number): boolean {
    const cell = this.getCell(row, col);
    if (!cell) return false;
    return cell.content === CellContent.EMPTY || cell.content === CellContent.VOID;
  }

  hasGem(row: number, col: number): boolean {
    const cell = this.getCell(row, col);
    if (!cell) return false;
    return cell.content === CellContent.EMPTY && cell.gemColor !== undefined;
  }

  getNeighbors(row: number, col: number): Point[] {
    const neighbors: Point[] = [];
    const directions: Point[] = [
      { row: -1, col: 0 },
      { row: 1, col: 0 },
      { row: 0, col: -1 },
      { row: 0, col: 1 },
    ];

    for (const dir of directions) {
      const nr = row + dir.row;
      const nc = col + dir.col;
      if (this.isValidPosition(nr, nc)) {
        neighbors.push({ row: nr, col: nc });
      }
    }
    return neighbors;
  }

  swap(row1: number, col1: number, row2: number, col2: number): void {
    const cell1 = this.cells[row1][col1];
    const cell2 = this.cells[row2][col2];
    this.cells[row1][col1] = cell2;
    this.cells[row2][col2] = cell1;
  }

  clone(): Grid {
    const newGrid = new Grid(this.rows, this.cols);
    newGrid.cells = this.cells.map((row) =>
      row.map((cell) => ({ ...cell, gemColor: cell.gemColor, specialType: cell.specialType }))
    );
    return newGrid;
  }
}
