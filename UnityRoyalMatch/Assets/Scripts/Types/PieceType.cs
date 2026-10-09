// PieceType.cs — Gem color/type enum
// Ported from src/types/PieceType.ts

using System.Collections.Generic;

namespace RoyalMatch.Types
{
    public enum PieceType
    {
        RED,
        BLUE,
        GREEN,
        YELLOW,
        PURPLE,
        CYAN,
    }

    public static class PieceTypeExtensions
    {
        public static readonly PieceType[] AllTypes = {
            PieceType.RED,
            PieceType.BLUE,
            PieceType.GREEN,
            PieceType.YELLOW,
            PieceType.PURPLE,
            PieceType.CYAN,
        };

        public static readonly List<PieceType> AllList = new List<PieceType>
        {
            PieceType.RED,
            PieceType.BLUE,
            PieceType.GREEN,
            PieceType.YELLOW,
            PieceType.PURPLE,
            PieceType.CYAN,
        };

        public static string ToString(PieceType type)
        {
            return type.ToString();
        }
    }
}
