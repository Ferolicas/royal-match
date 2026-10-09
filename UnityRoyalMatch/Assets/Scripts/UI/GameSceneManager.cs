// GameSceneManager.cs — Manages UI panel states and transitions within the GameScene
// Handles showing/hiding menus, level select, HUD, and game over panels

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RoyalMatch.Audio;

namespace RoyalMatch.UI
{
    public enum ScreenState
    {
        Splash,
        MainMenu,
        LevelSelect,
        Playing,
        GameOver,
        Settings,
    }

    public class GameSceneManager : MonoBehaviour
    {
        public static GameSceneManager Instance;

        [Header("UI Panels")]
        public GameObject SplashPanel;
        public GameObject MainMenuPanel;
        public GameObject LevelSelectPanel;
        public GameObject SettingsPanel;
        public GameObject GameOverPanel;
        public GameObject HUDRoot;

        [Header("Current State")]
        public ScreenState CurrentScreen = ScreenState.Splash;
        public int CurrentLevel = 1;

        [Header("Scene Names")]
        public string BootstrapScene = "Bootstrap";
        public string GameScene = "GameScene";

        [Header("Splash")]
        public float SplashDuration = 0.8f;

        public event Action<int> OnLevelStartRequested;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Debug.Log("GameSceneManager.Start: Setting up screens...");
            SetupScreens();
            Debug.Log($"GameSceneManager.Start: Transitioning to {ScreenState.Splash}");
            TransitionTo(ScreenState.Splash);
            StartCoroutine(AutoStartForQualityCheck());
        }

        private System.Collections.IEnumerator AutoStartForQualityCheck()
        {
            var args = Environment.GetCommandLineArgs();
            int marker = Array.IndexOf(args, "-autoLevel");
            if (marker < 0 || marker + 1 >= args.Length || !int.TryParse(args[marker + 1], out int level))
                yield break;

            // Let every scene component subscribe before emitting the request.
            yield return null;
            StartLevel(Math.Max(1, level));
        }

        private void SetupScreens()
        {
            // Find panels if not assigned
            if (SplashPanel == null) SplashPanel = GameObject.Find("SplashPanel");
            if (MainMenuPanel == null) MainMenuPanel = GameObject.Find("MainMenuPanel");
            if (LevelSelectPanel == null) LevelSelectPanel = GameObject.Find("LevelSelectPanel");
            if (SettingsPanel == null) SettingsPanel = GameObject.Find("SettingsPanel");
            if (GameOverPanel == null) GameOverPanel = GameObject.Find("GameOverPanel");
            if (HUDRoot == null) HUDRoot = GameObject.Find("HUDPanel");

            // Hide all panels initially, except splash is handled by TransitionTo
            SetPanelActive(SplashPanel, false);
            SetPanelActive(MainMenuPanel, false);
            SetPanelActive(LevelSelectPanel, false);
            SetPanelActive(SettingsPanel, false);
            SetPanelActive(GameOverPanel, false);
            SetPanelActive(HUDRoot, false);
        }

        public void TransitionTo(ScreenState state)
        {
            if (CurrentScreen == state && state != ScreenState.Splash) return;

            if (CurrentScreen == ScreenState.GameOver && state != ScreenState.GameOver)
                GameAudioManager.Instance?.StopLevelComplete();

            // Hide current screen
            switch (CurrentScreen)
            {
                case ScreenState.Splash: SetPanelActive(SplashPanel, false); break;
                case ScreenState.MainMenu: SetPanelActive(MainMenuPanel, false); break;
                case ScreenState.LevelSelect: SetPanelActive(LevelSelectPanel, false); break;
                case ScreenState.Settings: SetPanelActive(SettingsPanel, false); break;
                case ScreenState.GameOver: SetPanelActive(GameOverPanel, false); break;
                case ScreenState.Playing: SetPanelActive(HUDRoot, false); break;
            }

            CurrentScreen = state;

            // Show new screen
            switch (state)
            {
                case ScreenState.Splash: SetPanelActive(SplashPanel, true); break;
                case ScreenState.MainMenu: SetPanelActive(MainMenuPanel, true); break;
                case ScreenState.LevelSelect: SetPanelActive(LevelSelectPanel, true); break;
                case ScreenState.Settings: SetPanelActive(SettingsPanel, true); break;
                case ScreenState.GameOver: SetPanelActive(GameOverPanel, true); break;
                case ScreenState.Playing: SetPanelActive(HUDRoot, true); break;
            }
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel == null) return;
            panel.SetActive(active);

            // Handle CanvasGroup for fade effects
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.interactable = active;
                cg.blocksRaycasts = active;
                cg.alpha = active ? 1f : 0f;
            }
        }

        // Public API for UI buttons
        public void LoadMenuScene()
        {
            TransitionTo(ScreenState.MainMenu);
        }

        public void ShowMainMenu() => TransitionTo(ScreenState.MainMenu);
        public void ShowLevelSelect() => TransitionTo(ScreenState.LevelSelect);
        public void ShowSettings() => TransitionTo(ScreenState.Settings);
        public void ShowGameOver() => TransitionTo(ScreenState.GameOver);

        public void StartLevel(int levelNumber)
        {
            CurrentLevel = levelNumber;
            OnLevelStartRequested?.Invoke(levelNumber);
            TransitionTo(ScreenState.Playing);
        }

        public void RetryLevel()
        {
            OnLevelStartRequested?.Invoke(CurrentLevel);
            TransitionTo(ScreenState.Playing);
        }

        public void NextLevel()
        {
            CurrentLevel++;
            OnLevelStartRequested?.Invoke(CurrentLevel);
            TransitionTo(ScreenState.Playing);
        }

        public bool CanStartLevel(int levelNumber)
        {
            if (levelNumber == 1) return true;
            return GetLevelStars(levelNumber - 1) > 0;
        }

        public int GetLevelStars(int levelNumber)
        {
            return PlayerPrefs.GetInt($"level_{levelNumber}_stars", 0);
        }

        public void SetLevelStars(int levelNumber, int stars)
        {
            string key = $"level_{levelNumber}_stars";
            int previous = PlayerPrefs.GetInt(key, 0);
            PlayerPrefs.SetInt(key, Math.Max(previous, stars));
            PlayerPrefs.Save();
        }
    }
}
