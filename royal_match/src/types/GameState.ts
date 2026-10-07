export type Point = { row: number; col: number };

export type Direction = 'north' | 'south' | 'east' | 'west';

export type MatchDirection = 'horizontal' | 'vertical';

export interface Match {
  readonly start: Point;
  readonly end: Point;
  readonly length: number;
  readonly direction: MatchDirection;
  readonly color: string;
}

export enum CellContent {
  EMPTY = 'EMPTY',
  BLOCKED = 'BLOCKED',
  OBSTACLE_BOX = 'OBSTACLE_BOX',
  OBSTACLE_CAGE = 'OBSTACLE_CAGE',
  OBSTACLE_ICE = 'OBSTACLE_ICE',
  OBSTACLE_STONE = 'OBSTACLE_STONE',
  VOID = 'VOID',
}

export type AnimationType =
  | 'MOVE'
  | 'SCALE_POP'
  | 'SCALE_SHRINK'
  | 'ROTATE'
  | 'DESTROY'
  | 'SPAWN'
  | 'SPECIAL_ACTIVATE';

export interface AnimationCommand {
  readonly id: string;
  readonly type: AnimationType;
  readonly duration: number;
  readonly delay: number;
  readonly targets: string[];
  readonly params?: Record<string, unknown>;
}
