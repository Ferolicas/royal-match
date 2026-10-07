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

    const visited = new Set<string>();

    for (let r = 0; r < rows; r++) {
      for (let c = 0; c < cols; c++) {
        const cell = board[r][c];
        if (!isGemCell(cell)) continue;

        const color = getGemColor(cell)!;

        // Check horizontal
        const hMatches = MatchFinder.findDirection(board, r, c, 'horizontal');
        for (const m of hMatches) {
          const key = `${m.start.row},${m.start.col},${m.direction}`;
          if (!visited.has(key)) {
            matches.push(m);
            visited.add(key);
          }
        }

        // Check vertical
        const vMatches = MatchFinder.findDirection(board, r, c, 'vertical');
        for (const m of vMatches) {
          const key = `${m.start.row},${m.start.col},${m.direction}`;
          if (!visited.has(key)) {
            matches.push(m);
            visited.add(key);
          }
        }
      }
    }

    // Deduplicate overlapping matches (same cells, different entry point)
    return MatchFinder.deduplicate(matches);
  }

  private static findDirection(
    board: string[][],
    row: number,
    col: number,
    direction: MatchDirection
  ): Match[] {
    const matches: Match[] = [];
    const color = getGemColor(board[row][col]);
    if (!color) return matches;

    const rows = board.length;
    const cols = board[0].length;
    const dr = direction === 'vertical' ? 1 : 0;
    const dc = direction === 'horizontal' ? 1 : 0;

    let end = 0;
    for (let i = 1; i <= Math.max(rows, cols); i++) {
      const nr = row + dr * i;
      const nc = col + dc * i;
      if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) break;
      if (getGemColor(board[nr][nc]) === color) {
        end = i;
      } else {
        break;
      }
    }

    const length = end + 1;
    if (length >= MIN_MATCH) {
      matches.push({
        start: { row, col },
        end: { row: row + dr * end, col: col + dc * end },
        length,
        direction,
        color,
      });
    }

    return matches;
  }

  private static deduplicate(matches: Match[]): Match[] {
    const result: Match[] = [];
    const seen = new Set<string>();

    for (const m of matches) {
      const cells: string[] = [];
      if (m.direction === 'horizontal') {
        for (let c = m.start.col; c <= m.end.col; c++) {
          cells.push(`${m.start.row},${c}`);
        }
      } else {
        for (let r = m.start.row; r <= m.end.row; r++) {
          cells.push(`${r},${m.start.col}`);
        }
      }
      const key = cells.join('|');
      if (!seen.has(key)) {
        seen.add(key);
        result.push(m);
      }
    }

    return result;
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
      // Check for T or L shape by examining the pattern
      const fullLength = Math.abs(match.end.row - match.start.row) +
                          Math.abs(match.end.col - match.start.col) + 1;
      if (fullLength === match.length) {
        return match.length === 5 ? 'T' : 'L';
      }
    }
    return match.direction === 'horizontal' ? 'line' : 'line';
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
