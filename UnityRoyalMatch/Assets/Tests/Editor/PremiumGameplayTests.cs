using System.Collections.Generic;
using NUnit.Framework;
using RoyalMatch.Levels;
using RoyalMatch.Match3;
using RoyalMatch.Types;
using RoyalMatch.Core;

namespace RoyalMatch.Tests
{
    public class PremiumGameplayTests
    {
        [TestCase(1, 72, 4)]
        [TestCase(3, 52, 16)]
        [TestCase(6, 44, 24)]
        public void GeneratedLevelsUseTallBoardAndClearAllObstacles(int levelNumber,
            int moves, int blockers)
        {
            var level = LevelGenerator.GenerateLevel(levelNumber);
            Assert.That(level.Grid.Cols, Is.EqualTo(9));
            Assert.That(level.Grid.Rows, Is.EqualTo(11));
            Assert.That(level.MoveLimit, Is.EqualTo(moves));
            Assert.That(level.Objectives.Length, Is.EqualTo(1));
            Assert.That(level.Objectives[0].Type, Is.EqualTo("remove_blocker"));
            Assert.That(level.Objectives[0].Target, Is.EqualTo(blockers));
            int actual = 0;
            foreach (var row in level.BoardLayout)
                foreach (var cell in row)
                    if (cell.StartsWith("OBSTACLE_")) actual++;
            Assert.That(actual, Is.EqualTo(blockers));
        }

        [Test]
        public void GravityMovesDownAndSpawnsAtTop()
        {
            var board = new Board(new BoardConfig { Rows = 4, Cols = 1, GemTypes = 6, MinMatch = 3 });
            board.InitializeFromLayout(new[]
            {
                new[] { "GEM_RED" }, new[] { "GEM_BLUE" },
                new[] { "GEM_GREEN" }, new[] { "GEM_YELLOW" },
            });
            board.ClearPositions(new List<Point> { new Point(0, 0) });
            var transitions = new List<GemMove>();
            board.ApplyGravity(transitions);
            Assert.That(transitions.Count, Is.EqualTo(3));
            foreach (var move in transitions)
                Assert.That(move.To.Row, Is.LessThan(move.From.Row));
            Assert.That(board.GetPiece(0, 0).Color, Is.EqualTo(PieceType.BLUE));
            Assert.That(board.GetPiece(3, 0), Is.Null);
            var spawned = board.FillFromTop();
            Assert.That(spawned, Does.Contain(new Point(3, 0)));
        }

        [Test]
        public void FourFiveAndSquareMatchesCreateDifferentBoosters()
        {
            var four = MatchFinder.FindMatchesWithSpecials(new[]
            {
                new[] { "GEM_RED", "GEM_RED", "GEM_RED", "GEM_RED" },
            });
            Assert.That(four.SpecialPieces.Exists(s => s.Shape == "line" &&
                s.Direction == MatchDirection.Horizontal), Is.True);
            Assert.That(SpecialAbilities.MatchToSpecial(4, "line"), Is.EqualTo(SpecialType.ROCKET));

            var five = MatchFinder.FindMatchesWithSpecials(new[]
            {
                new[] { "GEM_RED", "GEM_RED", "GEM_RED", "GEM_RED", "GEM_RED" },
            });
            Assert.That(five.SpecialPieces.Exists(s => s.Shape == "line"), Is.True);
            Assert.That(SpecialAbilities.MatchToSpecial(5, "line"), Is.EqualTo(SpecialType.COLOR_BOMB));

            var square = MatchFinder.FindMatchesWithSpecials(new[]
            {
                new[] { "GEM_BLUE", "GEM_BLUE" },
                new[] { "GEM_BLUE", "GEM_BLUE" },
            });
            Assert.That(square.Matches, Is.Empty);
            Assert.That(square.SpecialPieces.Exists(s => s.Shape == "square" &&
                s.SourcePoints.Count == 4), Is.True);
            Assert.That(SpecialAbilities.MatchToSpecial(4, "square"), Is.EqualTo(SpecialType.PROPELLER));

            var cross = MatchFinder.FindMatchesWithSpecials(new[]
            {
                new[] { "GEM_BLUE", "GEM_RED", "GEM_GREEN" },
                new[] { "GEM_RED", "GEM_RED", "GEM_RED" },
                new[] { "GEM_YELLOW", "GEM_RED", "GEM_PURPLE" },
            });
            Assert.That(cross.SpecialPieces.Exists(s => s.Shape == "cross"), Is.True);
            Assert.That(SpecialAbilities.MatchToSpecial(5, "cross"), Is.EqualTo(SpecialType.BOMB));
        }

