// GameE2ETests.cs — End-to-end tests for game mechanics
// Verifies swaps, matches, scoring, cascades, win/lose conditions, and touch interactions

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RoyalMatch.Core;
using RoyalMatch.Match3;
using RoyalMatch.Types;
using RoyalMatch.Levels;
using RoyalMatch.Utils;

namespace RoyalMatch.Tests.E2E
{
    [TestFixture]
    public class GameE2ETests
    {
        private const int ROWS = 8;
        private const int COLS = 8;
        private static readonly BoardConfig LegacyConfig = new BoardConfig
        {
            Rows = ROWS, Cols = COLS, GemTypes = 6, MinMatch = 3,
        };

        // Helper to create a board with a guaranteed match
        private Board CreateBoardWithMatch()
        {
            var board = new Board(LegacyConfig);
            var layout = CreateHorizontalMatchLayout();
            board.InitializeFromLayout(layout);
            return board;
        }

        private string[][] CreateHorizontalMatchLayout()
        {
            var layout = new string[ROWS][];
            for (int r = 0; r < ROWS; r++)
            {
                layout[r] = new string[COLS];
                for (int c = 0; c < COLS; c++)
                {
                    if (r == 0 && (c == 0 || c == 1 || c == 2))
                    {
                        layout[r][c] = "GEM_RED";
                    }
                    else if ((r + c) % 2 == 0)
                    {
                        layout[r][c] = "GEM_BLUE";
                    }
                    else
                    {
                        layout[r][c] = "GEM_GREEN";
                    }
                }
            }
            return layout;
        }

        private string[][] CreateCleanLayout()
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

        private GameManager CreateManager(int targetScore, int moves)
        {
            var level = LevelGenerator.GenerateLevel(1);
            var state = new GameStateData
            {
                LevelNumber = level.LevelNumber,
                Difficulty = level.Difficulty,
                GridRows = level.Grid.Rows,
                GridCols = level.Grid.Cols,
                MoveLimit = moves,
                MovesRemaining = moves,
                Score = 0,
                ScoreAdded = 0,
                Status = GameStatus.WAITING_INPUT,
                Star1 = level.ScoreStar1,
                Star2 = level.ScoreStar2,
                Star3 = level.ScoreStar3,
                BoardLayout = level.BoardLayout,
                Objectives = new List<RuntimeObjective>(),
            };
            state.Objectives.Add(new RuntimeObjective(ObjectiveType.REACH_SCORE, "Score", targetScore));
            return new GameManager(state);
        }

