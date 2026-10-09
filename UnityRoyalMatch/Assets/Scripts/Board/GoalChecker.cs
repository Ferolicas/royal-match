// GoalChecker.cs — Checks win conditions, objective progress
// Ported from src/game/GoalChecker.ts

using System.Collections.Generic;
using RoyalMatch.Types;
using RoyalMatch.Levels;

namespace RoyalMatch.Match3
{
    public struct CheckResult
    {
        public bool Completed;
        public (int index, int current)[] ObjectiveProgress;
    }

    public static class GoalChecker
    {
        public static CheckResult Check(List<RuntimeObjective> objectives, List<Match> matches, int scoreDelta)
        {
            var progress = new List<(int index, int current)>();

            for (int i = 0; i < objectives.Count; i++)
            {
                var obj = objectives[i];

                switch (obj.Type)
                {
                    case ObjectiveType.COLLECT_COLOR:
                    {
                        int collected = 0;
                        foreach (var match in matches)
                        {
                            if (match.Color == obj.Color)
                                collected += match.Length;
                        }
                        obj.Current += collected;
                        break;
                    }
                    case ObjectiveType.REACH_SCORE:
                        obj.Current += scoreDelta;
                        break;
                    case ObjectiveType.REMOVE_BLOCKER:
                        obj.Current += matches.Count;
                        break;
                    case ObjectiveType.COLLECT_ITEM:
                        obj.Current += matches.Count / 3;
                        break;
                    case ObjectiveType.DROP_DOWN:
                        obj.Current += matches.Count;
                        break;
                }

                progress.Add((i, obj.Current));
            }

            bool completed = true;
            foreach (var obj in objectives)
            {
                if (obj.Current < obj.Target)
                {
                    completed = false;
                    break;
                }
            }

            return new CheckResult
            {
                Completed = completed,
                ObjectiveProgress = progress.ToArray(),
            };
        }

        public static bool IsComplete(List<RuntimeObjective> objectives)
        {
            foreach (var obj in objectives)
            {
                if (obj.Current < obj.Target)
                    return false;
            }
            return true;
        }

        public static bool IsFailed(int movesRemaining, List<RuntimeObjective> objectives)
        {
            return movesRemaining <= 0 && !IsComplete(objectives);
        }

        public static int GetCompletionPercentage(List<RuntimeObjective> objectives)
        {
            int totalProgress = 0;
            int totalTarget = 0;

            foreach (var obj in objectives)
            {
                totalProgress += System.Math.Min(obj.Current, obj.Target);
                totalTarget += obj.Target;
            }

            if (totalTarget == 0) return 100;
            return (int)System.Math.Round((float)totalProgress / totalTarget * 100);
        }
    }
}
