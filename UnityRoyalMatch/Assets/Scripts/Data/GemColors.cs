// GemColors.cs — Gem color palette definitions
// Ported from src/data/colors.ts

using UnityEngine;

namespace RoyalMatch.Data
{
    [System.Serializable]
    public class GemColorInfo
    {
        public string Name;
        public Color Color;
        public Color Dimmed;
        public Color Emissive;
        public Vector3 HSV;
    }

    public static class GemColors
    {
        public static readonly Color Red = new Color(1.0f, 0.231f, 0.231f);
        public static readonly Color Blue = new Color(0.290f, 0.565f, 1.0f);
        public static readonly Color Green = new Color(0.204f, 0.780f, 0.350f);
        public static readonly Color Yellow = new Color(1.0f, 0.800f, 0.0f);
        public static readonly Color Purple = new Color(0.686f, 0.322f, 0.890f);
        public static readonly Color Cyan = new Color(0.196f, 0.678f, 0.929f);

        public static Color GetColor(Types.PieceType type)
        {
            return type switch
            {
                Types.PieceType.RED => Red,
                Types.PieceType.BLUE => Blue,
                Types.PieceType.GREEN => Green,
                Types.PieceType.YELLOW => Yellow,
                Types.PieceType.PURPLE => Purple,
                Types.PieceType.CYAN => Cyan,
                _ => Color.white,
            };
        }

        public static string GetColorName(Types.PieceType type)
        {
            return type.ToString();
        }

        public static readonly Color BackgroundColor = new Color(0.059f, 0.047f, 0.161f);
        public static readonly Color GridColor = new Color(0.106f, 0.071f, 0.239f);
        public static readonly Color CellInnerColor = new Color(0.086f, 0.129f, 0.306f);
        public static readonly Color UIPanelColor = new Color(0.106f, 0.071f, 0.239f);
        public static readonly Color UIBorderColor = new Color(0.180f, 0.133f, 0.314f);
        public static readonly Color StarGold = new Color(1.0f, 0.843f, 0.0f);
        public static readonly Color StarSilver = new Color(0.753f, 0.753f, 0.753f);
        public static readonly Color StarBronze = new Color(0.804f, 0.498f, 0.196f);
    }
}
