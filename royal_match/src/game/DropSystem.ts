import type { Point } from '../types/GameState';

export interface DropResult {
  moved: Point[];
  spawned: Point[];
}

export interface DropConfig {
  cols: number;
  rows: number;
}

export function applyGravity(
  pieceGrid: (string | null)[][],
  config: DropConfig
): Point[] {
  const moved: Point[] = [];

  for (let c = 0; c < config.cols; c++) {
    let writeRow = config.rows - 1;

    for (let r = config.rows - 1; r >= 0; r--) {
      if (pieceGrid[r][c] !== null) {
        if (writeRow !== r) {
          pieceGrid[writeRow][c] = pieceGrid[r][c];
          pieceGrid[r][c] = null;
          moved.push({ row: writeRow, col: c, originalRow: r } as unknown as Point);
        }
        writeRow--;
      }
    }
  }

  return moved;
}

export function fillFromTop(
  pieceGrid: (string | null)[][],
  colors: string[],
  config: DropConfig
): Point[] {
  const spawned: Point[] = [];
  let colorIndex = 0;

  for (let c = 0; c < config.cols; c++) {
    for (let r = config.rows - 1; r >= 0; r--) {
      if (pieceGrid[r][c] === null) {
        const color = colors[colorIndex % colors.length];
        pieceGrid[r][c] = color;
        spawned.push({ row: r, col: c } as Point);
        colorIndex++;
      }
    }
  }

  return spawned;
}
