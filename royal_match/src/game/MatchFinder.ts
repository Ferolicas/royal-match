import type { Point, Match, MatchDirection } from '../types/GameState';
import { CellContent } from '../types/GameState';

export interface MatchResult {
  matches: Match[];
  specialPieces: {
    position: Point;
    direction: MatchDirection | null;
    shape: 'line' | 'T' | 'L' | 'cross';
    color: string;
  }[];
}

const MIN_MATCH = 3;

function getGemColor(cellValue: string): string | null {
  if (cellValue.startsWith('GEM_')) {
    return cellValue.substring(4);
  }
  return null;
}

function isGemCell(cellValue: string): boolean {
  return cellValue.startsWith('GEM_');
}

export class MatchFinder {
  static findMatches(board: string[][]): Match[] {
    const matches: Match[] = [];
    const rows = board.length;
    if (rows === 0) return matches;
    const cols = board[0].length;

    // Find horizontal matches — scan each row, skip past maximal runs
    for (let r = 0; r < rows; r++) {
      let c = 0;
      while (c < cols) {
        const cell = board[r][c];
        if (!isGemCell(cell)) {
          c++;
          continue;
        }

        const color = getGemColor(cell)!;
        let end = 0;
        for (let i = 1; i < cols - c; i++) {
          if (getGemColor(board[r][c + i]) === color) {
            end = i;
          } else {
            break;
          }
        }

        const length = end + 1;
        if (length >= MIN_MATCH) {
          matches.push({
            start: { row: r, col: c },
            end: { row: r, col: c + end },
            length,
            direction: 'horizontal',
            color,
          });
          c = c + end + 1; // Skip past the entire match
        } else {
          c++;
        }
      }
    }

    // Find vertical matches — scan each column, skip past maximal runs
    for (let c = 0; c < cols; c++) {
      let r = 0;
      while (r < rows) {
        const cell = board[r][c];
        if (!isGemCell(cell)) {
          r++;
          continue;
        }

        const color = getGemColor(cell)!;
        let end = 0;
        for (let i = 1; i < rows - r; i++) {
          if (getGemColor(board[r + i][c]) === color) {
            end = i;
          } else {
            break;
          }
        }

        const length = end + 1;
        if (length >= MIN_MATCH) {
          matches.push({
            start: { row: r, col: c },
            end: { row: r + end, col: c },
            length,
            direction: 'vertical',
            color,
          });
          r = r + end + 1; // Skip past the entire match
        } else {
          r++;
        }
      }
    }

    return matches;
  }

  static findMatchesWithSpecials(board: string[][]): MatchResult {
    const matches = MatchFinder.findMatches(board);
    const specialPieces: MatchResult['specialPieces'] = [];

    for (const match of matches) {
      if (match.length >= 4) {
        const shape = MatchFinder.determineShape(match);
        specialPieces.push({
          position: match.start,
          direction: match.direction,
          shape,
          color: match.color,
        });
      }
    }

    return { matches, specialPieces };
  }

  private static determineShape(match: Match): MatchResult['specialPieces'][number]['shape'] {
    if (match.length >= 6) {
      return 'line';
    }
    if (match.length === 5) {
      // All matches found by MatchFinder are straight lines.
      // T/L shape detection would require pattern matching on L/T intersections,
      // which is a future enhancement. For now, treat all 5-matches as 'L'.
      return 'L';
    }
    return 'line';
  }

  static hasAnyMatch(board: string[][]): boolean {
    return MatchFinder.findMatches(board).length > 0;
  }

  static cellToString(content: CellContent, color?: string): string {
    if (content === CellContent.EMPTY && color) {
      return `GEM_${color}`;
    }
    return content;
  }
}
