// PieceDefinitions.cs — Piece definitions (color → hex, sprite info, score value)
// Ported from src/data/pieces.ts

using UnityEngine;

namespace RoyalMatch.Data
{
    [System.Serializable]
    public class PieceDefinition
    {
        public string Type;
        public Types.PieceType Color;
        public int BaseScore;
        public int SpecialScore;
    }

    [System.Serializable]
    public class SpecialPieceDefinition
    {
        public string Name;
        public int Score;
        public int Radius;
        public string ColorEffect;
    }

    public static class PieceDefinitions
    {
        public static readonly PieceDefinition[] AllPieces = {
            new PieceDefinition { Type = "gem", Color = Types.PieceType.RED, BaseScore = 60, SpecialScore = 250 },
            new PieceDefinition { Type = "gem", Color = Types.PieceType.BLUE, BaseScore = 60, SpecialScore = 180 },
            new PieceDefinition { Type = "gem", Color = Types.PieceType.GREEN, BaseScore = 60, SpecialScore = 120 },
            new PieceDefinition { Type = "gem", Color = Types.PieceType.YELLOW, BaseScore = 60, SpecialScore = 200 },
            new PieceDefinition { Type = "gem", Color = Types.PieceType.PURPLE, BaseScore = 60, SpecialScore = 180 },
            new PieceDefinition { Type = "gem", Color = Types.PieceType.CYAN, BaseScore = 60, SpecialScore = 250 },
        };

        public static readonly SpecialPieceDefinition LineClear = new SpecialPieceDefinition { Name = "Line Clear", Score = 120, ColorEffect = "white" };
        public static readonly SpecialPieceDefinition Bomb = new SpecialPieceDefinition { Name = "Bomb", Score = 180, Radius = 1 };
        public static readonly SpecialPieceDefinition ColorBomb = new SpecialPieceDefinition { Name = "Color Bomb", Score = 250 };
        public static readonly SpecialPieceDefinition Rocket = new SpecialPieceDefinition { Name = "Rocket", Score = 200 };

        public static PieceDefinition GetPiece(Types.PieceType color)
        {
            foreach (var p in AllPieces)
                if (p.Color == color) return p;
            return AllPieces[0];
        }
    }
}
