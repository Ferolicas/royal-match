// BoostDefinitions.cs — Booster definitions (shuffle, hammer, rocket)
// Ported from src/data/boosts.ts

namespace RoyalMatch.Data
{
    public enum BoostType
    {
        SHUFFLE,
        HAMMER,
        ROCKET,
    }

    [System.Serializable]
    public class BoostDefinition
    {
        public BoostType Id;
        public string Name;
        public string Description;
        public string Icon;
        public int Cost;
    }

    public static class BoostDefinitions
    {
        public static readonly BoostDefinition Shuffle = new BoostDefinition
        {
            Id = BoostType.SHUFFLE,
            Name = "Shuffle",
            Description = "Shuffle all gems on the board to find new matches.",
            Icon = "⇋",
            Cost = 1,
        };

        public static readonly BoostDefinition Hammer = new BoostDefinition
        {
            Id = BoostType.HAMMER,
            Name = "Hammer",
            Description = "Tap to destroy a single gem and its neighbors.",
            Icon = "🔨",
            Cost = 1,
        };

        public static readonly BoostDefinition Rocket = new BoostDefinition
        {
            Id = BoostType.ROCKET,
            Name = "Rocket",
            Description = "Clear an entire row or column.",
            Icon = "🚀",
            Cost = 1,
        };
    }
}
