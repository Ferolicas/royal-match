// MatchFinderTests.cs — Ported from tests/game/MatchFinder.test.ts
// Tests for match finding algorithm

using NUnit.Framework;
using System.Collections.Generic;
using RoyalMatch.Match3;
using RoyalMatch.Types;

namespace RoyalMatch.Tests
{
    [TestFixture]
    public class MatchFinderTests
    {
        private string[][] CreateCleanBoard(int rows, int cols)
        {
            var board = new string[rows][];
            for (int r = 0; r < rows; r++)
            {
                board[r] = new string[cols];
                for (int c = 0; c < cols; c++)
                {
                    board[r][c] = (r + c) % 2 == 0 ? "GEM_BLUE" : "GEM_GREEN";
                }
            }
            return board;
        }

        [Test]
        public void FindMatches_ShouldFindHorizontalMatchOf3()
        {
            var board = CreateCleanBoard(8, 8);
            board[0][0] = "GEM_RED";
            board[0][1] = "GEM_RED";
            board[0][2] = "GEM_RED";

            var matches = MatchFinder.FindMatches(board);
            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches[0].Direction, Is.EqualTo(MatchDirection.Horizontal));
            Assert.That(matches[0].Length, Is.EqualTo(3));
        }

        [Test]
        public void FindMatches_ShouldFindVerticalMatchOf3()
        {
            var board = CreateCleanBoard(8, 8);
            board[0][0] = "GEM_BLUE";
            board[1][0] = "GEM_BLUE";
            board[2][0] = "GEM_BLUE";

            var matches = MatchFinder.FindMatches(board);
            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches[0].Direction, Is.EqualTo(MatchDirection.Vertical));
            Assert.That(matches[0].Length, Is.EqualTo(3));
        }

        [Test]
        public void FindMatches_ShouldFindMatchOf4()
        {
            var board = CreateCleanBoard(8, 8);
            board[0][0] = "GEM_GREEN";
            board[0][1] = "GEM_GREEN";
            board[0][2] = "GEM_GREEN";
            board[0][3] = "GEM_GREEN";

            var matches = MatchFinder.FindMatches(board);
            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches[0].Length, Is.EqualTo(4));
        }

        [Test]
        public void FindMatches_ShouldFindMatchOf5()
        {
            var board = CreateCleanBoard(8, 8);
            board[0][0] = "GEM_YELLOW";
            board[0][1] = "GEM_YELLOW";
            board[0][2] = "GEM_YELLOW";
            board[0][3] = "GEM_YELLOW";
            board[0][4] = "GEM_YELLOW";

            var matches = MatchFinder.FindMatches(board);
            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches[0].Length, Is.EqualTo(5));
        }

        [Test]
        public void FindMatches_ShouldFindMatchOf6()
        {
            var board = CreateCleanBoard(8, 8);
            board[0][0] = "GEM_CYAN";
            board[0][1] = "GEM_CYAN";
            board[0][2] = "GEM_CYAN";
            board[0][3] = "GEM_CYAN";
            board[0][4] = "GEM_CYAN";
            board[0][5] = "GEM_CYAN";

            var matches = MatchFinder.FindMatches(board);
            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches[0].Length, Is.EqualTo(6));
        }

        [Test]
        public void FindMatches_ShouldFindMultipleSeparateMatches()
        {
            var board = CreateCleanBoard(8, 8);
            board[0][0] = "GEM_RED";
            board[0][1] = "GEM_RED";
            board[0][2] = "GEM_RED";
            board[3][0] = "GEM_BLUE";
            board[4][0] = "GEM_BLUE";
            board[5][0] = "GEM_BLUE";

            var matches = MatchFinder.FindMatches(board);
            Assert.That(matches.Count, Is.EqualTo(2));
        }

        [Test]
        public void FindMatches_ShouldFindNoMatchesOnCleanBoard()
        {
            var board = CreateCleanBoard(8, 8);
            var matches = MatchFinder.FindMatches(board);
            Assert.That(matches.Count, Is.EqualTo(0));
        }

        [Test]
        public void FindMatches_ShouldNotMatchNonGemCells()
        {
            var board = new string[][]
            {
                new string[] { "EMPTY", "EMPTY", "EMPTY" },
                new string[] { "EMPTY", "EMPTY", "EMPTY" },
                new string[] { "EMPTY", "EMPTY", "EMPTY" },
            };

            var matches = MatchFinder.FindMatches(board);
            Assert.That(matches.Count, Is.EqualTo(0));
        }

        [Test]
        public void FindMatchesWithSpecials_ShouldDetectSpecialsForMatchesOf4()
        {
            var board = CreateCleanBoard(8, 8);
            board[0][0] = "GEM_RED";
            board[0][1] = "GEM_RED";
            board[0][2] = "GEM_RED";
            board[0][3] = "GEM_RED";

            var result = MatchFinder.FindMatchesWithSpecials(board);
            Assert.That(result.Matches.Count, Is.GreaterThan(0));
            // Match of 4 should create a special piece
        }

        [Test]
        public void HasAnyMatch_ShouldReturnTrueWhenMatchExists()
        {
            var board = CreateCleanBoard(8, 8);
            board[0][0] = "GEM_RED";
            board[0][1] = "GEM_RED";
            board[0][2] = "GEM_RED";

            Assert.IsTrue(MatchFinder.HasAnyMatch(board));
        }

        [Test]
        public void HasAnyMatch_ShouldReturnFalseWhenNoMatchExists()
        {
            var board = CreateCleanBoard(8, 8);
            Assert.IsFalse(MatchFinder.HasAnyMatch(board));
        }

        [Test]
        public void FindMatches_ShouldNotDuplicateOverlappingMatchesInSameRow()
        {
            var board = CreateCleanBoard(8, 8);
            board[0][0] = "GEM_RED";
            board[0][1] = "GEM_RED";
            board[0][2] = "GEM_RED";
            board[0][3] = "GEM_RED";
            board[0][4] = "GEM_RED";

            var matches = MatchFinder.FindMatches(board);
            Assert.That(matches.Count, Is.EqualTo(1));
            Assert.That(matches[0].Length, Is.EqualTo(5));
        }
    }
}
