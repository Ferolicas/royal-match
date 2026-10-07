import type { RenderableGem, EngineState } from './types';
import { GEM_COLORS } from '../data/colors';
import type { GemColor } from '../data/colors';
import { SpecialType } from '../types/SpecialType';
import type { SpecialName } from '../types/SpecialType';

/**
 * Manages the PlayCanvas entity lifecycle for the match-3 board.
 * Creates, pools, and syncs gem entities to match the game state.
 */
export class EntityManager {
  private gemEntities: Map<string, unknown> = new Map();
  private obstacleEntities: Map<string, unknown> = new Map();
  private entityCounter: number = 0;

  /** Generate a unique entity ID for a board cell. */
  getCellId(row: number, col: number): string {
    return `gem_${row}_${col}`;
  }

  /** Get the gem color hex for rendering. */
  getGemColorHex(color: GemColor): string {
    return GEM_COLORS[color]?.hex ?? '#FFFFFF';
  }

  /** Get the special piece visual modifier. */
  getSpecialModifier(special: SpecialName | null): string {
    if (!special) return 'none';
    switch (special) {
      case SpecialType.LINE:
        return 'line';
      case SpecialType.BOMB:
        return 'bomb';
      case SpecialType.COLOR_BOMB:
        return 'colorbomb';
      case SpecialType.ROCKET:
        return 'rocket';
      default:
        return 'none';
    }
  }

  /** Create or sync a gem entity to match the renderable state. */
  syncGem(renderable: RenderableGem, _engineApp: unknown): void {
    const cellId = this.getCellId(renderable.row, renderable.col);

    if (renderable.visible) {
      // In PlayCanvas, we would position/update the entity here.
      // For now, we just track state.
    }

    this.gemEntities.set(cellId, { ...renderable });
  }

  /** Remove all gem entities. */
  clearGems(): void {
    this.gemEntities.clear();
  }

  /** Remove all obstacle entities. */
  clearObstacles(): void {
    this.obstacleEntities.clear();
  }

  /** Convert EngineState to a list of sync operations for the renderer. */
  getGemChanges(prev: EngineState | null, next: EngineState): RenderableGem[] {
    if (!prev) return next.gems;
    return next.gems.filter(
      (gem) => !prev.gems.some((pg) => pg.entityId === gem.entityId && pg.visible === gem.visible)
    );
  }

  nextEntityId(): number {
    return this.entityCounter++;
  }
}
