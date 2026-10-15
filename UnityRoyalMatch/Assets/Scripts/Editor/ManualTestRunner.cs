// ManualTestRunner.cs — Editor script to run game tests programmatically
// Bypasses Unity Test Framework for batch-mode reliability

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using RoyalMatch.Levels;

namespace RoyalMatch.EditorTools
{
    public static class ManualTestRunner
    {
        private static readonly RoyalMatch.Match3.BoardConfig LegacyBoardConfig =
            new RoyalMatch.Match3.BoardConfig { Rows = 8, Cols = 8, GemTypes = 6, MinMatch = 3 };
        private static int _passed = 0;
        private static int _failed = 0;
        private static int _total = 0;

        [MenuItem("Royal Match/Run Game Tests")]
        public static void RunAll()
        {
            _passed = 0;
            _failed = 0;
            _total = 0;

            Debug.Log("=== Starting Game Tests ===");

            // Run Board tests
            RunTest("Board_Initialization", TestBoard_Initialization);
            RunTest("Board_SwapPieces", TestBoard_SwapPieces);
            RunTest("MatchFinder_HorizontalMatch", TestMatchFinder_HorizontalMatch);
            RunTest("MatchFinder_VerticalMatch", TestMatchFinder_VerticalMatch);
            RunTest("MatchFinder_NoMatchOnCleanBoard", TestMatchFinder_NoMatchOnCleanBoard);
            RunTest("Board_RemoveMatches", TestBoard_RemoveMatches);
            RunTest("Board_ApplyGravity", TestBoard_ApplyGravity);

            // Run GameManager tests
            RunTest("GameManager_SwapConsumesMove", TestGameManager_SwapConsumesMove);
            RunTest("GameManager_RejectsNonAdjacent", TestGameManager_RejectsNonAdjacent);
            RunTest("GameManager_ActivatesSpecial", TestGameManager_ActivatesSpecial);

            // Run Score tests
            RunTest("Scoring_CascadeScore", TestScoring_CascadeScore);
            RunTest("GameState_StarsEarned", TestGameState_StarsEarned);

            // Run LevelGenerator tests
            RunTest("LevelGenerator_CreatesValidLevel", TestLevelGenerator_CreatesValidLevel);
            RunTest("LevelGenerator_Deterministic", TestLevelGenerator_Deterministic);

            // Run SpecialAbilities tests
            RunTest("SpecialAbilities_Bomb", TestSpecialAbilities_Bomb);
            RunTest("SpecialAbilities_LineClear", TestSpecialAbilities_LineClear);
            RunTest("SpecialAbilities_ColorBomb", TestSpecialAbilities_ColorBomb);

            // Run DeterministicRNG tests
            RunTest("RNG_Deterministic", TestRNG_Deterministic);

            RunTest("LevelCatalog_FirstLevel", TestLevelCatalog_FirstLevel);
            RunTest("LevelCatalog_All14700Levels", TestLevelCatalog_All14700Levels);

            Debug.Log($"=== Test Results ===");
            Debug.Log($"Total: {_total}, Passed: {_passed}, Failed: {_failed}");

            if (_failed > 0)
                Debug.LogError($"TESTS FAILED: {_failed} out of {_total}");
            else
                Debug.Log("ALL TESTS PASSED!");

            EditorApplication.Exit(_failed > 0 ? 1 : 0);
        }

