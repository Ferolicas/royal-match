// LevelGenerator.cs — Deterministic level generator from seed
// Ported from src/levels/LevelGenerator.ts

using System;
using System.Collections.Generic;
using UnityEngine;
using RoyalMatch.Types;
using RoyalMatch.Utils;

namespace RoyalMatch.Levels
{
    public static class LevelGenerator
    {
        private class ObstacleSpec
        {
            public string[] Types;
            public int BlockerCount;
        }

        private static readonly Dictionary<Difficulty, ObstacleSpec> ObstacleSpecs = new Dictionary<Difficulty, ObstacleSpec>
        {
            { Difficulty.EASY, new ObstacleSpec { Types = new[] { "OBSTACLE_BOX" }, BlockerCount = 4 } },
            { Difficulty.NORMAL, new ObstacleSpec { Types = new[] { "OBSTACLE_BOX", "OBSTACLE_CAGE" }, BlockerCount = 8 } },
            { Difficulty.HARD, new ObstacleSpec { Types = new[] { "OBSTACLE_BOX", "OBSTACLE_CAGE", "OBSTACLE_ICE" }, BlockerCount = 16 } },
            { Difficulty.SUPER_HARD, new ObstacleSpec { Types = new[] { "OBSTACLE_BOX", "OBSTACLE_CAGE", "OBSTACLE_ICE", "OBSTACLE_STONE" }, BlockerCount = 24 } },
        };

        private static string[][] CreateEmptyBoard(int rows, int cols)
        {
            var board = new string[rows][];
            for (int r = 0; r < rows; r++)
            {
                board[r] = new string[cols];
                for (int c = 0; c < cols; c++)
                    board[r][c] = "EMPTY";
            }
            return board;
        }

        private static (int obstacleCells, int blockedCount) PlaceObstacles(string[][] board, DeterministicRNG rng, ObstacleSpec spec, int rows, int cols)
        {
            int obstacleCells = 0;
            int blockedCount = 0;

            var positions = new List<(int r, int c)>();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    positions.Add((r, c));
            for (int i = positions.Count - 1; i > 0; i--)
            {
                int j = rng.NextInRange(0, i);
                (positions[i], positions[j]) = (positions[j], positions[i]);
            }
            int count = System.Math.Min(spec.BlockerCount, positions.Count);
            for (int i = 0; i < count; i++)
            {
                var (r, c) = positions[i];
                board[r][c] = rng.PickOne(spec.Types);
                blockedCount++;
                obstacleCells++;
            }

            return (obstacleCells, blockedCount);
        }

        private static bool HasMatchAt(string[][] board, int row, int col)
        {
            string value = board[row][col];
            if (!value.StartsWith("GEM_")) return false;

            int horizontal = 1;
            for (int c = col - 1; c >= 0 && board[row][c] == value; c--) horizontal++;
            for (int c = col + 1; c < board[row].Length && board[row][c] == value; c++) horizontal++;

            int vertical = 1;
            for (int r = row - 1; r >= 0 && board[r][col] == value; r--) vertical++;
            for (int r = row + 1; r < board.Length && board[r][col] == value; r++) vertical++;

            return horizontal >= 3 || vertical >= 3;
        }

