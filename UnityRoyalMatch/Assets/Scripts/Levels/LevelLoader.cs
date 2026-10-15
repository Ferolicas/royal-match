// LevelLoader.cs — Loads level JSON files and initializes GameState
// Ported from src/levels/LevelLoader.ts

using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using RoyalMatch.Core;

namespace RoyalMatch.Levels
{
    public class LevelLoader
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Formatting = Formatting.None,
        };

        public async Task<LevelData> LoadLevel(int levelNumber)
        {
            if (levelNumber < 1 || levelNumber > DifficultySystem.TotalLevels)
                throw new ArgumentException($"Invalid level number: {levelNumber}");
            // Existing V1 JSON remains in the project for migration/reference.
            // The gameplay catalog is generated from one deterministic V2 rule on
            // every target (including Android, where StreamingAssets is a JAR).
            await Task.Yield();
            return LevelGenerator.GenerateLevel(levelNumber);
        }

        public string GetLevelPath(int levelNumber)
        {
            return DifficultySystem.GetLevelPath(levelNumber);
        }

        public Core.GameStateData CreateGameState(LevelData levelData)
        {
            return Core.GameState.Create(levelData);
        }

        // Validate level JSON structure
        public bool ValidateLevel(string json)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<LevelData>(json, JsonSettings);
                return data?.Validate() ?? false;
            }
            catch
            {
                return false;
            }
        }
    }
}
