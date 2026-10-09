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
            if (levelNumber < 1 || levelNumber > 1000)
                throw new ArgumentException($"Invalid level number: {levelNumber}");

            string filename = DifficultySystem.GetLevelFilename(levelNumber);
            string fullPath = Path.Combine(Application.streamingAssetsPath, "Levels", filename);

            string json;
            if (File.Exists(fullPath))
            {
                json = await File.ReadAllTextAsync(fullPath);
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            else
            {
                // Fallback for Android JAR access
                string uri = Path.Combine(Application.streamingAssetsPath, "Levels", filename);
                using (var www = new UnityEngine.Networking.UnityWebRequest(uri))
                {
                    await www.SendWebRequest();
                    if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                        throw new InvalidOperationException($"Failed to load level {levelNumber}: {www.error}");
                    json = www.downloadHandler.text;
                }
            }
#endif
            else
            {
                // Generate on-the-fly if file doesn't exist
                var levelData = LevelGenerator.GenerateLevel(levelNumber);
                json = JsonConvert.SerializeObject(levelData, Formatting.Indented, JsonSettings);
            }

            var levelDataResult = JsonConvert.DeserializeObject<LevelData>(json, JsonSettings);
            if (!levelDataResult.Validate())
                throw new InvalidOperationException($"Level {levelNumber} validation failed");

            return levelDataResult;
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
