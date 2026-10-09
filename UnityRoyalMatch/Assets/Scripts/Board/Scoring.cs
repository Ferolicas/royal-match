// Scoring.cs — Score calculation, combo multipliers, star thresholds
// Ported from src/game/Scoring.ts

using System.Collections.Generic;
using RoyalMatch.Types;

namespace RoyalMatch.Match3
{
    public class ScoreResult
    {
        public int Points;
        public int Combo;
        public bool SpecialBonus;
    }

    public static class Scoring
    {
        public static int CalculateMatchScore(Match match, int cascadeNumber, bool specialsInvolved)
        {
            int baseScore = match.Length * 50;
            int points = baseScore * cascadeNumber;

            if (specialsInvolved)
                points = MathfRound(points * 1.5f);

            return points;
        }

        public static (int points, bool specialsInvolved) CalculateCascadeScore(List<Match> matches, int cascadeNumber)
        {
            int totalPoints = 0;
            bool specialsInvolved = false;

            foreach (var match in matches)
            {
                int score = CalculateMatchScore(match, cascadeNumber, false);
                totalPoints += score;
            }

            return (totalPoints, specialsInvolved);
        }

        public static int ComputeStarRating(int score, int star1, int star2, int star3)
        {
            if (score >= star3) return 3;
            if (score >= star2) return 2;
            if (score >= star1) return 1;
            return 0;
        }

        public static string GetComboDescription(int cascade)
        {
            string[] combos = { "", "Double!", "Triple!", "Quadruple!", "Omega!" };
            return cascade - 1 < combos.Length ? combos[cascade - 1] : "Mega!";
        }

        private static int MathfRound(float f)
        {
            return (int)System.Math.Round(f);
        }
    }
}
