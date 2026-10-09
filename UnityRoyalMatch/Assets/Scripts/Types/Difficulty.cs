// Difficulty.cs — Difficulty enum
// Ported from src/types/Difficulty.ts

namespace RoyalMatch.Types
{
    public enum Difficulty
    {
        EASY,
        NORMAL,
        HARD,
        SUPER_HARD,
    }

    public static class DifficultyExtensions
    {
        public static readonly Difficulty[] All = {
            Difficulty.EASY,
            Difficulty.NORMAL,
            Difficulty.HARD,
            Difficulty.SUPER_HARD,
        };
    }
}
