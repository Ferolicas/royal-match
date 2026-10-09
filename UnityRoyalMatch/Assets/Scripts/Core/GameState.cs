// GameState.cs — Game state data and static helpers
// Ported from src/game/GameState.ts

using System.Collections.Generic;
using RoyalMatch.Types;
using RoyalMatch.Levels;

namespace RoyalMatch.Core
{
    public class GameStateData
    {
        public int LevelNumber;
        public Difficulty Difficulty;
        public GameStatus Status;
        public int MoveLimit;
        public int MovesRemaining;
        public int Score;
        public List<RuntimeObjective> Objectives;
        public int Star1;
        public int Star2;
        public int Star3;
        public string[][] BoardLayout;
        public int GridCols;
        public int GridRows;
        public List<Match> LastMatches;
        public int ScoreAdded;
        public bool IsAnimating;

        public GameStateData()
        {
            Objectives = new List<RuntimeObjective>();
            LastMatches = new List<Match>();
        }
    }

    public static class GameState
    {
        public static GameStateData Create(LevelData levelData)
        {
            var state = new GameStateData
            {
                LevelNumber = levelData.LevelNumber,
                Difficulty = levelData.Difficulty,
                Status = GameStatus.WAITING_INPUT,
                MoveLimit = levelData.MoveLimit,
                MovesRemaining = levelData.MoveLimit,
                Score = 0,
                Objectives = new List<RuntimeObjective>(),
                Star1 = levelData.ScoreStar1,
                Star2 = levelData.ScoreStar2,
                Star3 = levelData.ScoreStar3,
                BoardLayout = levelData.BoardLayout,
                GridCols = levelData.Grid.Cols,
                GridRows = levelData.Grid.Rows,
                LastMatches = new List<Match>(),
                ScoreAdded = 0,
                IsAnimating = false,
            };

            foreach (var obj in levelData.Objectives)
            {
                state.Objectives.Add(new RuntimeObjective(obj));
            }

            return state;
        }

        public static int GetStarsEarned(GameStateData state)
        {
            if (state.Objectives.Count == 1 && state.Objectives[0].Type == ObjectiveType.REMOVE_BLOCKER)
            {
                if (state.Status != GameStatus.WIN) return 0;
                float remaining = state.MoveLimit > 0 ?
                    (float)state.MovesRemaining / state.MoveLimit : 0f;
                return remaining >= 0.40f ? 3 : remaining >= 0.20f ? 2 : 1;
            }
            if (state.Score >= state.Star3) return 3;
            if (state.Score >= state.Star2) return 2;
            if (state.Score >= state.Star1) return 1;
            return state.Status == GameStatus.WIN ? 1 : 0;
        }

        public static bool IsLevelComplete(GameStateData state)
        {
            foreach (var obj in state.Objectives)
            {
                if (obj.Current < obj.Target)
                    return false;
            }
            return true;
        }
    }
}
