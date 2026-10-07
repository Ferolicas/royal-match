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

export const CELL_VALUES: CellValue[] = [
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
];

const ObjectiveBaseSchema = z.object({
  description: z.string(),
  target: z.number().int().positive(),
  current: z.number().int().optional(),
});

export const ObjectiveSchema = z.discriminatedUnion('type', [
  ObjectiveBaseSchema.extend({ type: z.literal('collect_color'), color: z.enum(['RED', 'BLUE', 'GREEN', 'YELLOW', 'PURPLE', 'CYAN']) }),
  ObjectiveBaseSchema.extend({ type: z.literal('reach_score') }),
  ObjectiveBaseSchema.extend({ type: z.literal('remove_blocker') }),
  ObjectiveBaseSchema.extend({ type: z.literal('collect_item'), itemId: z.string().min(1) }),
  ObjectiveBaseSchema.extend({ type: z.literal('drop_down'), itemId: z.string().min(1).optional() }),
]);

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
}).superRefine((level, context) => {
  if (level.boardLayout.length !== level.grid.rows) {
    context.addIssue({ code: z.ZodIssueCode.custom, path: ['boardLayout'], message: 'Board row count must match grid.rows' });
  }
  level.boardLayout.forEach((row, index) => {
    if (row.length !== level.grid.cols) {
      context.addIssue({ code: z.ZodIssueCode.custom, path: ['boardLayout', index], message: 'Board column count must match grid.cols' });
    }
  });
  if (!(level.scoreStar1 <= level.scoreStar2 && level.scoreStar2 <= level.scoreStar3)) {
    context.addIssue({ code: z.ZodIssueCode.custom, path: ['scoreStar1'], message: 'Star thresholds must be monotonic' });
  }
});

export type LevelData = z.infer<typeof LevelDataSchema>;

/** Runtime objective with a mutable `current` progress tracker. */
type WithCurrent<T> = T extends unknown ? Omit<T, 'current'> & { current: number } : never;
export type RuntimeObjective = WithCurrent<ObjectiveData>;

export const LEVEL_SCHEMA_VERSION = '1.0.0';
