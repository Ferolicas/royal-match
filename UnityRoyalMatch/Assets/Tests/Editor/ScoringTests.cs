// ScoringTests.cs — Ported from tests/game/Scoring.test.ts
// Tests for score calculation, cascade multipliers, and star thresholds

using NUnit.Framework;
using System.Collections.Generic;
using RoyalMatch.Core;
using RoyalMatch.Match3;
using RoyalMatch.Types;

namespace RoyalMatch.Tests
{
    [TestFixture]
    public class ScoringTests
    {
        [Test]
        public void CalculateCascadeScore_BasicMatch_ShouldScore50PerGem()
        {
            var matches = new List<Match>
            {
                new Match(new Point(0, 0), new Point(0, 2), 3, MatchDirection.Horizontal, "RED"),
            };

            var result = Scoring.CalculateCascadeScore(matches, 1);
            // 3 gems * 50 = 150 points for cascade 1
            Assert.That(result.points, Is.EqualTo(150));
        }

        [Test]
        public void CalculateCascadeScore_CascadeMultiplier_ShouldDoubleAtCascade2()
        {
            var matches = new List<Match>
            {
                new Match(new Point(0, 0), new Point(0, 2), 3, MatchDirection.Horizontal, "RED"),
            };

            var result = Scoring.CalculateCascadeScore(matches, 2);
            // Cascade 2 should double: 150 * 2 = 300
            Assert.That(result.points, Is.EqualTo(300));
        }

        [Test]
        public void CalculateCascadeScore_LargerMatch_ShouldAwardMore()
        {
            var matches = new List<Match>
            {
                new Match(new Point(0, 0), new Point(0, 4), 5, MatchDirection.Horizontal, "RED"),
            };

            var result = Scoring.CalculateCascadeScore(matches, 1);
            // 5 gems * 50 = 250 points for cascade 1
            Assert.That(result.points, Is.EqualTo(250));
        }

        [Test]
        public void GetStarsEarned_ShouldReturn3StarsForHighScore()
        {
            // Create a state with very high score
            var state = new GameStateData
            {
                Star1 = 1000,
                Star2 = 2000,
                Star3 = 3000,
                Score = 5000,
            };

            int stars = GameState.GetStarsEarned(state);
            Assert.That(stars, Is.EqualTo(3));
        }

        [Test]
        public void GetStarsEarned_ShouldReturn0StarsForLowScore()
        {
            var state = new GameStateData
            {
                Star1 = 1000,
                Star2 = 2000,
                Star3 = 3000,
                Score = 500,
            };

            int stars = GameState.GetStarsEarned(state);
            Assert.That(stars, Is.EqualTo(0));
        }

        [Test]
        public void GetStarsEarned_ShouldReturn2StarsForMediumScore()
        {
            var state = new GameStateData
            {
                Star1 = 1000,
                Star2 = 2000,
                Star3 = 3000,
                Score = 2500,
            };

            int stars = GameState.GetStarsEarned(state);
            Assert.That(stars, Is.EqualTo(2));
        }
    }
}
