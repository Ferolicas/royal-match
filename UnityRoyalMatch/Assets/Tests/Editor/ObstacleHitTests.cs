using System.Collections.Generic;
using NUnit.Framework;
using RoyalMatch.Match3;
using RoyalMatch.Types;

namespace RoyalMatch.Tests
{
    public class ObstacleHitTests
    {
        [Test]
        public void AdjacentMatchesDamageEachObstacleOnceAndPreserveHealth()
        {
            var board = new Board(new BoardConfig { Rows = 3, Cols = 3, GemTypes = 6, MinMatch = 3 });
            board.InitializeFromLayout(new[]
            {
                new[] { "GEM_RED", "GEM_RED", "GEM_RED" },
                new[] { "GEM_BLUE", "OBSTACLE_CAGE", "GEM_GREEN" },
                new[] { "GEM_YELLOW", "GEM_PURPLE", "GEM_CYAN" },
            });

            var adjacent = new List<Point> { new Point(0, 1), new Point(1, 0) };
            var firstHits = new List<ObstacleHit>();
            var firstDestroyed = board.DamageAdjacentObstacles(adjacent, firstHits);
            Assert.That(firstHits.Count, Is.EqualTo(1));
            Assert.That(firstHits[0].HealthBefore, Is.EqualTo(2));
            Assert.That(firstHits[0].HealthAfter, Is.EqualTo(1));
            Assert.That(firstDestroyed, Is.Empty);
            Assert.That(board.Grid.GetCell(1, 1).Health, Is.EqualTo(1));

            var secondHits = new List<ObstacleHit>();
            var secondDestroyed = board.DamageAdjacentObstacles(adjacent, secondHits);
            Assert.That(secondHits.Count, Is.EqualTo(1));
            Assert.That(secondHits[0].Destroyed, Is.True);
            Assert.That(secondDestroyed.Count, Is.EqualTo(1));
            Assert.That(board.Grid.GetCell(1, 1).Content, Is.EqualTo(CellContent.EMPTY));
        }

        [Test]
        public void SpecialBlastDamagesObstacleAtTargetCell()
        {
            var board = new Board(new BoardConfig { Rows = 2, Cols = 2, GemTypes = 6, MinMatch = 3 });
            board.InitializeFromLayout(new[]
            {
                new[] { "OBSTACLE_BOX", "GEM_RED" },
                new[] { "GEM_BLUE", "GEM_GREEN" },
            });
            var hits = new List<ObstacleHit>();
            var destroyed = board.DamageAdjacentObstacles(new List<Point>(), hits,
                new[] { new Point(0, 0) });

            Assert.That(hits.Count, Is.EqualTo(1));
            Assert.That(hits[0].Content, Is.EqualTo(CellContent.OBSTACLE_BOX));
            Assert.That(destroyed.Count, Is.EqualTo(1));
            Assert.That(board.Grid.GetCell(0, 0).Content, Is.EqualTo(CellContent.EMPTY));
        }
    }
}
