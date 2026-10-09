// MatchFinder.cs — Finds all maximal matches of 3+ in the grid
// Ported from src/game/MatchFinder.ts

using System.Collections.Generic;
using RoyalMatch.Types;

namespace RoyalMatch.Match3
{
    public class MatchFinder
    {
        private const int MIN_MATCH = 3;

        public class MatchResult
        {
            public List<Match> Matches;
            public List<SpecialPiece> SpecialPieces;

            public MatchResult()
            {
                Matches = new List<Match>();
                SpecialPieces = new List<SpecialPiece>();
            }
        }

        public class SpecialPiece
        {
            public Point Position;
            public MatchDirection? Direction;
            public string Shape; // "line", "T", "L", "cross"
            public string Color;
            public List<Point> SourcePoints;

            public SpecialPiece(Point position, MatchDirection? direction, string shape, string color,
                List<Point> sourcePoints = null)
            {
                Position = position;
                Direction = direction;
                Shape = shape;
                Color = color;
                SourcePoints = sourcePoints ?? new List<Point>();
            }
        }

        private static string GetGemColor(string cellValue)
        {
            if (cellValue.StartsWith("GEM_"))
                return cellValue.Substring(4);
            return null;
        }

        private static bool IsGemCell(string cellValue)
        {
            return cellValue.StartsWith("GEM_");
        }

        public static List<Match> FindMatches(string[][] board)
        {
            var matches = new List<Match>();
            int rows = board.Length;
            if (rows == 0) return matches;
            int cols = board[0].Length;

            // Find horizontal matches — scan each row, skip past maximal runs
            for (int r = 0; r < rows; r++)
            {
                int c = 0;
                while (c < cols)
                {
                    string cell = board[r][c];
                    if (!IsGemCell(cell))
                    {
                        c++;
                        continue;
                    }

                    string color = GetGemColor(cell);
                    int end = 0;
                    for (int i = 1; i < cols - c; i++)
                    {
                        if (GetGemColor(board[r][c + i]) == color)
                            end = i;
                        else
                            break;
                    }

                    int length = end + 1;
                    if (length >= MIN_MATCH)
                    {
                        matches.Add(new Match(
                            new Point(r, c),
                            new Point(r, c + end),
                            length,
                            MatchDirection.Horizontal,
                            color));
                        c = c + end + 1;
                    }
                    else
                    {
                        c++;
                    }
                }
            }

            // Find vertical matches — scan each column, skip past maximal runs
            for (int c = 0; c < cols; c++)
            {
                int r = 0;
                while (r < rows)
                {
                    string cell = board[r][c];
                    if (!IsGemCell(cell))
                    {
                        r++;
                        continue;
                    }

                    string color = GetGemColor(cell);
                    int end = 0;
                    for (int i = 1; i < rows - r; i++)
                    {
                        if (GetGemColor(board[r + i][c]) == color)
                            end = i;
                        else
                            break;
                    }

                    int length = end + 1;
                    if (length >= MIN_MATCH)
                    {
                        matches.Add(new Match(
                            new Point(r, c),
                            new Point(r + end, c),
                            length,
                            MatchDirection.Vertical,
                            color));
                        r = r + end + 1;
                    }
                    else
                    {
                        r++;
                    }
                }
            }

            return matches;
        }

        public static MatchResult FindMatchesWithSpecials(string[][] board)
        {
            var result = new MatchResult();
            result.Matches = FindMatches(board);
            var specialPieces = new List<SpecialPiece>();
            var shapedMatches = new HashSet<Match>();

            var horizontal = result.Matches.FindAll(m => m.Direction == MatchDirection.Horizontal);
            var vertical = result.Matches.FindAll(m => m.Direction == MatchDirection.Vertical);

            foreach (var h in horizontal)
            {
                foreach (var v in vertical)
                {
                    bool intersects = h.Color == v.Color &&
                        v.Start.Col >= h.Start.Col && v.Start.Col <= h.End.Col &&
                        h.Start.Row >= v.Start.Row && h.Start.Row <= v.End.Row;

                    if (!intersects) continue;

                    shapedMatches.Add(h);
                    shapedMatches.Add(v);
                    var intersection = new Point(h.Start.Row, v.Start.Col);
                    bool hEndpoint = intersection.Col == h.Start.Col || intersection.Col == h.End.Col;
                    bool vEndpoint = intersection.Row == v.Start.Row || intersection.Row == v.End.Row;
                    string shape = hEndpoint && vEndpoint ? "L" :
                                  (hEndpoint || vEndpoint ? "T" : "cross");

                    specialPieces.Add(new SpecialPiece(intersection, null, shape, h.Color));
                }
            }

            foreach (var match in result.Matches)
            {
                if (match.Length >= 4 && !shapedMatches.Contains(match))
                {
                    Point pos;
                    if (match.Direction == MatchDirection.Horizontal)
                    {
                        pos = new Point(match.Start.Row, match.Start.Col + (match.Length - 1) / 2);
                    }
                    else
                    {
                        pos = new Point(match.Start.Row + (match.Length - 1) / 2, match.Start.Col);
                    }
                    specialPieces.Add(new SpecialPiece(pos, match.Direction, "line", match.Color));
                }
            }

            // A 2x2 square creates a flying propeller even without a straight
            // three-gem run. Existing straight matches take precedence.
            for (int r = 0; r < board.Length - 1; r++)
            for (int c = 0; c < board[r].Length - 1; c++)
            {
                string value = board[r][c];
                if (!IsGemCell(value) || board[r][c + 1] != value ||
                    board[r + 1][c] != value || board[r + 1][c + 1] != value)
                    continue;
                var square = new List<Point>
                {
                    new Point(r, c), new Point(r, c + 1),
                    new Point(r + 1, c), new Point(r + 1, c + 1),
                };
                bool overlapsLine = false;
                foreach (var point in square)
                foreach (var match in result.Matches)
                    if (Contains(match, point)) overlapsLine = true;
                if (overlapsLine) continue;
                specialPieces.Add(new SpecialPiece(new Point(r, c), null, "square",
                    GetGemColor(value), square));
            }

            result.SpecialPieces = specialPieces;
            return result;
        }

        private static bool Contains(Match match, Point point)
        {
            return match.Direction == MatchDirection.Horizontal
                ? point.Row == match.Start.Row && point.Col >= match.Start.Col && point.Col <= match.End.Col
                : point.Col == match.Start.Col && point.Row >= match.Start.Row && point.Row <= match.End.Row;
        }

        public static bool HasAnyMatch(string[][] board)
        {
            return FindMatches(board).Count > 0;
        }

        public static string CellToString(CellContent content, string color = null)
        {
            if (content == CellContent.EMPTY && color != null)
                return $"GEM_{color}";
            return content.ToString();
        }
    }
}