        private static bool HasValidMove(string[][] board)
        {
            var directions = new (int dr, int dc)[] { (1, 0), (0, 1) };
            int rows = board.Length;
            int cols = board[0]?.Length ?? 0;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (!board[r][c].StartsWith("GEM_")) continue;
                    foreach (var (dr, dc) in directions)
                    {
                        int nr = r + dr;
                        int nc = c + dc;
                        if (nr < 0 || nr >= rows || nc < 0 || nc >= cols || !board[nr][nc].StartsWith("GEM_")) continue;
                        // Swap
                        var first = board[r][c];
                        var second = board[nr][nc];
                        board[r][c] = board[nr][nc];
                        board[nr][nc] = first;
                        bool createsMatch = HasMatchAt(board, r, c) || HasMatchAt(board, nr, nc);
                        // Swap back
                        board[r][c] = first;
                        board[nr][nc] = second;
                        if (createsMatch) return true;
                    }
                }
            }
            return false;
        }

        private static void FillGems(string[][] board, DeterministicRNG rng, int rows, int cols)
        {
            var gemCells = new List<(int r, int c)>();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (board[r][c] == "EMPTY")
                        gemCells.Add((r, c));

            var gemTypes = PieceTypeExtensions.AllTypes;

            for (int attempt = 0; attempt < 100; attempt++)
            {
                foreach (var (r, c) in gemCells)
                {
                    // Shuffle gem types
                    var candidates = new PieceType[gemTypes.Length];
                    Array.Copy(gemTypes, candidates, gemTypes.Length);
                    for (int i = candidates.Length - 1; i > 0; i--)
                    {
                        int j = rng.NextInRange(0, i);
                        var temp = candidates[i];
                        candidates[i] = candidates[j];
                        candidates[j] = temp;
                    }

                    var color = candidates[0];
                    for (int k = 0; k < candidates.Length; k++)
                    {
                        string value = $"GEM_{candidates[k]}";
                        bool createsMatch =
                            (c >= 2 && board[r][c - 1] == value && board[r][c - 2] == value) ||
                            (r >= 2 && board[r - 1][c] == value && board[r - 2][c] == value) ||
                            (r >= 1 && c >= 1 && board[r - 1][c] == value &&
                             board[r][c - 1] == value && board[r - 1][c - 1] == value);
                        if (!createsMatch)
                        {
                            color = candidates[k];
                            break;
                        }
                    }
                    board[r][c] = $"GEM_{color}";
                }

                if (HasValidMove(board)) return;

                foreach (var (r, c) in gemCells)
                    board[r][c] = "EMPTY";
            }

            throw new InvalidOperationException("Unable to generate a board with at least one valid move");
        }

        private static LevelDataObjective[] GetValidObjectives(int levelNumber, Difficulty difficulty, DeterministicRNG rng, int obstacleCount, int moveLimit)
        {
            return new[] { new LevelDataObjective
            {
                Type = "remove_blocker",
                Description = "Elimina obstáculos",
                Target = obstacleCount,
            }};
        }

        private static (int star1, int star2, int star3) ComputeStarThresholds(int moveLimit, LevelDataObjective[] objectives, Difficulty difficulty)
        {
            int avgPointsPerMove = 80;
            int baseScore = moveLimit * avgPointsPerMove;
            float difficultyBonus = difficulty switch
            {
                Difficulty.EASY => 0.7f,
                Difficulty.NORMAL => 1.0f,
                Difficulty.HARD => 1.3f,
                Difficulty.SUPER_HARD => 1.8f,
                _ => 1.0f,
            };

            int adjusted = (int)System.Math.Round(baseScore * difficultyBonus);
            return (adjusted, (int)System.Math.Round(adjusted * 1.5), (int)System.Math.Round(adjusted * 2.2));
        }

        public static LevelData GenerateLevel(int levelNumber)
        {
            int seed = DifficultySystem.GetSeed(levelNumber);
            var rng = new DeterministicRNG(seed);
            rng.Reset();

            var difficulty = DifficultySystem.GetDifficulty(levelNumber);
            int moveLimit = DifficultySystem.ComputeMoveLimit(levelNumber);
            var spec = ObstacleSpecs[difficulty];

            var board = CreateEmptyBoard(DifficultySystem.GridRows, DifficultySystem.GridCols);

            var (obstacleCells, _) = PlaceObstacles(board, rng, spec, DifficultySystem.GridRows, DifficultySystem.GridCols);
            FillGems(board, rng, DifficultySystem.GridRows, DifficultySystem.GridCols);

            var objectives = GetValidObjectives(levelNumber, difficulty, rng, obstacleCells, moveLimit);
            var (star1, star2, star3) = ComputeStarThresholds(moveLimit, objectives, difficulty);

            string[] prefixes = { "", "", "", "The", "A", "Mystic", "Hidden", "Ancient" };
            string prefix = prefixes[levelNumber % prefixes.Length];
            string[] namePool = difficulty switch
            {
                Difficulty.EASY => new[] { "Fresh Start", "First Steps", "Gem Gathering", "Simple Swap" },
                Difficulty.NORMAL => new[] { "Clearing Skies", "Gem Cascade", "Blocked Path", "Treasure Hunt" },
                Difficulty.HARD => new[] { "Mountain Peak", "Heavy Blocks", "Color Storm", "Tough Challenge" },
                Difficulty.SUPER_HARD => new[] { "Boss Level", "Final Push", "Ultimate Test", "Last Stand" },
                _ => new[] { "Fresh Start" },
            };

            string realmName = DifficultySystem.GetWorldThematicName(levelNumber);
            string title = prefix != ""
                ? $"{prefix} {namePool[(levelNumber - 1) % namePool.Length]}"
                : $"{namePool[(levelNumber - 1) % namePool.Length]} — {realmName}";

            // Determine special piece type for reward (based on difficulty)
            int coins = difficulty switch
            {
                Difficulty.SUPER_HARD => 30,
                Difficulty.HARD => 20,
                Difficulty.EASY => 5,
                _ => 10,
            };
            int stars = difficulty switch
            {
                Difficulty.SUPER_HARD => 3,
                Difficulty.HARD => 2,
                _ => 1,
            };

            string[] unlocks = null;
            if (levelNumber % 50 == 0)
                unlocks = new[] { $"world_{levelNumber / 50 + 1}" };

            var levelData = new LevelData
            {
                LevelNumber = levelNumber,
                Difficulty = difficulty,
                Title = title,
                Description = $"Difficulty: {difficulty} | Realm: {realmName}",
                Grid = new GridData { Cols = DifficultySystem.GridCols, Rows = DifficultySystem.GridRows },
                MoveLimit = moveLimit,
                ScoreStar1 = star1,
                ScoreStar2 = star2,
                ScoreStar3 = star3,
                Objectives = objectives,
                BoardLayout = board,
                Seed = seed,
                Version = LevelDataConstants.SchemaVersion,
                PreplacedSpecials = null,
                Rewards = new RewardsData { Coins = coins, Stars = stars, Unlocks = unlocks },
            };

            if (!levelData.Validate())
                throw new InvalidOperationException($"Level {levelNumber} failed schema validation");

            return levelData;
        }

        // Generate all 1000 levels and write to disk
        public static void GenerateAllLevels(string outputPath)
        {
            for (int i = 1; i <= DifficultySystem.TotalLevels; i++)
            {
                var levelData = GenerateLevel(i);
                System.IO.Directory.CreateDirectory(outputPath);
                string filename = DifficultySystem.GetLevelFilename(i);
                string filePath = System.IO.Path.Combine(outputPath, filename);

                string json = JsonUtility.ToJson(levelData, true);
                System.IO.File.WriteAllText(filePath, json);
            }
        }
    }
}
