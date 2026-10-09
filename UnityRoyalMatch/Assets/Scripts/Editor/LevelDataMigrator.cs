// LevelDataMigrator.cs — Editor script to migrate level JSON files from the TypeScript project
// Usage: Run "Royal Match > Migrate Level Data" from the Unity Editor menu

using System.IO;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

namespace RoyalMatch.EditorTools
{
    public static class LevelDataMigrator
    {
        private static readonly string SourceLevelsDir =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..", "..", "..",
                "royal_match", "src", "levels", "levels"));

        private static readonly string TargetLevelsDir =
            Path.Combine(Application.dataPath, "StreamingAssets", "Levels");

        [MenuItem("Royal Match/Migrate Level Data")]
        public static void MigrateLevelData()
        {
            if (!Directory.Exists(SourceLevelsDir))
            {
                Debug.LogError($"Source levels directory not found: {SourceLevelsDir}");
                Debug.Log("Generating levels from C# instead...");
                GenerateAllLevels();
                return;
            }

            Directory.CreateDirectory(TargetLevelsDir);

            string[] sourceDirs = Directory.GetDirectories(SourceLevelsDir);
            int totalCopied = 0;

            foreach (string sourceDir in sourceDirs)
            {
                string dirName = Path.GetFileName(sourceDir);
                string targetDir = Path.Combine(TargetLevelsDir, dirName);
                Directory.CreateDirectory(targetDir);

                string[] jsonFiles = Directory.GetFiles(sourceDir, "*.json");
                foreach (string jsonFile in jsonFiles)
                {
                    string fileName = Path.GetFileName(jsonFile);
                    string targetPath = Path.Combine(targetDir, fileName);
                    File.Copy(jsonFile, targetPath, true);
                    totalCopied++;
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"Migrated {totalCopied} level files from TypeScript project to {TargetLevelsDir}");
        }

        [MenuItem("Royal Match/Generate Levels (C#)")]
        public static void GenerateAllLevels()
        {
            Directory.CreateDirectory(TargetLevelsDir);

            int total = Levels.DifficultySystem.TotalLevels;
            for (int i = 1; i <= total; i++)
            {
                var levelData = Levels.LevelGenerator.GenerateLevel(i);
                string filename = Levels.DifficultySystem.GetLevelFilename(i);
                string filePath = Path.Combine(TargetLevelsDir, filename);

                // Use Newtonsoft.Json for proper camelCase serialization
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(levelData,
                    Newtonsoft.Json.Formatting.Indented,
                    new Newtonsoft.Json.JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                    });
                File.WriteAllText(filePath, json);
            }

            AssetDatabase.Refresh();
            Debug.Log($"Generated {total} levels in {TargetLevelsDir}");
        }

        [MenuItem("Royal Match/Validate Levels")]
        public static void ValidateLevels()
        {
            string[] levelFiles = Directory.GetFiles(TargetLevelsDir, "*.json", SearchOption.AllDirectories);
            int valid = 0;
            int invalid = 0;

            foreach (string filePath in levelFiles)
            {
                string json = File.ReadAllText(filePath);
                var loader = new Levels.LevelLoader();
                if (loader.ValidateLevel(json))
                    valid++;
                else
                    invalid++;
            }

            Debug.Log($"Validation complete: {valid} valid, {invalid} invalid out of {valid + invalid} levels");
        }
    }
}