        private static void RunTest(string name, Action test)
        {
            _total++;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                test();
                sw.Stop();
                _passed++;
                Debug.Log($"[PASS] {name} ({sw.ElapsedMilliseconds}ms)");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _failed++;
                Debug.LogError($"[FAIL] {name}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void Assert(bool condition, string message = "")
        {
            if (!condition)
                throw new Exception($"Assertion failed: {message}");
        }

        #region Board Tests
        private static void TestBoard_Initialization()
        {
            var board = new RoyalMatch.Match3.Board(LegacyBoardConfig);
            Assert(board.Config.Rows == 8, "Rows should be 8");
            Assert(board.Config.Cols == 8, "Cols should be 8");
            Assert(board.Config.GemTypes == 6, "GemTypes should be 6");
            Assert(board.Config.MinMatch == 3, "MinMatch should be 3");
        }

        private static void TestBoard_SwapPieces()
        {
            var board = new RoyalMatch.Match3.Board(LegacyBoardConfig);
            var layout = CreateCleanLayout();
            board.InitializeFromLayout(layout);

            var pieceBefore1 = board.GetPiece(0, 0);
            var pieceBefore2 = board.GetPiece(0, 1);

            board.SwapPieces(0, 0, 0, 1);

            Assert(board.GetPiece(0, 0) == pieceBefore2, "Piece at (0,0) should be the old (0,1)");
            Assert(board.GetPiece(0, 1) == pieceBefore1, "Piece at (0,1) should be the old (0,0)");
        }

        private static void TestMatchFinder_HorizontalMatch()
        {
            var board = new RoyalMatch.Match3.Board(LegacyBoardConfig);
            var layout = CreateHorizontalMatchLayout();
            board.InitializeFromLayout(layout);

            var (matches, specials) = board.FindMatches();
            Assert(matches.Count > 0, "Should find at least one match");
            var hMatch = matches.Find(m => m.Direction == RoyalMatch.Types.MatchDirection.Horizontal);
            Assert(hMatch != null, "Should find horizontal match");
            Assert(hMatch.Length == 3, $"Match length should be 3, got {hMatch.Length}");
        }

        private static void TestMatchFinder_VerticalMatch()
        {
            var board = new RoyalMatch.Match3.Board(LegacyBoardConfig);
            var layout = CreateVerticalMatchLayout();
            board.InitializeFromLayout(layout);

            var (matches, _) = board.FindMatches();
            Assert(matches.Count > 0, "Should find at least one match");
            var vMatch = matches.Find(m => m.Direction == RoyalMatch.Types.MatchDirection.Vertical);
            Assert(vMatch != null, "Should find vertical match");
            Assert(vMatch.Length == 3, $"Match length should be 3, got {vMatch.Length}");
        }

        private static void TestMatchFinder_NoMatchOnCleanBoard()
        {
            var board = new RoyalMatch.Match3.Board(LegacyBoardConfig);
            var layout = CreateCleanLayout();
            board.InitializeFromLayout(layout);

            var (matches, _) = board.FindMatches();
            Assert(matches.Count == 0, "Should find no matches on clean board");
        }

        private static void TestBoard_RemoveMatches()
        {
            var board = new RoyalMatch.Match3.Board(LegacyBoardConfig);
            var layout = CreateHorizontalMatchLayout();
            board.InitializeFromLayout(layout);

            var (matches, _) = board.FindMatches();
            var destroyed = board.RemoveMatches(matches);
            Assert(destroyed.Count == 3, $"Should destroy 3 pieces, got {destroyed.Count}");
            Assert(board.GetPiece(0, 0) == null, "Piece at (0,0) should be destroyed");
            Assert(board.GetPiece(0, 1) == null, "Piece at (0,1) should be destroyed");
            Assert(board.GetPiece(0, 2) == null, "Piece at (0,2) should be destroyed");
        }

        private static void TestBoard_ApplyGravity()
        {
            var board = new RoyalMatch.Match3.Board(LegacyBoardConfig);
            var layout = CreateCleanLayout();
            board.InitializeFromLayout(layout);

            board.ClearPositions(new System.Collections.Generic.List<RoyalMatch.Types.Point> {
                new RoyalMatch.Types.Point(0, 0),
                new RoyalMatch.Types.Point(1, 0)
            });

            var moved = board.ApplyGravity();
            Assert(moved.Count > 0, "Pieces should have moved");
            Assert(board.GetPiece(0, 0) != null, "Piece should have fallen to row 0");
        }
        #endregion

        #region GameManager Tests
        private static void TestGameManager_SwapConsumesMove()
        {
            var manager = CreateTestManager(100, 5);
            var state = manager.GetState();
            int initialMoves = state.MovesRemaining;

            var (from, to) = FindValidSwap(manager);
            bool result = manager.TrySwap(from.Row, from.Col, to.Row, to.Col);

            Assert(result, "Swap should succeed");
            Assert(state.MovesRemaining == initialMoves - 1, "Move should be consumed");
            Assert(state.Score > 0, "Score should increase");
        }

        private static void TestGameManager_RejectsNonAdjacent()
        {
            var manager = CreateTestManager(999999, 5);
            var state = manager.GetState();

            bool result = manager.TrySwap(0, 0, 2, 0);
            Assert(!result, "Non-adjacent swap should fail");
            Assert(state.MovesRemaining == 5, "Move should not be consumed");
        }

        private static void TestGameManager_ActivatesSpecial()
        {
            var manager = CreateTestManager(100, 3);
            var board = manager.GetBoard();
            board.SetSpecial(0, 0, RoyalMatch.Types.SpecialType.BOMB);
            var state = manager.GetState();

            bool result = manager.TrySwap(0, 0, 0, 1);
            Assert(result, "Special swap should succeed");
            Assert(state.MovesRemaining == 2, "Move should be consumed");
            Assert(state.Score >= 180, $"Special should award >= 180 points, got {state.Score}");
        }
        #endregion

        #region Scoring Tests
        private static void TestScoring_CascadeScore()
        {
            var matches = new System.Collections.Generic.List<RoyalMatch.Types.Match>
            {
                new RoyalMatch.Types.Match(
                    new RoyalMatch.Types.Point(0, 0),
                    new RoyalMatch.Types.Point(0, 2),
                    3, RoyalMatch.Types.MatchDirection.Horizontal, "RED"),
            };

            var (points, _) = RoyalMatch.Match3.Scoring.CalculateCascadeScore(matches, 1);
            Assert(points == 150, $"Score should be 150, got {points}");
        }

        private static void TestGameState_StarsEarned()
        {
            var state = new RoyalMatch.Core.GameStateData
            {
                Star1 = 1000, Star2 = 2000, Star3 = 3000,
                Score = 2500
            };

            int stars = RoyalMatch.Core.GameState.GetStarsEarned(state);
            Assert(stars == 2, $"Should get 2 stars, got {stars}");
        }
        #endregion

        #region LevelGenerator Tests
        private static void TestLevelGenerator_CreatesValidLevel()
        {
            var level = RoyalMatch.Levels.LevelGenerator.GenerateLevel(1);

            Assert(level.LevelNumber == 1, "Level number should be 1");
            Assert(level.Grid != null, "Grid should not be null");
            Assert(level.Grid.Rows > 0, "Rows should be > 0");
            Assert(level.Grid.Cols > 0, "Cols should be > 0");
            Assert(level.BoardLayout != null, "BoardLayout should not be null");
            Assert(level.Validate(), "Level should validate");
        }

        private static void TestLevelGenerator_Deterministic()
        {
            var level1 = RoyalMatch.Levels.LevelGenerator.GenerateLevel(1);
            var level2 = RoyalMatch.Levels.LevelGenerator.GenerateLevel(1);

            Assert(level1.Seed == level2.Seed, "Same seed for same level number");
            Assert(level1.BoardLayout.Length == level2.BoardLayout.Length, "Layout length should match");
        }
        #endregion

        #region SpecialAbilities Tests
        private static void TestSpecialAbilities_Bomb()
        {
            string[][] board = CreateCleanBoardString();
            var affected = RoyalMatch.Match3.SpecialAbilities.ResolveBomb(board, new RoyalMatch.Types.Point(3, 4));
            Assert(affected.Length > 0, "Bomb should affect some cells");
        }

        private static void TestSpecialAbilities_LineClear()
        {
            string[][] board = CreateCleanBoardString();
            var affected = RoyalMatch.Match3.SpecialAbilities.ResolveLineClear(board, new RoyalMatch.Types.Point(3, 3), "both");
            Assert(affected.Length > 0, "Line clear should affect cells");
            bool hasRow3 = System.Array.Exists(affected, p => p.Row == 3);
            bool hasCol3 = System.Array.Exists(affected, p => p.Col == 3);
            Assert(hasRow3, "Line clear should affect row 3");
            Assert(hasCol3, "Line clear should affect col 3");
        }

        private static void TestSpecialAbilities_ColorBomb()
        {
            string[][] board = CreateCleanBoardString();
            board[0][0] = "GEM_BLUE";
            board[1][0] = "GEM_BLUE";
            board[2][0] = "GEM_BLUE";

            var affected = RoyalMatch.Match3.SpecialAbilities.ResolveColorBomb(board, "BLUE");
            // Color bomb should affect ALL blue cells (32 in 8x8 alternating board + 1 extra from manual placement = 33)
            Assert(affected.Length == 33, $"Color bomb should affect 33 BLUE cells, got {affected.Length}");
        }
        #endregion

        #region RNG Tests
        private static void TestRNG_Deterministic()
        {
            var rng1 = new RoyalMatch.Utils.DeterministicRNG(42);
            var rng2 = new RoyalMatch.Utils.DeterministicRNG(42);

            for (int i = 0; i < 100; i++)
            {
                int a = rng1.NextInRange(0, 100);
                int b = rng2.NextInRange(0, 100);
                Assert(a == b, $"RNG values should match at iteration {i}");
            }
        }
        #endregion

        #region Helpers
        private const int ROWS = 8;
        private const int COLS = 8;

        private static string[][] CreateCleanLayout()
        {
            var layout = new string[ROWS][];
            for (int r = 0; r < ROWS; r++)
            {
                layout[r] = new string[COLS];
                for (int c = 0; c < COLS; c++)
                {
                    layout[r][c] = (r + c) % 2 == 0 ? "GEM_BLUE" : "GEM_GREEN";
                }
            }
            return layout;
        }

        private static string[][] CreateHorizontalMatchLayout()
        {
            var layout = new string[ROWS][];
            for (int r = 0; r < ROWS; r++)
            {
                layout[r] = new string[COLS];
                for (int c = 0; c < COLS; c++)
                {
                    if (r == 0 && (c == 0 || c == 1 || c == 2))
                        layout[r][c] = "GEM_RED";
                    else if ((r + c) % 2 == 0)
                        layout[r][c] = "GEM_BLUE";
                    else
                        layout[r][c] = "GEM_GREEN";
                }
            }
            return layout;
        }

        private static string[][] CreateVerticalMatchLayout()
        {
            var layout = new string[ROWS][];
            for (int r = 0; r < ROWS; r++)
            {
                layout[r] = new string[COLS];
                for (int c = 0; c < COLS; c++)
                {
                    if (c == 0 && (r == 0 || r == 1 || r == 2))
                        layout[r][c] = "GEM_RED";
                    else if ((r + c) % 2 == 0)
                        layout[r][c] = "GEM_BLUE";
                    else
                        layout[r][c] = "GEM_GREEN";
                }
            }
            return layout;
        }

        private static string[][] CreateCleanBoardString()
        {
            var board = new string[ROWS][];
            for (int r = 0; r < ROWS; r++)
            {
                board[r] = new string[COLS];
                for (int c = 0; c < COLS; c++)
                {
                    board[r][c] = (r + c) % 2 == 0 ? "GEM_BLUE" : "GEM_GREEN";
                }
            }
            return board;
        }

        private static RoyalMatch.Core.GameManager CreateTestManager(int targetScore, int moves)
        {
            var level = RoyalMatch.Levels.LevelGenerator.GenerateLevel(1);
            var state = new RoyalMatch.Core.GameStateData
            {
                LevelNumber = level.LevelNumber,
                Difficulty = level.Difficulty,
                GridRows = level.Grid.Rows,
                GridCols = level.Grid.Cols,
                MoveLimit = moves,
                MovesRemaining = moves,
                Score = 0,
                BoardLayout = level.BoardLayout,
                Objectives = new System.Collections.Generic.List<RoyalMatch.Levels.RuntimeObjective>(),
                Star1 = level.ScoreStar1,
                Star2 = level.ScoreStar2,
                Star3 = level.ScoreStar3,
                Status = RoyalMatch.Core.GameStatus.WAITING_INPUT
            };
            state.Objectives.Add(new RoyalMatch.Levels.RuntimeObjective(
                RoyalMatch.Types.ObjectiveType.REACH_SCORE, "Score", targetScore));

            return new RoyalMatch.Core.GameManager(state);
        }

        private static (RoyalMatch.Types.Point from, RoyalMatch.Types.Point to) FindValidSwap(RoyalMatch.Core.GameManager manager)
        {
            var board = manager.GetBoard();
            var directions = new (int dr, int dc)[] { (1, 0), (0, 1) };

            for (int row = 0; row < board.Config.Rows; row++)
            {
                for (int col = 0; col < board.Config.Cols; col++)
                {
                    if (board.GetPiece(row, col) == null) continue;
                    foreach (var (dr, dc) in directions)
                    {
                        int nr = row + dr;
                        int nc = col + dc;
                        if (nr >= board.Config.Rows || nc >= board.Config.Cols) continue;
                        if (board.GetPiece(nr, nc) == null) continue;

                        board.SwapPieces(row, col, nr, nc);
                        var (matches, _) = board.FindMatches();
                        bool hasMatch = matches.Count > 0;
                        board.SwapPieces(row, col, nr, nc);

                        if (hasMatch)
                            return (new RoyalMatch.Types.Point(row, col), new RoyalMatch.Types.Point(nr, nc));
                    }
                }
            }
            throw new System.Exception("Expected level to contain a valid swap");
        }
        #endregion

        #region Level Catalog Tests

        private static void TestLevelCatalog_FirstLevel()
        {
            var level = LevelGenerator.GenerateLevel(1);
            Assert(level.Grid.Cols == DifficultySystem.GridCols &&
                level.Grid.Rows == DifficultySystem.GridRows, "Board should be 9x11");
            Assert(level.Difficulty == RoyalMatch.Types.Difficulty.EASY, "First level should be easy");
            Assert(level.ColorCount == 4 && level.MoveLimit <= 35, "First level tuning");
            Assert(level.Validate(), "First level should validate");
        }

        private static void TestLevelCatalog_All14700Levels()
        {
            for (int number = 1; number <= DifficultySystem.TotalLevels; number++)
            {
                var level = LevelGenerator.GenerateLevel(number);
                Assert(level.Validate(), $"Level {number} failed schema validation");
                Assert(level.Difficulty == DifficultySystem.GetDifficulty(number),
                    $"Level {number} has the wrong difficulty");
                var board = new RoyalMatch.Match3.Board(new RoyalMatch.Match3.BoardConfig
                {
                    Rows = level.Grid.Rows, Cols = level.Grid.Cols,
                    GemTypes = level.ColorCount, MinMatch = 3,
                });
                board.InitializeFromLayout(level.BoardLayout);
                Assert(board.FindMatches().matches.Count == 0, $"Level {number} starts with a match");
                Assert(board.HasValidMove(), $"Level {number} has no first move");
            }
        }

        #endregion
    }
}
