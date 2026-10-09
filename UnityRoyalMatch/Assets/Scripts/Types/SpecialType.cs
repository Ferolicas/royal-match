// SpecialType.cs — Special piece enum
// Ported from src/types/SpecialType.ts

namespace RoyalMatch.Types
{
    public enum SpecialType
    {
        NONE,
        LINE,
        BOMB,
        COLOR_BOMB,
        ROCKET,
        ROCKET_HORIZONTAL,
        ROCKET_VERTICAL,
        PROPELLER,
    }

    public static class SpecialTypeExtensions
    {
        public static bool IsSpecial(SpecialType type)
        {
            return type != SpecialType.NONE;
        }
    }
}
