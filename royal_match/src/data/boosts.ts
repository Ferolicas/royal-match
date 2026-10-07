export enum BoostType {
  SHUFFLE = 'shuffle',
  HAMMER = 'hammer',
  ROCKET = 'rocket',
}

export interface BoostDefinition {
  id: BoostType;
  name: string;
  description: string;
  icon: string;
  cost: number;
}

export const BOOST_DEFINITIONS: Record<BoostType, BoostDefinition> = {
  [BoostType.SHUFFLE]: {
    id: BoostType.SHUFFLE,
    name: 'Shuffle',
    description: 'Shuffle all gems on the board to find new matches.',
    icon: '⇋',
    cost: 1,
  },
  [BoostType.HAMMER]: {
    id: BoostType.HAMMER,
    name: 'Hammer',
    description: 'Tap to destroy a single gem and its neighbors.',
    icon: '🔨',
    cost: 1,
  },
  [BoostType.ROCKET]: {
    id: BoostType.ROCKET,
    name: 'Rocket',
    description: 'Clear an entire row or column.',
    icon: '🚀',
    cost: 1,
  },
};
