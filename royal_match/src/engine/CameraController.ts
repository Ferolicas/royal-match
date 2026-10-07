import type { CameraState } from './types';

/**
 * Orthographic camera controller that fits the board to the screen.
 * Uses a top-down view looking down the Z axis.
 */
export class CameraController {
  private state: CameraState;
  private aspectRatio: number = 1;

  constructor() {
    this.state = {
      position: { x: 0, y: 0, z: 15 },
      rotation: { x: -90, y: 0, z: 0 },
      fov: 50,
      isOrtho: true,
    };
  }

  getState(): CameraState {
    return this.state;
  }

  /** Set the camera to fit an 8x8 board (default PlayCanvas units). */
  fitToBoard(rows: number, cols: number): void {
    const boardSize = Math.max(rows, cols);
    const padding = 2;
    this.state.position.z = boardSize / 2 + padding;
    this.state.position.x = (cols - 1) / 2;
    this.state.position.y = (rows - 1) / 2;
  }

  /** Update the camera aspect ratio for the canvas. */
  setAspectRatio(ratio: number): void {
    this.aspectRatio = ratio;
  }

  getAspectRatio(): number {
    return this.aspectRatio;
  }

  /** Project a screen coordinate to board coordinates. */
  screenToBoard(
    screenX: number,
    screenY: number,
    canvasWidth: number,
    canvasHeight: number,
    rows: number,
    cols: number
  ): { row: number; col: number } | null {
    const boardSize = Math.max(rows, cols);
    const cellSize = 1; // 1 unit per cell
    const boardWidth = cols * cellSize;
    const boardHeight = rows * cellSize;

    // Account for letterboxing
    const scale = Math.min(canvasWidth / boardWidth, canvasHeight / boardHeight);
    const offsetX = (canvasWidth - boardWidth * scale) / 2;
    const offsetY = (canvasHeight - boardHeight * scale) / 2;

    const boardX = (screenX - offsetX) / scale;
    const boardY = (screenY - offsetY) / scale;

    const col = Math.floor(boardX / cellSize);
    const row = Math.floor((rows * cellSize - boardY) / cellSize);

    if (row < 0 || row >= rows || col < 0 || col >= cols) return null;

    return { row, col };
  }
}
