export enum SpecialType {
  NONE = 'NONE',
  LINE = 'LINE',
  BOMB = 'BOMB',
  COLOR_BOMB = 'COLOR_BOMB',
  ROCKET = 'ROCKET',
}

export type SpecialName = Exclude<SpecialType, SpecialType.NONE>;
