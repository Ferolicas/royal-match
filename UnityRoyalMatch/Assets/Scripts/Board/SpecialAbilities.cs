// SpecialAbilities.cs — Activates special pieces: line clear, bomb, color bomb, rocket
// Ported from src/game/SpecialAbilities.ts

using System.Collections.Generic;
using RoyalMatch.Types;

namespace RoyalMatch.Match3
{
    public class SpecialAbilities
    {
        public static Point[] ResolveLineClear(string[][] board, Point position, string direction)
        {
            var cleared = new List<Point>();
            int rows = board.Length;
            int cols = board[0].Length;

            cleared.Add(position);

            if (direction == "both" || direction == "horizontal")
            {
                for (int c = 0; c < cols; c++)
                {
                    if (c != position.Col)
                        cleared.Add(new Point(position.Row, c));
                }
            }

            if (direction == "both" || direction == "vertical")
            {
                for (int r = 0; r < rows; r++)
                {
                    if (r != position.Row)
                        cleared.Add(new Point(r, position.Col));
                }
            }

            return cleared.ToArray();
        }

        public static Point[] ResolveBomb(string[][] board, Point position, int radius = 1)
        {
            var cleared = new List<Point>();
            int rows = board.Length;
            int cols = board[0].Length;

            for (int dr = -radius; dr <= radius; dr++)
            {
                for (int dc = -radius; dc <= radius; dc++)
                {
                    int r = position.Row + dr;
                    int c = position.Col + dc;
                    if (r >= 0 && r < rows && c >= 0 && c < cols)
                        cleared.Add(new Point(r, c));
                }
            }

            return cleared.ToArray();
        }

        public static Point[] ResolveColorBomb(string[][] board, string color)
        {
            var cleared = new List<Point>();
            int rows = board.Length;
            int cols = board[0].Length;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    string cell = board[r][c];
                    if (cell != null && cell.StartsWith($"GEM_{color}"))
                        cleared.Add(new Point(r, c));
                }
            }

            return cleared.ToArray();
        }

        public static Point[] ResolveRocket(string[][] board, Point position, string direction)
        {
            var cleared = new List<Point>();
            int rows = board.Length;
            int cols = board[0].Length;

            cleared.Add(position);

            if (direction == "horizontal")
            {
                for (int c = 0; c < cols; c++)
                {
                    if (c != position.Col)
                        cleared.Add(new Point(position.Row, c));
                }
            }
            else
            {
                for (int r = 0; r < rows; r++)
                {
                    if (r != position.Row)
                        cleared.Add(new Point(r, position.Col));
                }
            }

            return cleared.ToArray();
        }

        public static Point[] ApplySpecialEffect(string[][] board, SpecialEffect effect)
        {
            switch (effect.Type)
            {
                case "line_clear":
                    return ResolveLineClear(board, effect.Position, effect.Direction ?? "both");
                case "bomb":
                    return ResolveBomb(board, effect.Position, 1);
                case "color_bomb":
                    return ResolveColorBomb(board, effect.Color ?? "");
                case "rocket":
                    return ResolveRocket(board, effect.Position,
                        (effect.Direction != null && effect.Direction != "both") ? effect.Direction : "horizontal");
                default:
                    return new Point[0];
            }
        }

        public static SpecialType MatchToSpecial(int length, string shape)
        {
            if (shape == "square") return SpecialType.PROPELLER;
            if (shape == "L" || shape == "T" || shape == "cross") return SpecialType.BOMB;
            if (length >= 5) return SpecialType.COLOR_BOMB;
            if (length == 4) return SpecialType.ROCKET;
            return SpecialType.NONE;
        }
    }

    public class SpecialEffect
    {
        public string Type; // "line_clear" | "bomb" | "color_bomb" | "rocket"
        public Point Position;
        public string Color;
        public string Direction; // "horizontal" | "vertical" | "both"
    }
}
