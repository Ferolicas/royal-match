import { z } from 'zod';
import { Difficulty } from '../types/Difficulty';

export const CellSchema = z.enum([
  'EMPTY',
  'GEM_RED',
  'GEM_BLUE',
  'GEM_GREEN',
  'GEM_YELLOW',
  'GEM_PURPLE',
  'GEM_CYAN',
  'BLOCKED',
  'OBSTACLE_BOX',
  'OBSTACLE_CAGE',
  'OBSTACLE_ICE',
  'OBSTACLE_STONE',
  'VOID',
]);

export type CellValue = z.infer<typeof CellSchema>;

export const ObjectiveSchema = z.object({
  type: z.enum([
    'collect_color',
    'reach_score',
    'remove_blocker',
    'collect_item',
    'drop_down',
  ]),
  description: z.string(),
  target: z.number().int().positive(),
  color: z.string().optional(),
  itemId: z.string().optional(),
  current: z.number().int().optional(),
});

export type ObjectiveData = z.infer<typeof ObjectiveSchema>;

export const LevelDataSchema = z.object({
  levelNumber: z.number().int().min(1).max(1000),
  difficulty: z.nativeEnum(Difficulty),
  title: z.string(),
  description: z.string(),
  grid: z.object({
    cols: z.number().int().min(4).max(12),
    rows: z.number().int().min(4).max(12),
  }),
  moveLimit: z.number().int().min(1).max(50),
  scoreStar1: z.number().int().min(0),
  scoreStar2: z.number().int().min(0),
  scoreStar3: z.number().int().min(0),
  objectives: z.array(ObjectiveSchema).min(1).max(5),
  boardLayout: z.array(z.array(CellSchema)),
  preplacedSpecials: z
    .array(
      z.object({
        position: z.object({ row: z.number(), col: z.number() }),
        specialType: z.enum(['LINE', 'BOMB', 'COLOR_BOMB', 'ROCKET']),
        color: z.string().optional(),
      })
    )
    .optional(),
  rewards: z
    .object({
      coins: z.number().int().min(0),
      stars: z.number().int().min(1).max(3),
      unlocks: z.array(z.string()).optional(),
    })
    .optional(),
  seed: z.number().int(),
  version: z.string(),
});

export type LevelData = z.infer<typeof LevelDataSchema>;

/** Runtime objective with a mutable `current` progress tracker. */
export type RuntimeObjective = Omit<ObjectiveData, 'current'> & { current: number };

export const LEVEL_SCHEMA_VERSION = '1.0.0';
