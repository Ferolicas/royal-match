using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoyalMatch.EditorTools
{
    public static class BuildScript
    {
        [MenuItem("Royal Match/Build Standalone")]
        public static void BuildStandalone()
        {
            string buildPath = "Builds/RoyalMatch_Standalone.x86_64";
            string scenesPath = "Assets/Scenes";

            // Keep startup order deterministic. Directory enumeration order is not
            // guaranteed and GameScene cannot be the player entry point.
            var scenes = new[]
            {
                Path.Combine(scenesPath, "Bootstrap.unity"),
                Path.Combine(scenesPath, "GameScene.unity"),
            };

            foreach (var scene in scenes)
            {
                if (!File.Exists(scene))
                    throw new FileNotFoundException($"Required build scene is missing: {scene}", scene);
            }

            ValidateRequiredAssets();

            // Ensure scenes are in build settings
            var buildSettingsScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var scene in scenes)
            {
                buildSettingsScenes.Add(new EditorBuildSettingsScene(scene, true));
            }
            EditorBuildSettings.scenes = buildSettingsScenes.ToArray();

            // Configure player settings
            PlayerSettings.companyName = "Olcas";
            PlayerSettings.productName = "Mil Gemas";
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            Debug.Log($"Building standalone to: {buildPath}");
            Debug.Log($"Scenes: {string.Join(", ", scenes)}");

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {report.summary.totalSize / (1024 * 1024)} MB");
            }
            else
            {
                Debug.LogError($"Build failed: {report.summary.result}");
            }

            EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }

        [MenuItem("Royal Match/Build Web (Mobile + Desktop)")]
        public static void BuildWeb()
        {
            string[] scenes =
            {
                "Assets/Scenes/Bootstrap.unity",
                "Assets/Scenes/GameScene.unity",
            };
            foreach (var scene in scenes)
                if (!File.Exists(scene))
                    throw new FileNotFoundException($"Required build scene is missing: {scene}", scene);

            ValidateRequiredAssets();
            EditorBuildSettings.scenes = scenes.Select(scene =>
                new EditorBuildSettingsScene(scene, true)).ToArray();

            PlayerSettings.companyName = "Olcas";
            PlayerSettings.productName = "Mil Gemas";
            PlayerSettings.WebGL.template = "PROJECT:RoyalMobile";

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/RoyalMatch_Web",
                target = BuildTarget.WebGL,
                options = BuildOptions.StrictMode,
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new BuildFailedException($"Web build failed: {report.summary.result}");

            Debug.Log($"Web build succeeded: {report.summary.totalSize / (1024 * 1024)} MB");
        }

        private static void ValidateRequiredAssets()
        {
            var gemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gem.prefab");
            var obstaclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Obstacle.prefab");
            if (gemPrefab == null || gemPrefab.GetComponent<MeshFilter>()?.sharedMesh == null)
                throw new BuildFailedException("Gem.prefab has no renderable mesh.");
            if (obstaclePrefab == null || obstaclePrefab.GetComponent<MeshFilter>()?.sharedMesh == null)
                throw new BuildFailedException("Obstacle.prefab has no renderable mesh.");

            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
            try
            {
                var background = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<MeshFilter>(true))
                    .FirstOrDefault(filter => filter.name == "BoardBackground");
                if (background?.sharedMesh == null)
                    throw new BuildFailedException("GameScene BoardBackground has no renderable mesh.");
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
