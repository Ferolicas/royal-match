// BoardTests.cs — Ported from tests/game/Board.test.ts
// Tests for board mechanics: swap, match, gravity, fill, clone

using NUnit.Framework;
using RoyalMatch.Match3;
using RoyalMatch.Types;

namespace RoyalMatch.Tests
{
    [TestFixture]
    public class BoardTests
    {
        private const int ROWS = 8;
        private const int COLS = 8;
        private static readonly BoardConfig TestConfig = new BoardConfig
        {
            Rows = ROWS, Cols = COLS, GemTypes = 6, MinMatch = 3,
        };

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

        private string[][] CreateVerticalMatchLayout()
        {
            var layout = new string[ROWS][];
            for (int r = 0; r < ROWS; r++)
            {
                layout[r] = new string[COLS];
                for (int c = 0; c < COLS; c++)
                {
                    if (c == 0 && (r == 0 || r == 1 || r == 2))
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

        [Test]
        public void Initialization_ShouldCreateEmptyBoardWithCorrectDimensions()
        {
            var board = new Board(TestConfig);
            for (int r = 0; r < ROWS; r++)
            {
                for (int c = 0; c < COLS; c++)
                {
                    Assert.IsNull(board.GetPiece(r, c));
                }
            }
        }

        [Test]
        public void Initialization_ShouldInitializeFromLayout()
        {
            var board = new Board(TestConfig);
            var layout = CreateHorizontalMatchLayout();
            board.InitializeFromLayout(layout);

            var piece = board.GetPiece(0, 0);
            Assert.IsNotNull(piece);
            Assert.That(piece.Color, Is.EqualTo(PieceType.RED));
        }

        [Test]
        public void Swapping_ShouldSwapTwoPiecesOnTheBoard()
        {
            var board = new Board(TestConfig);
            var layout = CreateCleanLayout();
            board.InitializeFromLayout(layout);

            board.SwapPieces(0, 0, 0, 1);

            var piece1 = board.GetPiece(0, 0);
            var piece2 = board.GetPiece(0, 1);
            Assert.That(piece1.Color, Is.EqualTo(PieceType.GREEN));
            Assert.That(piece2.Color, Is.EqualTo(PieceType.BLUE));
        }

        [Test]
        public void Swapping_ShouldUpdatePiecePositions()
        {
            var board = new Board(TestConfig);
            var layout = CreateCleanLayout();
            board.InitializeFromLayout(layout);

            board.SwapPieces(0, 0, 0, 1);

            Assert.That(board.GetPiece(0, 0).Row, Is.EqualTo(0));
            Assert.That(board.GetPiece(0, 0).Col, Is.EqualTo(0));
            Assert.That(board.GetPiece(0, 1).Row, Is.EqualTo(0));
            Assert.That(board.GetPiece(0, 1).Col, Is.EqualTo(1));
        }

        [Test]
        public void MatchFinding_ShouldFindHorizontalMatchOf3()
        {
            var board = new Board(TestConfig);
            var layout = CreateHorizontalMatchLayout();
            board.InitializeFromLayout(layout);

            var result = board.FindMatches();
            Assert.That(result.matches.Count, Is.GreaterThan(0));
            var match = result.matches.Find(m => m.Direction == MatchDirection.Horizontal);
            Assert.IsNotNull(match);
            Assert.That(match.Length, Is.EqualTo(3));
        }

        [Test]
        public void MatchFinding_ShouldFindVerticalMatchOf3()
        {
            var board = new Board(TestConfig);
            var layout = CreateVerticalMatchLayout();
            board.InitializeFromLayout(layout);

            var result = board.FindMatches();
            var match = result.matches.Find(m => m.Direction == MatchDirection.Vertical);
            Assert.IsNotNull(match);
            Assert.That(match.Length, Is.EqualTo(3));
        }

        [Test]
        public void MatchFinding_ShouldFindNoMatchesOnCleanBoard()
        {
            var board = new Board(TestConfig);
            var layout = CreateCleanLayout();
            board.InitializeFromLayout(layout);

            var result = board.FindMatches();
            Assert.That(result.matches.Count, Is.EqualTo(0));
        }

        [Test]
        public void RemovingMatches_ShouldRemoveMatchedPieces()
        {
            var board = new Board(TestConfig);
            var layout = CreateHorizontalMatchLayout();
            board.InitializeFromLayout(layout);

            var (matches, _) = board.FindMatches();
            var destroyed = board.RemoveMatches(matches);
            Assert.That(destroyed.Count, Is.EqualTo(3));
            Assert.IsNull(board.GetPiece(0, 0));
            Assert.IsNull(board.GetPiece(0, 1));
            Assert.IsNull(board.GetPiece(0, 2));
        }

        [Test]
        public void Gravity_ShouldMovePiecesWhenBottomRowIsEmpty()
        {
            var board = new Board(TestConfig);
            var layout = CreateHorizontalMatchLayout();
            board.InitializeFromLayout(layout);

            var (matches, _) = board.FindMatches();
            board.RemoveMatches(matches);

            var moved = board.ApplyGravity();
            Assert.That(moved.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Gravity_ShouldDropPiecesIntoGaps()
        {
            var board = new Board(TestConfig);
            var layout = CreateCleanLayout();
            layout[2][0] = "OBSTACLE_BOX";
            board.InitializeFromLayout(layout);
            board.ClearPositions(new System.Collections.Generic.List<Point>
            {
                new Point(3, 0),
                new Point(1, 1)
            });

            var moved = board.ApplyGravity();

            Assert.IsNotNull(board.GetPiece(3, 0));
            Assert.That(moved.Exists(p => p.Row == 3 && p.Col == 0));
            Assert.That(moved.Exists(p => p.Row == 1 && p.Col == 1));
        }

        [Test]
        public void FillFromTop_ShouldSpawnNewPiecesInEmptyCells()
        {
            var board = new Board(TestConfig);
            var layout = CreateCleanLayout();
            board.InitializeFromLayout(layout);

            board.SetPiece(3, 3, null);

            var spawned = board.FillFromTop();
            Assert.That(spawned.Count, Is.EqualTo(1));
            Assert.IsNotNull(board.GetPiece(3, 3));
        }

        [Test]
        public void Cloning_ShouldCreateIndependentClone()
        {
            var board = new Board(TestConfig);
            var layout = CreateHorizontalMatchLayout();
            board.InitializeFromLayout(layout);

            var clone = board.Clone();
            clone.SwapPieces(0, 0, 0, 1);

            // Original should be unchanged
            var originalPiece = board.GetPiece(0, 0);
            Assert.That(originalPiece.Color, Is.EqualTo(PieceType.RED));
        }
    }
}
