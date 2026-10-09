// Piece.cs — Gem piece with color, special type, and position
// Ported from src/game/Piece.ts

using RoyalMatch.Types;

namespace RoyalMatch.Match3
{
    public class Piece
    {
        public readonly PieceType Color;
        public SpecialType Special;
        public int Row;
        public int Col;
        public bool IsMoving;
        public bool IsInSpecialArea;
        public float FallDistance;
        public bool Collected;

        public Point Position => new Point(Row, Col);

        public Piece(PieceType color, SpecialType special, int row, int col)
        {
            Color = color;
            Special = special;
            Row = row;
            Col = col;
            IsMoving = false;
            IsInSpecialArea = false;
            FallDistance = 0;
            Collected = false;
        }

        public void SetPosition(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public bool IsSpecialPiece()
        {
            return SpecialTypeExtensions.IsSpecial(Special);
        }

        // Static helper: convert a CellContent to a PieceType color
        public static PieceType CellToColor(CellContent cell)
        {
            if (cell.ToString().StartsWith("GEM_"))
            {
                string colorStr = cell.ToString().Substring(4);
                if (System.Enum.TryParse<PieceType>(colorStr, out var result))
                    return result;
            }
            throw new System.ArgumentException($"Cannot convert cell content to color: {cell}");
        }

        // Static helper: convert a color to CellContent string (e.g. "GEM_RED")
        public static string ColorToCell(PieceType color)
        {
            return $"GEM_{color}";
        }
    }
}
