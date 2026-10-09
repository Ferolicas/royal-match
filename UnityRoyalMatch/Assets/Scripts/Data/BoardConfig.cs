// BoardConfig.cs — Board dimensions, gem count, match rules, animation durations
// Ported from src/data/boardConfig.ts

using UnityEngine;

namespace RoyalMatch.Data
{
    [System.Serializable]
    public class BoardConfigData
    {
        public int Rows = 11;
        public int Cols = 9;
        public int GemTypes = 6;
        public int MinMatch = 3;
        public int BaseMatchScore = 60;
        public bool CascadeMultiplier = true;
    }

    public static class BoardConfig
    {
        public static readonly BoardConfigData Config = new BoardConfigData();

        // Animation durations (in seconds for Unity)
        public const float SwapAnimationDuration = 0.25f;
        public const float MatchDestroyDuration = 0.3f;
        public const float GravityDuration = 0.25f;
        public const float SpawnDuration = 0.25f;
        public const float SpecialActivateDuration = 0.35f;

        public const float CascadeDelay = 0.6f;
        public const float TurnResolveDelay = 1.0f;
    }
}
