using System.Collections.Generic;
using RoyalMatch.Types;

namespace RoyalMatch.Match3
{
    // Resolves a booster swap against the live board before any pieces are removed.
    public static class SpecialComboResolver
    {
        public static List<Point> Resolve(Board board, Piece first, Piece second,
            List<GemMove> flights = null)
        {
            var affected = new HashSet<Point> { first.Position, second.Position };
            var serialized = board.ToSerializedBoard();
            var a = first.Special;
            var b = second.Special;
            var center = second.Position;

            if (a == SpecialType.COLOR_BOMB && b == SpecialType.COLOR_BOMB)
            {
                AddWholeBoard(board, affected);
            }
            else if (a == SpecialType.COLOR_BOMB || b == SpecialType.COLOR_BOMB)
            {
                var other = a == SpecialType.COLOR_BOMB ? second : first;
                var targets = SpecialAbilities.ResolveColorBomb(serialized, other.Color.ToString());
                var usedTargets = new HashSet<Point>();
                foreach (var point in targets)
                {
                    affected.Add(point);
                    if (other.Special != SpecialType.NONE)
                        AddEffect(board, other.Special, point, other.Color, affected, usedTargets, flights);
                }
            }
            else if (IsPropeller(a) || IsPropeller(b))
            {
                var usedTargets = new HashSet<Point>();
                var comboTarget = FindObstacle(board, center, usedTargets);
                AddEffect(board, a, first.Position, second.Color, affected, usedTargets, flights);
                AddEffect(board, b, second.Position, first.Color, affected, usedTargets, flights);
                if (IsPropeller(a) && b != SpecialType.NONE && !IsPropeller(b))
                {
                    if (comboTarget.HasValue)
                        AddEffect(board, b, comboTarget.Value, first.Color, affected, usedTargets, flights);
                }
                else if (IsPropeller(b) && a != SpecialType.NONE && !IsPropeller(a))
                {
                    if (comboTarget.HasValue)
                        AddEffect(board, a, comboTarget.Value, second.Color, affected, usedTargets, flights);
                }
            }
            else if (a == SpecialType.BOMB && b == SpecialType.BOMB)
            {
                AddArea(board, first.Position, 2, affected);
                AddArea(board, second.Position, 2, affected);
            }
            else if ((a == SpecialType.BOMB && IsRocket(b)) ||
                     (b == SpecialType.BOMB && IsRocket(a)))
            {
                for (int offset = -1; offset <= 1; offset++)
                {
                    AddRow(board, center.Row + offset, affected);
                    AddColumn(board, center.Col + offset, affected);
                }
            }
            else if (IsRocket(a) && IsRocket(b))
            {
                AddRow(board, first.Row, affected);
                AddColumn(board, first.Col, affected);
                AddRow(board, second.Row, affected);
                AddColumn(board, second.Col, affected);
            }
            else
            {
                var usedTargets = new HashSet<Point>();
                AddEffect(board, a, first.Position, second.Color, affected, usedTargets, flights);
                AddEffect(board, b, second.Position, first.Color, affected, usedTargets, flights);
            }

            // A blast that reaches another booster activates it in the same turn.
            var activated = new HashSet<Point> { first.Position, second.Position };
            var queue = new Queue<Point>(affected);
            while (queue.Count > 0)
            {
                var point = queue.Dequeue();
                var piece = board.GetPiece(point.Row, point.Col);
                if (piece == null || piece.Special == SpecialType.NONE || !activated.Add(point))
                    continue;
                var before = new HashSet<Point>(affected);
                AddEffect(board, piece.Special, point, piece.Color, affected, new HashSet<Point>(), flights);
                foreach (var added in affected)
                    if (!before.Contains(added)) queue.Enqueue(added);
            }

            return new List<Point>(affected);
        }

        private static bool IsRocket(SpecialType type) =>
            type == SpecialType.ROCKET_HORIZONTAL || type == SpecialType.ROCKET_VERTICAL ||
            type == SpecialType.ROCKET || type == SpecialType.LINE;

        private static bool IsPropeller(SpecialType type) => type == SpecialType.PROPELLER;

        private static void AddEffect(Board board, SpecialType type, Point point, PieceType color,
            HashSet<Point> affected, HashSet<Point> usedTargets, List<GemMove> flights)
        {
            switch (type)
            {
                case SpecialType.ROCKET_HORIZONTAL:
                case SpecialType.ROCKET:
                    AddRow(board, point.Row, affected);
                    break;
                case SpecialType.ROCKET_VERTICAL:
                    AddColumn(board, point.Col, affected);
                    break;
                case SpecialType.LINE:
                    AddRow(board, point.Row, affected);
                    AddColumn(board, point.Col, affected);
                    break;
                case SpecialType.BOMB:
                    AddArea(board, point, 1, affected);
                    break;
                case SpecialType.COLOR_BOMB:
                    foreach (var target in SpecialAbilities.ResolveColorBomb(board.ToSerializedBoard(), color.ToString()))
                        affected.Add(target);
                    break;
                case SpecialType.PROPELLER:
                    affected.Add(point);
                    foreach (var neighbor in board.Grid.GetNeighbors(point.Row, point.Col))
                        affected.Add(neighbor);
                    var obstacle = FindObstacle(board, point, usedTargets);
                    if (obstacle.HasValue)
                    {
                        affected.Add(obstacle.Value);
                        usedTargets.Add(obstacle.Value);
                        flights?.Add(new GemMove(point, obstacle.Value));
                    }
                    break;
            }
        }

        private static Point? FindObstacle(Board board, Point origin, HashSet<Point> excluded)
        {
            Point? best = null;
            int distance = int.MaxValue;
            for (int row = 0; row < board.Config.Rows; row++)
            for (int col = 0; col < board.Config.Cols; col++)
            {
                var point = new Point(row, col);
                var cell = board.Grid.GetCell(row, col);
                if (cell == null || !CellContentExtensions.IsObstacle(cell.Content) || excluded.Contains(point))
                    continue;
                int current = System.Math.Abs(row - origin.Row) + System.Math.Abs(col - origin.Col);
                if (current >= distance) continue;
                best = point;
                distance = current;
            }
            return best;
        }

        private static void AddWholeBoard(Board board, HashSet<Point> affected)
        {
            for (int row = 0; row < board.Config.Rows; row++)
            for (int col = 0; col < board.Config.Cols; col++)
                affected.Add(new Point(row, col));
        }

        private static void AddRow(Board board, int row, HashSet<Point> affected)
        {
            if (row < 0 || row >= board.Config.Rows) return;
            for (int col = 0; col < board.Config.Cols; col++)
                affected.Add(new Point(row, col));
        }

        private static void AddColumn(Board board, int col, HashSet<Point> affected)
        {
            if (col < 0 || col >= board.Config.Cols) return;
            for (int row = 0; row < board.Config.Rows; row++)
                affected.Add(new Point(row, col));
        }

        private static void AddArea(Board board, Point center, int radius, HashSet<Point> affected)
        {
            for (int row = center.Row - radius; row <= center.Row + radius; row++)
            for (int col = center.Col - radius; col <= center.Col + radius; col++)
                if (board.Grid.IsValidPosition(row, col)) affected.Add(new Point(row, col));
        }
    }
}
