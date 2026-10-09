// Difficulty.cs — Difficulty rules, seed computation, and move limit
// Ported from src/levels/Difficulty.ts + src/data/levelConfig.ts

using RoyalMatch.Types;

namespace RoyalMatch.Levels
{
    public static class DifficultySystem
    {
        public const int TotalLevels = 1000;
        public const int SeedBase = 0x7a4e;
        public const int LevelsPerFolder = 100;
        public const int GridCols = 9;
        public const int GridRows = 11;
        public const int GemTypesCount = 6;

        public const int BaseMoveLimit = 15;
        public const int MinMoveLimit = 6;

        public static Difficulty GetDifficulty(int levelNumber)
        {
            if (levelNumber <= 2) return Difficulty.EASY;
            if (levelNumber % 6 == 0) return Difficulty.SUPER_HARD;
            if (levelNumber % 3 == 0) return Difficulty.HARD;
            return Difficulty.NORMAL;
        }

        public static int GetSeed(int levelNumber)
        {
            return SeedBase + levelNumber;
        }

        public static int ComputeMoveLimit(int levelNumber)
        {
            var diff = GetDifficulty(levelNumber);
            int offset = diff switch
            {
                Difficulty.EASY => 3,
                Difficulty.NORMAL => 0,
                Difficulty.HARD => -2,
                Difficulty.SUPER_HARD => -4,
                _ => 0,
            };
            int moves = BaseMoveLimit + offset;
            return System.Math.Max(MinMoveLimit, moves) * 4;
        }

        public static float GetGoalTargetMultiplier(Difficulty diff)
        {
            return diff switch
            {
                Difficulty.EASY => 0.7f,
                Difficulty.NORMAL => 1.0f,
                Difficulty.HARD => 1.4f,
                Difficulty.SUPER_HARD => 2.0f,
                _ => 1.0f,
            };
        }

        public static float GetObstacleDensity(Difficulty diff)
        {
            return diff switch
            {
                Difficulty.EASY => 0f,
                Difficulty.NORMAL => 0.08f,
                Difficulty.HARD => 0.18f,
                Difficulty.SUPER_HARD => 0.35f,
                _ => 0f,
            };
        }

        public static int GetBlockerCount(Difficulty diff)
        {
            return diff switch
            {
                Difficulty.EASY => 4,
                Difficulty.NORMAL => 8,
                Difficulty.HARD => 16,
                Difficulty.SUPER_HARD => 24,
                _ => 0,
            };
        }

        public static int GetVoidCount(Difficulty diff)
        {
            // Every cell must be playable so obstacle layouts cannot leave dead gaps.
            return 0;
        }

        public static string[] GetObstacleTypes(Difficulty diff)
        {
            return diff switch
            {
                Difficulty.EASY => new[] { "OBSTACLE_BOX" },
                Difficulty.NORMAL => new[] { "OBSTACLE_BOX", "OBSTACLE_CAGE" },
                Difficulty.HARD => new[] { "OBSTACLE_BOX", "OBSTACLE_CAGE", "OBSTACLE_ICE" },
                Difficulty.SUPER_HARD => new[] { "OBSTACLE_BOX", "OBSTACLE_CAGE", "OBSTACLE_ICE", "OBSTACLE_STONE" },
                _ => new[] { "BLOCKED" },
            };
        }

        public static int GetDifficultyBonusMoves(Difficulty diff)
        {
            return diff switch
            {
                Difficulty.EASY => 3,
                Difficulty.NORMAL => 0,
                Difficulty.HARD => -2,
                Difficulty.SUPER_HARD => -4,
                _ => 0,
            };
        }

        public static string GetWorldThematicName(int levelNumber)
        {
            if (levelNumber <= 100) return "Tutorial Island";
            if (levelNumber <= 400) return "Forest Realm";
            if (levelNumber <= 700) return "Mountain Realm";
            return "Castle Realm";
        }

        public static string GetLevelFilename(int levelNumber)
        {
            return $"level-{levelNumber:D4}.json";
        }

        public static string GetFolderRange(int levelNumber)
        {
            int start = ((levelNumber - 1) / LevelsPerFolder) * LevelsPerFolder + 1;
            int end = System.Math.Min(start + LevelsPerFolder - 1, TotalLevels);
            return $"{start:D4}-{end:D4}";
        }

        public static string GetLevelPath(int levelNumber)
        {
            return $"levels/{GetFolderRange(levelNumber)}/{GetLevelFilename(levelNumber)}";
        }
    }
}
