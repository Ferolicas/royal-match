// GameManagerTests.cs — Ported from tests/game/GameManager.test.ts
// Tests for end-to-end game flow: swap, scoring, win/lose conditions

using NUnit.Framework;
using System.Collections.Generic;
using RoyalMatch.Core;
using RoyalMatch.Match3;
using RoyalMatch.Types;
using RoyalMatch.Levels;

namespace RoyalMatch.Tests
{
    [TestFixture]
    public class GameManagerTests
    {
        private const int ROWS = 8;
        private const int COLS = 8;

        private (Point from, Point to) FindValidSwap(GameManager manager)
        {
            var board = manager.GetBoard();
            var directions = new (int dr, int dc)[] { (1, 0), (0, 1) };

            for (int row = 0; row < board.Config.Rows; row++)
            {
                for (int col = 0; col < board.Config.Cols; col++)
                {
                    var p1 = board.GetPiece(row, col);
                    if (p1 == null) continue;

                    foreach (var (dr, dc) in directions)
                    {
                        int nr = row + dr;
                        int nc = col + dc;
                        if (nr >= board.Config.Rows || nc >= board.Config.Cols) continue;

                        var p2 = board.GetPiece(nr, nc);
                        if (p2 == null) continue;

                        board.SwapPieces(row, col, nr, nc);
                        var (matches, _) = board.FindMatches();
                        bool hasMatch = matches.Count > 0;
                        board.SwapPieces(row, col, nr, nc);

                        if (hasMatch)
                            return (new Point(row, col), new Point(nr, nc));
                    }
                }
            }
            throw new System.Exception("Expected generated level to contain a valid swap");
        }

        private GameManager CreateManager(int target, int moves)
        {
            var level = LevelGenerator.GenerateLevel(1);
            var layout = level.BoardLayout;

            // Create a board with matches for testing
            var board = new Board(new BoardConfig
            {
                Rows = level.Grid.Rows, Cols = level.Grid.Cols, GemTypes = 6, MinMatch = 3,
            });
            board.InitializeFromLayout(layout);

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
                Objectives = new List<RuntimeObjective>()
            };

            // Add score objective
            state.Objectives.Add(new RuntimeObjective(ObjectiveType.REACH_SCORE, "Score", target));

            return new GameManager(state);
        }

        [Test]
        public void EndToEnd_ConsumesOneMoveAndScoresMatch()
        {
            var manager = CreateManager(1, 3);
            var board = manager.GetBoard();

            // Create a horizontal match
            var layout = CreateMatchLayout();
            // Reinitialize to ensure a clean state
            board.InitializeFromLayout(layout);
            var state = manager.GetState();
            state.MovesRemaining = 3;

            var (from, to) = FindValidSwap(manager);

            bool result = manager.TrySwap(from.Row, from.Col, to.Row, to.Col);
            Assert.IsTrue(result);
            Assert.That(state.MovesRemaining, Is.EqualTo(2));
            Assert.That(state.Score, Is.GreaterThan(0));
        }

        [Test]
        public void EndToEnd_RejectsNonAdjacentInputWithoutConsumingMove()
        {
            var manager = CreateManager(100, 3);
            var state = manager.GetState();

            bool result = manager.TrySwap(0, 0, 2, 0);
            Assert.IsFalse(result);
            Assert.That(state.MovesRemaining, Is.EqualTo(3));
            Assert.That(state.Status, Is.EqualTo(GameStatus.WAITING_INPUT));
        }

        [Test]
        public void EndToEnd_ActivatesSpecialWhenSwapped()
        {
            var manager = CreateManager(100, 3);
            var board = manager.GetBoard();

            // Set a special piece
            board.SetSpecial(0, 0, SpecialType.BOMB);
            var state = manager.GetState();

            bool result = manager.TrySwap(0, 0, 0, 1);
            Assert.IsTrue(result);
            Assert.That(state.MovesRemaining, Is.EqualTo(2));
            Assert.That(state.Score, Is.GreaterThanOrEqualTo(180));
        }

        private string[][] CreateMatchLayout()
        {
            var layout = new string[DifficultySystem.GridRows][];
            for (int r = 0; r < DifficultySystem.GridRows; r++)
            {
                layout[r] = new string[DifficultySystem.GridCols];
                for (int c = 0; c < DifficultySystem.GridCols; c++)
                {
                    if (r == 0 && (c <= 4))
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
    }
}
