import { ObjectiveType } from '../types/ObjectiveType';

export interface ObjectiveMetadata {
  id: ObjectiveType;
  name: string;
  description: string;
  icon: string;
}

export const OBJECTIVE_METADATA: Record<ObjectiveType, ObjectiveMetadata> = {
  [ObjectiveType.COLLECT_COLOR]: {
    id: ObjectiveType.COLLECT_COLOR,
    name: 'Collect Gems',
    description: 'Collect {target} gems of a specific color',
    icon: '◆',
  },
  [ObjectiveType.REACH_SCORE]: {
    id: ObjectiveType.REACH_SCORE,
    name: 'Score Target',
    description: 'Reach {target} points',
    icon: '★',
  },
  [ObjectiveType.REMOVE_BLOCKER]: {
    id: ObjectiveType.REMOVE_BLOCKER,
    name: 'Clear Blockers',
    description: 'Destroy {target} obstacle cells',
    icon: '▢',
  },
  [ObjectiveType.COLLECT_ITEM]: {
    id: ObjectiveType.COLLECT_ITEM,
    name: 'Collect Items',
    description: 'Collect {target} {itemId}s',
    icon: '★',
  },
  [ObjectiveType.DROP_DOWN]: {
    id: ObjectiveType.DROP_DOWN,
    name: 'Drop Objects',
    description: 'Drop {target} objects to the bottom',
    icon: '↓',
  },
};

export function getObjectiveDescription(type: ObjectiveType, target: number, color?: string, itemId?: string): string {
  const meta = OBJECTIVE_METADATA[type];
  return meta.description
    .replace('{target}', String(target))
    .replace('{itemId}', itemId ?? '')
    .replace('{color}', color ?? '');
}
