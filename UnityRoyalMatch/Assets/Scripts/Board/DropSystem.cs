// DropSystem.cs — Gravity simulation: fills empty cells from above
// Ported from src/game/DropSystem.ts

using System.Collections.Generic;
using RoyalMatch.Types;

namespace RoyalMatch.Match3
{
    public struct DropResult
    {
        public Point[] Moved;
        public Point[] Spawned;
    }

    public struct DropConfig
    {
        public int Cols;
        public int Rows;

        public DropConfig(int cols, int rows)
        {
            Cols = cols;
            Rows = rows;
        }
    }

    public static class DropSystem
    {
        public static Point[] ApplyGravity(string[][] pieceGrid, DropConfig config)
        {
            var moved = new List<Point>();

            for (int c = 0; c < config.Cols; c++)
            {
                int writeRow = config.Rows - 1;

                for (int r = config.Rows - 1; r >= 0; r--)
                {
                    if (pieceGrid[r][c] != null)
                    {
                        if (writeRow != r)
                        {
                            pieceGrid[writeRow][c] = pieceGrid[r][c];
                            pieceGrid[r][c] = null;
                            moved.Add(new Point(writeRow, c));
                        }
                        writeRow--;
                    }
                }
            }

            return moved.ToArray();
        }

        public static Point[] FillFromTop(string[][] pieceGrid, PieceType[] colors, DropConfig config)
        {
            var spawned = new List<Point>();
            int colorIndex = 0;

            for (int c = 0; c < config.Cols; c++)
            {
                for (int r = config.Rows - 1; r >= 0; r--)
                {
                    if (pieceGrid[r][c] == null)
                    {
                        var color = colors[colorIndex % colors.Length];
                        pieceGrid[r][c] = $"GEM_{color}";
                        spawned.Add(new Point(r, c));
                        colorIndex++;
                    }
                }
            }

            return spawned.ToArray();
        }
    }
}
