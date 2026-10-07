export enum PieceType {
  RED = 'RED',
  BLUE = 'BLUE',
  GREEN = 'GREEN',
  YELLOW = 'YELLOW',
  PURPLE = 'PURPLE',
  CYAN = 'CYAN',
}

export const PIECE_TYPES = [
  PieceType.RED,
  PieceType.BLUE,
  PieceType.GREEN,
  PieceType.YELLOW,
  PieceType.PURPLE,
  PieceType.CYAN,
] as const;

export type PieceColor = PieceType;
