import type { EngineState, RenderableGem } from './types';
import type { GameStateData } from '../game/GameState';
import type { Board } from '../game/Board';
import { CellContent } from '../types/GameState';
import { EntityManager } from './EntityManager';

/**
 * Translates the pure GameState into a renderable EngineState.
 * This is the bridge between game logic and the rendering engine.
 */
export class GameRenderer {
  private entityManager: EntityManager;

  constructor() {
    this.entityManager = new EntityManager();
  }

  /**
   * Convert the game logic Board + GameStateData into an EngineState
   * that the rendering engine can consume.
   */
  render(gameState: GameStateData, board: Board): EngineState {
    const gems: RenderableGem[] = [];
    const obstacles: EngineState['obstacles'] = [];

    for (let r = 0; r < gameState.gridRows; r++) {
      for (let c = 0; c < gameState.gridCols; c++) {
        const piece = board.getPiece(r, c);
        const cell = board.grid.getCell(r, c);

        if (piece && piece.collected === false) {
          // Active gem piece
          gems.push({
            entityId: this.entityManager.getCellId(r, c),
            row: r,
            col: c,
            color: piece.color,
            special: piece.special ? String(piece.special) : null,
            visible: true,
            scale: piece.isSpecial() ? 1.2 : 1,
          });
        } else if (cell && this.isObstacle(cell.content)) {
          // Obstacle cell
          let health = 1;
          if (cell.content === CellContent.OBSTACLE_CAGE) health = 2;
          if (cell.content === CellContent.OBSTACLE_ICE) health = 3;

          obstacles.push({
            row: r,
            col: c,
            content: cell.content,
            health,
          });
        }
      }
    }

    // Add destroyed gems (fading out)
    for (const destroyed of board.destroyedThisTurn) {
      gems.push({
        entityId: this.entityManager.getCellId(destroyed.row, destroyed.col),
        row: destroyed.row,
        col: destroyed.col,
        color: '',
        special: null,
        visible: false,
        scale: 0,
      });
    }

    return {
      gems,
      obstacles,
      camera: {
        position: { x: 0, y: 0, z: 15 },
        rotation: { x: -90, y: 0, z: 0 },
        fov: 50,
        isOrtho: true,
      },
      backgroundColor: '#0f0c29',
      isAnimating: gameState.isAnimating || false,
    };
  }

  /**
   * Convert game coordinates to render coordinates.
   * PlayCanvas uses a Y-up coordinate system; our board is [row][col].
   */
  boardToScreen(row: number, col: number): { x: number; y: number } {
    // Center the board, with row 0 at top
    const x = col;
    const y = row;
    return { x, y };
  }

  /**
   * Convert screen coordinates back to board coordinates.
   */
  screenToBoard(x: number, y: number, cols: number, rows: number): { row: number; col: number } | null {
    const col = Math.floor(x);
    const row = Math.floor(y);

    if (row < 0 || row >= rows || col < 0 || col >= cols) return null;
    return { row, col };
  }

  private isObstacle(content: CellContent): boolean {
    return (
      content === CellContent.OBSTACLE_BOX ||
      content === CellContent.OBSTACLE_CAGE ||
      content === CellContent.OBSTACLE_ICE ||
      content === CellContent.OBSTACLE_STONE
    );
  }

  /** Get the EntityManager for entity pool access. */
  getEntityManager(): EntityManager {
    return this.entityManager;
  }
}