        [TestCase(false, SpecialType.ROCKET_HORIZONTAL)]
        [TestCase(true, SpecialType.COLOR_BOMB)]
        public void StraightSwapCreatesExpectedBooster(bool five, SpecialType expected)
        {
            var layout = new[]
            {
                new[] { "GEM_RED", "GEM_RED", "GEM_GREEN", "GEM_RED",
                    five ? "GEM_RED" : "GEM_YELLOW" },
                new[] { "GEM_BLUE", "GEM_GREEN", "GEM_RED", "GEM_BLUE", "GEM_GREEN" },
                new[] { "GEM_GREEN", "GEM_BLUE", "GEM_YELLOW", "GEM_GREEN", "GEM_BLUE" },
            };
            var state = new GameStateData
            {
                LevelNumber = 1, GridRows = 3, GridCols = 5,
                MoveLimit = 10, MovesRemaining = 10,
                Status = GameStatus.WAITING_INPUT,
                BoardLayout = layout,
                Objectives = new List<RuntimeObjective>
                {
                    new RuntimeObjective(ObjectiveType.REACH_SCORE, "Legacy goal", 10000),
                },
            };
            var manager = new GameManager(state);
            Assert.That(manager.TrySwap(1, 2, 0, 2), Is.True);
            bool found = false;
            for (int row = 0; row < 3; row++)
            for (int col = 0; col < 5; col++)
                if (manager.GetBoard().GetPiece(row, col)?.Special == expected) found = true;
            Assert.That(found, Is.True, $"Expected {expected} from straight match");
        }

        [Test]
        public void BoosterPairsResolveTogetherAndHitObstacles()
        {
            var board = new Board(new BoardConfig { Rows = 5, Cols = 5, GemTypes = 6, MinMatch = 3 });
            var layout = new string[5][];
            for (int row = 0; row < 5; row++)
            {
                layout[row] = new string[5];
                for (int col = 0; col < 5; col++) layout[row][col] = "GEM_RED";
            }
            layout[0][0] = "OBSTACLE_BOX";
            board.InitializeFromLayout(layout);

            board.SetSpecial(2, 1, SpecialType.ROCKET_HORIZONTAL);
            board.SetSpecial(2, 2, SpecialType.BOMB);
            var blast = SpecialComboResolver.Resolve(board, board.GetPiece(2, 1), board.GetPiece(2, 2));
            Assert.That(blast, Does.Contain(new Point(0, 1)));

            board.SetSpecial(2, 1, SpecialType.PROPELLER);
            board.SetSpecial(2, 2, SpecialType.NONE);
            var flight = SpecialComboResolver.Resolve(board, board.GetPiece(2, 1), board.GetPiece(2, 2));
            Assert.That(flight, Does.Contain(new Point(0, 0)));

            board.SetSpecial(2, 1, SpecialType.COLOR_BOMB);
            board.SetSpecial(2, 2, SpecialType.COLOR_BOMB);
            var rainbow = SpecialComboResolver.Resolve(board, board.GetPiece(2, 1), board.GetPiece(2, 2));
            Assert.That(rainbow.Count, Is.EqualTo(25));
        }

