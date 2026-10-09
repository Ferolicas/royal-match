// CellValueUtils.cs — Helpers for converting between cell value strings and enums
// In TypeScript, CellValue strings like "GEM_RED", "OBSTACLE_BOX" map to CellContent.
// In C#, we use enums with string conversion utilities.

using System;
using RoyalMatch.Types;

namespace RoyalMatch.Match3
{
    public static class CellValueUtils
    {
        public static bool IsGemCell(string cellValue)
        {
            return cellValue.StartsWith("GEM_");
        }

        public static PieceType ParseGemColor(string cellValue)
        {
            if (!cellValue.StartsWith("GEM_"))
                throw new ArgumentException($"Not a gem cell: {cellValue}");

            string colorStr = cellValue.Substring(4);
            if (Enum.TryParse<PieceType>(colorStr, out var result))
                return result;
            throw new ArgumentException($"Unknown gem color: {colorStr}");
        }

        public static string GemCell(PieceType color)
        {
            return $"GEM_{color}";
        }

        public static bool TryParseCellContent(string cellValue, out CellContent content)
        {
            if (Enum.TryParse<CellContent>(cellValue, out content))
                return true;
            content = CellContent.EMPTY;
            return false;
        }

        public static string CellContentToString(CellContent content)
        {
            return content.ToString();
        }
    }
}
