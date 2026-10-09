// LevelData.cs — Level data schema and runtime objective
// Ported from src/levels/LevelData.ts (Zod schema -> C# classes)
// Uses Newtonsoft.Json for camelCase JSON compatibility

using System;
using Newtonsoft.Json;
using RoyalMatch.Types;

namespace RoyalMatch.Levels
{
    [Serializable]
    public class GridData
    {
        [JsonProperty("cols")]
        public int Cols;

        [JsonProperty("rows")]
        public int Rows;
    }

    [Serializable]
    public class LevelDataObjective
    {
        [JsonProperty("type")]
        public string Type;

        [JsonProperty("description")]
        public string Description;

        [JsonProperty("target")]
        public int Target;

        [JsonProperty("color")]
        public string Color;

        [JsonProperty("itemId")]
        public string ItemId;
    }

    [Serializable]
    public class RewardsData
    {
        [JsonProperty("coins")]
        public int Coins;

        [JsonProperty("stars")]
        public int Stars;

        [JsonProperty("unlocks")]
        public string[] Unlocks;
    }

    [Serializable]
    public class PreplacedSpecialData
    {
        [JsonProperty("position")]
        public PositionData Position;

        [JsonProperty("specialType")]
        public string SpecialType;

        [JsonProperty("color")]
        public string Color;
    }

    [Serializable]
    public class PositionData
    {
        [JsonProperty("row")]
        public int Row;

        [JsonProperty("col")]
        public int Col;
    }

    [Serializable]
    public class LevelData
    {
        [JsonProperty("levelNumber")]
        public int LevelNumber;

        [JsonProperty("difficulty")]
        public Difficulty Difficulty;

        [JsonProperty("title")]
        public string Title;

        [JsonProperty("description")]
        public string Description;

        [JsonProperty("grid")]
        public GridData Grid;

        [JsonProperty("moveLimit")]
        public int MoveLimit;

        [JsonProperty("scoreStar1")]
        public int ScoreStar1;

        [JsonProperty("scoreStar2")]
        public int ScoreStar2;

        [JsonProperty("scoreStar3")]
        public int ScoreStar3;

        [JsonProperty("objectives")]
        public LevelDataObjective[] Objectives;

        [JsonProperty("boardLayout")]
        public string[][] BoardLayout;

        [JsonProperty("preplacedSpecials")]
        public PreplacedSpecialData[] PreplacedSpecials;

        [JsonProperty("rewards")]
        public RewardsData Rewards;

        [JsonProperty("seed")]
        public int Seed;

        [JsonProperty("version")]
        public string Version;

        public bool Validate()
        {
            if (LevelNumber < 1 || LevelNumber > 1000) return false;
            if (Grid == null) return false;
            if (BoardLayout == null || BoardLayout.Length != Grid.Rows) return false;
            if (ScoreStar1 > ScoreStar2 || ScoreStar2 > ScoreStar3) return false;
            foreach (string[] row in BoardLayout)
                if (row.Length != Grid.Cols) return false;
            return true;
        }

        // Convert to runtime objective list
        public RuntimeObjective[] CreateRuntimeObjectives()
        {
            var result = new RuntimeObjective[Objectives.Length];
            for (int i = 0; i < Objectives.Length; i++)
            {
                var rt = new RuntimeObjective();
                var obj = Objectives[i];

                if (Enum.TryParse<ObjectiveType>(obj.Type, true, out var t))
                    rt.Type = t;
                rt.Description = obj.Description;
                rt.Target = obj.Target;
                rt.Current = 0;
                rt.Color = obj.Color;
                rt.ItemId = obj.ItemId;
                result[i] = rt;
            }
            return result;
        }
    }

    public class RuntimeObjective
    {
        public ObjectiveType Type;
        public string Description;
        public int Target;
        public int Current;
        public string Color;
        public string ItemId;

        public RuntimeObjective() { }

        public RuntimeObjective(LevelDataObjective obj)
        {
            if (Enum.TryParse<ObjectiveType>(obj.Type, true, out var t))
                Type = t;
            Description = obj.Description;
            Target = obj.Target;
            Current = 0;
            Color = obj.Color;
            ItemId = obj.ItemId;
        }

        public RuntimeObjective(RuntimeObjective other)
        {
            Type = other.Type;
            Description = other.Description;
            Target = other.Target;
            Current = other.Current;
            Color = other.Color;
            ItemId = other.ItemId;
        }

        public RuntimeObjective(ObjectiveType type, string description, int target)
        {
            Type = type;
            Description = description;
            Target = target;
            Current = 0;
        }
    }

    public static class LevelDataConstants
    {
        public const string SchemaVersion = "1.0.0";
    }
}