        private (Point from, Point to) FindValidSwap(GameManager manager)
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
                            return (new Point(row, col), new Point(nr, nc));
                    }
                }
            }
            throw new System.Exception("Expected level to contain a valid swap");
        }

        [Test]
        public void BoardInitialization_ShouldCreateCorrectDimensions()
        {
            var board = new Board(LegacyConfig);
            Assert.That(board.Config.Rows, Is.EqualTo(8));
            Assert.That(board.Config.Cols, Is.EqualTo(8));
            Assert.That(board.Config.GemTypes, Is.EqualTo(6));
            Assert.That(board.Config.MinMatch, Is.EqualTo(3));
        }

        [Test]
        public void BoardInitFromLayout_ShouldLoadPiecesCorrectly()
        {
            var board = new Board(LegacyConfig);
            var layout = CreateHorizontalMatchLayout();
            board.InitializeFromLayout(layout);

            var piece = board.GetPiece(0, 0);
            Assert.IsNotNull(piece);
            Assert.That(piece.Color, Is.EqualTo(PieceType.RED));
        }

        [Test]
        public void BoardSwap_ShouldExchangePieces()
        {
            var board = new Board(LegacyConfig);
            var layout = CreateCleanLayout();
            board.InitializeFromLayout(layout);

            var pieceBefore00 = board.GetPiece(0, 0);
            var pieceBefore01 = board.GetPiece(0, 1);

            board.SwapPieces(0, 0, 0, 1);

            Assert.AreSame(pieceBefore01, board.GetPiece(0, 0));
            Assert.AreSame(pieceBefore00, board.GetPiece(0, 1));
        }

        [Test]
        public void MatchFinder_ShouldDetectHorizontalMatch()
        {
            var board = new Board(LegacyConfig);
            var layout = CreateHorizontalMatchLayout();
            board.InitializeFromLayout(layout);

            var (matches, _) = board.FindMatches();
            Assert.That(matches.Count, Is.GreaterThan(0));
            var horizontal = matches.Find(m => m.Direction == MatchDirection.Horizontal);
            Assert.IsNotNull(horizontal);
            Assert.That(horizontal.Length, Is.EqualTo(3));
        }

        [Test]
        public void MatchFinder_ShouldDetectNoMatchesOnCleanBoard()
        {
            var board = new Board(LegacyConfig);
            var layout = CreateCleanLayout();
            board.InitializeFromLayout(layout);

            var (matches, _) = board.FindMatches();
            Assert.That(matches.Count, Is.EqualTo(0));
        }

        [Test]
        public void BoardRemoveMatches_ShouldDestroyMatchedPieces()
        {
            var board = new Board(LegacyConfig);
            var layout = CreateHorizontalMatchLayout();
            board.InitializeFromLayout(layout);

            var (matches, _) = board.FindMatches();
            int destroyedCount = 0;
            foreach (var match in matches)
            {
                if (match.Direction == MatchDirection.Horizontal)
                {
                    for (int c = match.Start.Col; c <= match.End.Col; c++)
                    {
                        if (board.GetPiece(match.Start.Row, c) != null) destroyedCount++;
                    }
                }
            }

            var destroyed = board.RemoveMatches(matches);
            Assert.That(destroyed.Count, Is.EqualTo(3));
            Assert.IsNull(board.GetPiece(0, 0));
            Assert.IsNull(board.GetPiece(0, 1));
            Assert.IsNull(board.GetPiece(0, 2));
        }

        [Test]
        public void Gravity_ShouldDropPiecesDown()
        {
            var board = new Board(LegacyConfig);
            var layout = CreateCleanLayout();
            board.InitializeFromLayout(layout);

            // Clear positions to create gaps
            board.ClearPositions(new List<Point> { new Point(0, 0), new Point(1, 0) });

            var moved = board.ApplyGravity();
            Assert.That(moved.Count, Is.GreaterThan(0));
            Assert.IsNotNull(board.GetPiece(0, 0));
            Assert.IsNotNull(board.GetPiece(1, 0));
        }

        [Test]
        public void FillFromTop_ShouldSpawnNewPieces()
        {
            var board = new Board(LegacyConfig);
            var layout = CreateCleanLayout();
            board.InitializeFromLayout(layout);

            // Set piece to null to create a gap
            board.SetPiece(7, 0, null);
            Assert.IsNull(board.GetPiece(7, 0));

            var spawned = board.FillFromTop();
            Assert.That(spawned.Count, Is.EqualTo(1));
            Assert.IsNotNull(board.GetPiece(7, 0));
        }

        [Test]
        public void GameManager_TrySwap_ShouldConsumeOneMove()
        {
            var manager = CreateManager(100, 5);
            var state = manager.GetState();
            int initialMoves = state.MovesRemaining;

            var (from, to) = FindValidSwap(manager);
            bool result = manager.TrySwap(from.Row, from.Col, to.Row, to.Col);

            Assert.IsTrue(result);
            Assert.That(state.MovesRemaining, Is.EqualTo(initialMoves - 1));
            Assert.That(state.Score, Is.GreaterThan(0));
        }

        [Test]
        public void GameManager_TrySwap_ShouldRejectNonAdjacentSwap()
        {
            var manager = CreateManager(999999, 5);
            var state = manager.GetState();

            bool result = manager.TrySwap(0, 0, 2, 0);
            Assert.IsFalse(result);
            Assert.That(state.MovesRemaining, Is.EqualTo(5));
        }

        [Test]
        public void GameManager_TrySwap_ShouldRevertIfNoMatch()
        {
            var board = new Board(LegacyConfig);
            var layout = CreateCleanLayout();
            board.InitializeFromLayout(layout);

            var state = new GameStateData
            {
                LevelNumber = 1,
                GridRows = 8,
                GridCols = 8,
                MoveLimit = 3,
                MovesRemaining = 3,
                Score = 0,
                BoardLayout = layout,
                Status = GameStatus.WAITING_INPUT,
                Star1 = 1000, Star2 = 2000, Star3 = 3000,
                Objectives = new List<RuntimeObjective>()
            };

            var manager = new GameManager(state);
            var pieceBefore1 = board.GetPiece(0, 0);
            var pieceBefore2 = board.GetPiece(0, 1);

            bool result = manager.TrySwap(0, 0, 0, 1);
            Assert.IsFalse(result);

            // Pieces should be reverted
            Assert.AreSame(pieceBefore1, board.GetPiece(0, 0));
            Assert.AreSame(pieceBefore2, board.GetPiece(0, 1));

            // Move should not be consumed
            Assert.That(state.MovesRemaining, Is.EqualTo(3));
        }

        [Test]
        public void GameManager_State_ShouldTransitionToResolving()
        {
            var manager = CreateManager(100, 3);
            var state = manager.GetState();
            var (from, to) = FindValidSwap(manager);

            manager.TrySwap(from.Row, from.Col, to.Row, to.Col);

            // After a successful swap with matches, status should be RESOLVING or WIN
            Assert.That(state.Status == GameStatus.RESOLVING || state.Status == GameStatus.WIN);
        }

        [Test]
        public void GameManager_Scoring_ShouldAwardPoints()
        {
            var manager = CreateManager(1, 5);
            var state = manager.GetState();
            int initialScore = state.Score;

            var (from, to) = FindValidSwap(manager);
            manager.TrySwap(from.Row, from.Col, to.Row, to.Col);

            Assert.That(state.Score, Is.GreaterThan(initialScore));
        }

        [Test]
        public void GameManager_WinCondition_ShouldTriggerWithHighTarget()
        {
            // Use a low target score so we win quickly
            var manager = CreateManager(100, 10);
            var state = manager.GetState();

            var (from, to) = FindValidSwap(manager);
            manager.TrySwap(from.Row, from.Col, to.Row, to.Col);

            // After resolving, score should be > target (100)
            Assert.That(state.Score, Is.GreaterThanOrEqualTo(100));
        }

        [Test]
        public void GameManager_LoseCondition_ShouldTriggerWhenMovesExhausted()
        {
            var manager = CreateManager(999999, 1);
            var state = manager.GetState();

            var (from, to) = FindValidSwap(manager);
            manager.TrySwap(from.Row, from.Col, to.Row, to.Col);

            // With only 1 move, after swap, should lose
            Assert.That(state.MovesRemaining, Is.EqualTo(0));
            Assert.That(state.Status, Is.EqualTo(GameStatus.LOSE));
        }

        [Test]
        public void GameManager_HasValidMove_ShouldReturnTrueForActiveBoard()
        {
            var manager = CreateManager(100, 10);
            var board = manager.GetBoard();

            bool hasMove = board.HasValidMove();
            // Generated levels should have at least one valid move
            Assert.IsTrue(hasMove);
        }

        [Test]
        public void GameManager_AnimationSteps_ShouldContainStepsAfterSwap()
        {
            var manager = CreateManager(100, 5);

            var (from, to) = FindValidSwap(manager);
            manager.TrySwap(from.Row, from.Col, to.Row, to.Col);

            var steps = manager.GetAnimationSteps();
            Assert.That(steps.Count, Is.GreaterThan(0));
        }

        [Test]
        public void AnimationSteps_ReconstructResolvedBoardAfterSwap()
        {
            var manager = CreateManager(10000, 10);
            var board = manager.GetBoard();
            var before = board.Clone();
            var (from, to) = FindValidSwap(manager);
            var visual = new Dictionary<Point, GemAppearance>();
            for (int row = 0; row < before.Config.Rows; row++)
            for (int col = 0; col < before.Config.Cols; col++)
            {
                var piece = before.GetPiece(row, col);
                if (piece != null)
                {
                    var position = new Point(row, col);
                    visual[position] = new GemAppearance(position, piece.Color, piece.Special);
                }
            }

            var swapped = visual[from];
            visual[from] = visual[to];
            visual[to] = swapped;
            Assert.IsTrue(manager.TrySwap(from.Row, from.Col, to.Row, to.Col));

            foreach (var step in manager.GetAnimationSteps())
            {
                var command = step.Command;
                if (command.Type == AnimationType.DESTROY ||
                    command.Type == AnimationType.SPECIAL_ACTIVATE)
                {
                    foreach (var target in command.Targets)
                    {
                        var parts = target.Split(',');
                        visual.Remove(new Point(int.Parse(parts[0]), int.Parse(parts[1])));
                    }
                }
                else if (command.Type == AnimationType.MOVE)
                {
                    foreach (var move in command.Moves)
                    {
                        Assert.IsTrue(visual.TryGetValue(move.From, out var gem),
                            $"Missing source for visual move {move.From} -> {move.To}");
                        visual.Remove(move.From);
                        visual[move.To] = gem;
                    }
                }
                else if (command.Type == AnimationType.SPAWN)
                {
                    foreach (var appearance in command.Appearances)
                        visual[appearance.Position] = appearance;
                }
            }

            for (int row = 0; row < board.Config.Rows; row++)
            for (int col = 0; col < board.Config.Cols; col++)
            {
                var position = new Point(row, col);
                var piece = board.GetPiece(row, col);
                if (piece == null)
                    Assert.IsFalse(visual.ContainsKey(position), $"Unexpected gem at {position}");
                else
                {
                    Assert.IsTrue(visual.TryGetValue(position, out var gem),
                        $"Missing visual gem at {position}");
                    Assert.AreEqual(piece.Color, gem.Color, $"Wrong color at {position}");
                    Assert.AreEqual(piece.Special, gem.Special, $"Wrong special at {position}");
                }
            }
        }

        [Test]
        public void BoardClone_ShouldBeIndependent()
        {
            var board = new Board(LegacyConfig);
            var layout = CreateHorizontalMatchLayout();
            board.InitializeFromLayout(layout);

            var clone = board.Clone();
            clone.SwapPieces(0, 0, 0, 1);

            // Original should be unchanged
            var originalPiece = board.GetPiece(0, 0);
            Assert.That(originalPiece.Color, Is.EqualTo(PieceType.RED));
        }

        [Test]
        public void DeterministicRNG_ShouldProduceSameSequenceWithSameSeed()
        {
            var rng1 = new DeterministicRNG(42);
            var rng2 = new DeterministicRNG(42);

            for (int i = 0; i < 100; i++)
            {
                int a = rng1.NextInRange(0, 100);
                int b = rng2.NextInRange(0, 100);
                Assert.That(a, Is.EqualTo(b));
            }
        }

        [Test]
        public void LevelGenerator_ShouldCreateValidLevelData()
        {
            var level = LevelGenerator.GenerateLevel(1);

            Assert.That(level.LevelNumber, Is.EqualTo(1));
            Assert.That(level.Grid, Is.Not.Null);
            Assert.That(level.Grid.Rows, Is.GreaterThan(0));
            Assert.That(level.Grid.Cols, Is.GreaterThan(0));
            Assert.That(level.BoardLayout, Is.Not.Null);
            Assert.IsTrue(level.Validate());
        }

        [Test]
        public void LevelGenerator_ShouldProduceDeterministicResults()
        {
            var level1 = LevelGenerator.GenerateLevel(1);
            var level2 = LevelGenerator.GenerateLevel(1);

            Assert.That(level1.LevelNumber, Is.EqualTo(level2.LevelNumber));
            Assert.That(level1.Seed, Is.EqualTo(level2.Seed));
            Assert.That(level1.BoardLayout.Length, Is.EqualTo(level2.BoardLayout.Length));
        }

        [Test]
        public void Scoring_CalculateCascadeScore_ShouldAwardCorrectBasePoints()
        {
            var matches = new List<Match>
            {
                new Match(new Point(0, 0), new Point(0, 2), 3, MatchDirection.Horizontal, "RED"),
            };

            var (points, _) = Scoring.CalculateCascadeScore(matches, 1);
            // 3 gems at 50 points each.
            Assert.That(points, Is.EqualTo(150));
        }

        [Test]
        public void Scoring_CascadeMultiplier_ShouldScaleWithCascade()
        {
            var matches = new List<Match>
            {
                new Match(new Point(0, 0), new Point(0, 2), 3, MatchDirection.Horizontal, "RED"),
            };

            var (points1, _) = Scoring.CalculateCascadeScore(matches, 1);
            var (points2, _) = Scoring.CalculateCascadeScore(matches, 2);
            var (points3, _) = Scoring.CalculateCascadeScore(matches, 3);

            Assert.That(points2, Is.EqualTo(points1 * 2));
            Assert.That(points3, Is.EqualTo(points1 * 3));
        }

        [Test]
        public void SpecialAbilities_ShouldResolveBombCorrectly()
        {
            string[][] board = CreateCleanBoardString();
            // Place a bomb in the center
            board[3][3] = "GEM_RED";
            board[3][4] = "GEM_RED";
            board[3][5] = "GEM_RED";

            var affected = SpecialAbilities.ResolveBomb(board, new Point(3, 4));
            Assert.That(affected.Length, Is.GreaterThan(0));
        }

        [Test]
        public void SpecialAbilities_ShouldResolveLineClearCorrectly()
        {
            string[][] board = CreateCleanBoardString();

            var affected = SpecialAbilities.ResolveLineClear(board, new Point(3, 3), "both");
            Assert.That(affected.Length, Is.GreaterThan(0));
            // Should affect both row 3 and column 3
            bool hasRow3 = System.Array.Exists(affected, p => p.Row == 3);
            bool hasCol3 = System.Array.Exists(affected, p => p.Col == 3);
            Assert.IsTrue(hasRow3);
            Assert.IsTrue(hasCol3);
        }

        [Test]
        public void SpecialAbilities_ShouldResolveColorBombCorrectly()
        {
            string[][] board = CreateCleanBoardString();
            for (int r = 0; r < ROWS; r++)
                for (int c = 0; c < COLS; c++) board[r][c] = "GEM_GREEN";
            board[0][0] = "GEM_BLUE";
            board[1][0] = "GEM_BLUE";
            board[2][0] = "GEM_BLUE";

            var affected = SpecialAbilities.ResolveColorBomb(board, "BLUE");
            Assert.That(affected.Length, Is.EqualTo(3));
        }

        private string[][] CreateCleanBoardString()
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
    }
}
