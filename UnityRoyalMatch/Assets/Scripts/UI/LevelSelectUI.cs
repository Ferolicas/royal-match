// LevelSelectUI.cs — Level selection grid with star progress display

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using RoyalMatch.Types;
using RoyalMatch.Levels;

namespace RoyalMatch.UI
{
    public class LevelSelectUI : MonoBehaviour
    {
        [Header("References")]
        public Transform ContentParent;
        public GameObject LevelButtonPrefab;
        public TextMeshProUGUI TitleLabel;
        public Button BackButton;

        [Header("Worlds")]
        public WorldTab[] WorldTabs;

        private void Start()
        {
            if (BackButton != null)
                BackButton.onClick.AddListener(OnBack);

            if (TitleLabel != null)
                TitleLabel.text = "SELECT LEVEL";

            BuildLevelGrid();
        }

        private void BuildLevelGrid()
        {
            if (ContentParent == null || LevelButtonPrefab == null) return;

            // Clear existing
            foreach (Transform child in ContentParent)
                Destroy(child.gameObject);

            // Use a simple grid layout
            var grid = ContentParent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(100, 100);
            grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            for (int level = 1; level <= 100; level++) // Show first 100 levels
            {
                var buttonObj = Instantiate(LevelButtonPrefab, ContentParent);
                var button = buttonObj.GetComponent<LevelSelectButton>();
                if (button != null)
                {
                    bool unlocked = IsLevelUnlocked(level);
                    int stars = GetLevelStars(level);
                    var difficulty = DifficultySystem.GetDifficulty(level);
                    button.Setup(level, stars, difficulty, unlocked);
                    button.OnClicked += (lvl) => OnLevelSelected(lvl);
                }
            }
        }

        private bool IsLevelUnlocked(int levelNumber)
        {
            if (levelNumber == 1) return true;
            return GetLevelStars(levelNumber - 1) > 0;
        }

        private int GetLevelStars(int levelNumber)
        {
            return PlayerPrefs.GetInt($"level_{levelNumber}_stars", 0);
        }

        private void OnBack()
        {
            GameSceneManager.Instance?.ShowMainMenu();
        }

        private void OnLevelSelected(int levelNumber)
        {
            GameSceneManager.Instance?.StartLevel(levelNumber);
        }
    }

    [System.Serializable]
    public class WorldTab
    {
        public Button Button;
        public TextMeshProUGUI Label;
        public int WorldId;
    }
}
