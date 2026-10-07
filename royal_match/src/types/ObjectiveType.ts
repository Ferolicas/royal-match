export enum ObjectiveType {
  COLLECT_COLOR = 'collect_color',
  REACH_SCORE = 'reach_score',
  REMOVE_BLOCKER = 'remove_blocker',
  COLLECT_ITEM = 'collect_item',
  DROP_DOWN = 'drop_down',
}

export type ObjectiveTypeId = `${ObjectiveType}`;

export interface BaseObjective {
  readonly type: ObjectiveType;
  readonly description: string;
}

export interface CollectColorObjective extends BaseObjective {
  readonly type: ObjectiveType.COLLECT_COLOR;
  readonly color: string;
  readonly target: number;
  current: number;
}

export interface ReachScoreObjective extends BaseObjective {
  readonly type: ObjectiveType.REACH_SCORE;
  readonly target: number;
  current: number;
}

export interface RemoveBlockerObjective extends BaseObjective {
  readonly type: ObjectiveType.REMOVE_BLOCKER;
  readonly target: number;
  current: number;
}

export interface CollectItemObjective extends BaseObjective {
  readonly type: ObjectiveType.COLLECT_ITEM;
  readonly itemId: string;
  readonly target: number;
  current: number;
}

export interface DropDownObjective extends BaseObjective {
  readonly type: ObjectiveType.DROP_DOWN;
  readonly target: number;
  current: number;
}

export type AnyObjective =
  | CollectColorObjective
  | ReachScoreObjective
  | RemoveBlockerObjective
  | CollectItemObjective
  | DropDownObjective;