        [Test]
        public void EveryBoosterPairProducesAValidEffect()
        {
            var types = new[]
            {
                SpecialType.ROCKET_HORIZONTAL, SpecialType.ROCKET_VERTICAL,
                SpecialType.BOMB, SpecialType.COLOR_BOMB, SpecialType.PROPELLER,
            };
            foreach (var first in types)
            foreach (var second in types)
            {
                var layout = new string[5][];
                for (int row = 0; row < 5; row++)
                {
                    layout[row] = new string[5];
                    for (int col = 0; col < 5; col++) layout[row][col] = "GEM_RED";
                }
                layout[0][0] = "OBSTACLE_BOX";
                var board = new Board(new BoardConfig
                {
                    Rows = 5, Cols = 5, GemTypes = 6, MinMatch = 3,
                });
                board.InitializeFromLayout(layout);
                board.SetSpecial(2, 1, first);
                board.SetSpecial(2, 2, second);
                var affected = SpecialComboResolver.Resolve(board,
                    board.GetPiece(2, 1), board.GetPiece(2, 2));
                Assert.That(affected.Count, Is.GreaterThan(2), $"{first} + {second}");
                Assert.That(affected, Does.Contain(new Point(2, 1)), $"{first} + {second}");
                Assert.That(affected, Does.Contain(new Point(2, 2)), $"{first} + {second}");
            }
        }

        [Test]
        public void SwappingIntoSquareCreatesFlyingPropeller()
        {
            var layout = new[]
            {
                new[] { "GEM_RED", "GEM_RED", "GEM_GREEN" },
                new[] { "GEM_RED", "GEM_BLUE", "GEM_RED" },
                new[] { "GEM_YELLOW", "GEM_GREEN", "GEM_PURPLE" },
            };
            var state = new GameStateData
            {
                LevelNumber = 1,
                GridRows = 3,
                GridCols = 3,
                MoveLimit = 10,
                MovesRemaining = 10,
                Status = GameStatus.WAITING_INPUT,
                BoardLayout = layout,
                Objectives = new List<RuntimeObjective>
                {
                    new RuntimeObjective(ObjectiveType.REACH_SCORE, "Legacy goal", 10000),
                },
            };
            var manager = new GameManager(state);
            Assert.That(manager.TrySwap(1, 2, 1, 1), Is.True);
            Assert.That(state.MovesRemaining, Is.EqualTo(9));
            bool found = false;
            for (int row = 0; row < 3; row++)
            for (int col = 0; col < 3; col++)
                if (manager.GetBoard().GetPiece(row, col)?.Special == SpecialType.PROPELLER)
                    found = true;
            Assert.That(found, Is.True);
        }

        [Test]
        public void PropellerFliesToObjectiveAndClearsIt()
        {
            var layout = new[]
            {
                new[] { "OBSTACLE_BOX", "GEM_RED", "GEM_BLUE" },
                new[] { "GEM_GREEN", "GEM_PURPLE", "GEM_YELLOW" },
                new[] { "GEM_CYAN", "GEM_BLUE", "GEM_GREEN" },
            };
            var state = new GameStateData
            {
                LevelNumber = 1,
                GridRows = 3,
                GridCols = 3,
                MoveLimit = 10,
                MovesRemaining = 10,
                Status = GameStatus.WAITING_INPUT,
                BoardLayout = layout,
                Objectives = new List<RuntimeObjective>
                {
                    new RuntimeObjective(ObjectiveType.REMOVE_BLOCKER, "Clear obstacle", 1),
                },
            };
            var manager = new GameManager(state);
            manager.GetBoard().SetSpecial(2, 2, SpecialType.PROPELLER);
            Assert.That(manager.TrySwap(2, 2, 2, 1), Is.True);
            Assert.That(state.Objectives[0].Current, Is.EqualTo(1));
            Assert.That(state.Status, Is.EqualTo(GameStatus.WIN));
            Assert.That(manager.GetAnimationSteps().Exists(step =>
                step.Command.Flights.Exists(flight => flight.To == new Point(0, 0))), Is.True);
        }
    }
}
