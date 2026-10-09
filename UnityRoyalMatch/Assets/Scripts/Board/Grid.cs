// Grid.cs — 2D grid of cells with swap/bounds/neighbors
// Ported from src/game/Grid.ts

using System.Collections.Generic;
using RoyalMatch.Types;

namespace RoyalMatch.Match3
{
    public class Grid
    {
        public readonly int Rows;
        public readonly int Cols;
        public Cell[][] Cells;

        public Grid(int rows, int cols)
        {
            Rows = rows;
            Cols = cols;
            Cells = CreateEmptyGrid();
        }

        private Cell[][] CreateEmptyGrid()
        {
            var grid = new Cell[Rows][];
            for (int r = 0; r < Rows; r++)
            {
                grid[r] = new Cell[Cols];
                for (int c = 0; c < Cols; c++)
                {
                    grid[r][c] = new Cell(CellContent.EMPTY);
                }
            }
            return grid;
        }

        public bool IsValidPosition(int row, int col)
        {
            return row >= 0 && row < Rows && col >= 0 && col < Cols;
        }

        public Cell GetCell(int row, int col)
        {
            if (!IsValidPosition(row, col)) return null;
            return Cells[row][col];
        }

        public void SetCell(int row, int col, Cell cell)
        {
            if (!IsValidPosition(row, col))
                throw new System.IndexOutOfRangeException($"SetCell out of bounds: {row}, {col}");
            Cells[row][col] = cell;
        }

        public bool IsCellEmpty(int row, int col)
        {
            var cell = GetCell(row, col);
            if (cell == null) return false;
            return cell.Content == CellContent.EMPTY || cell.Content == CellContent.VOID;
        }

        public bool HasGem(int row, int col)
        {
            var cell = GetCell(row, col);
            if (cell == null) return false;
            return cell.Content == CellContent.EMPTY && cell.GemColor.HasValue;
        }

        public Point[] GetNeighbors(int row, int col)
        {
            var neighbors = new List<Point>();
            var directions = new Point[]
            {
                new Point(-1, 0),
                new Point(1, 0),
                new Point(0, -1),
                new Point(0, 1),
            };

            foreach (var dir in directions)
            {
                int nr = row + dir.Row;
                int nc = col + dir.Col;
                if (IsValidPosition(nr, nc))
                {
                    neighbors.Add(new Point(nr, nc));
                }
            }
            return neighbors.ToArray();
        }

        public void Swap(int r1, int c1, int r2, int c2)
        {
            Cell cell1 = Cells[r1][c1];
            Cell cell2 = Cells[r2][c2];
            Cells[r1][c1] = cell2;
            Cells[r2][c2] = cell1;
        }

        public Grid Clone()
        {
            var newGrid = new Grid(Rows, Cols);
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    newGrid.Cells[r][c] = this.Cells[r][c].Clone();
                }
            }
            return newGrid;
        }
    }
}
