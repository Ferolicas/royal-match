// Bootstrap.cs — Entry point for the game. Shows splash and loads GameScene.

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoyalMatch.Core
{
    public class Bootstrap : MonoBehaviour
    {
        [Header("Settings")]
        public float SplashDelay = 1.0f;

        private void Awake()
        {
            Application.runInBackground = true;
        }

        private void Start()
        {
            Debug.Log("Bootstrap.Start: Starting game, waiting for splash...");
            // Ensure GameSceneManager persists and is available in GameScene
            StartCoroutine(LoadGameScene());
        }

        private IEnumerator LoadGameScene()
        {
            Debug.Log($"Bootstrap: Waiting {SplashDelay}s for splash screen...");
            // Wait for splash
            yield return new WaitForSecondsRealtime(SplashDelay);

            Debug.Log("Bootstrap: Loading GameScene...");
            SceneManager.LoadScene("GameScene");
            Debug.Log("Bootstrap: GameScene loaded.");
        }
    }
}
