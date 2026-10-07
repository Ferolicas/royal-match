import type { LevelData } from './LevelData';
import { LevelDataSchema } from './LevelData';
import { getLevelPath } from './Difficulty';
import { GameState, type GameStateData } from '../game/GameState';

const levelModules = import.meta.glob<{ default: unknown }>('./levels/**/*.json');

export class LevelLoader {
  async loadLevel(levelNumber: number): Promise<LevelData> {
    if (!Number.isInteger(levelNumber) || levelNumber < 1 || levelNumber > 1000) {
      throw new Error(`Invalid level number: ${levelNumber}`);
    }
    const path = this.getLevelPath(levelNumber);
    const loadModule = levelModules[path];
    if (!loadModule) throw new Error(`Level module not found: ${path}`);
    const module = await loadModule();
    const json = module.default ?? module;
    const parsed = this.validateLevel(json);

    if (!parsed.success) {
      throw new Error(
        `Level ${levelNumber} validation failed: ${JSON.stringify(parsed.error)}`
      );
    }

    return parsed.data;
  }

  getLevelPath(levelNumber: number): string {
    return `./${getLevelPath(levelNumber)}`;
  }

  validateLevel(json: unknown) {
    return LevelDataSchema.safeParse(json);
  }

  createGameState(levelData: LevelData): GameStateData {
    return GameState.create(levelData);
  }
}
