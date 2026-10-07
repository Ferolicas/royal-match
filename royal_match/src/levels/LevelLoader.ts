import type { LevelData } from './LevelData';
import { LevelDataSchema } from './LevelData';
import { getLevelPath } from './Difficulty';
import { GameStatus, GameState, type GameStateData } from '../game/GameState';

export class LevelLoader {
  private levelsBasePath: string = 'src/levels/levels';

  async loadLevel(levelNumber: number): Promise<LevelData> {
    const path = this.getLevelPath(levelNumber);

    const response = await fetch(path);
    if (!response.ok) {
      throw new Error(`Failed to load level ${levelNumber}: ${response.status}`);
    }

    const json = (await response.json()) as unknown;
    const parsed = this.validateLevel(json);

    if (!parsed.success) {
      throw new Error(
        `Level ${levelNumber} validation failed: ${JSON.stringify(parsed.error)}`
      );
    }

    return parsed.data;
  }

  getLevelPath(levelNumber: number): string {
    const relativePath = getLevelPath(levelNumber);
    return `/${this.levelsBasePath}/${relativePath}`.replace('//', '/');
  }

  validateLevel(json: unknown) {
    return LevelDataSchema.safeParse(json);
  }

  createGameState(levelData: LevelData): GameStateData {
    return GameState.create(levelData);
  }

  async loadLevelDirect(importFn: (levelNumber: number) => Promise<unknown>, levelNumber: number): Promise<LevelData> {
    const json = await importFn(levelNumber);
    const parsed = this.validateLevel(json);
    if (!parsed.success) {
      throw new Error(`Level ${levelNumber} validation failed`);
    }
    return parsed.data;
  }
}
