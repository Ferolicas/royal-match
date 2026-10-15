// LevelSelectUI.cs — Level selection grid with star progress display

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using RoyalMatch.Types;
using RoyalMatch.Levels;
using RoyalMatch.Progress;

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
        private const int PageSize = 20;
        private int _pageIndex;
        private Button _previousPage;
        private Button _nextPage;

        private void OnEnable()
        {
            _pageIndex = (PlayerProgress.CurrentLevel - 1) / PageSize;
            BuildLevelGrid();
        }

        private void Start()
        {
            if (TitleLabel == null)
            {
                foreach (var label in GetComponentsInChildren<TextMeshProUGUI>(true))
                    if (label.name == "TitleLabel") { TitleLabel = label; break; }
            }
            if (BackButton != null)
                BackButton.onClick.AddListener(OnBack);

            if (TitleLabel != null)
                TitleLabel.text = "SELECT LEVEL";

            if (BackButton != null)
            {
                _previousPage = CreateNavigationButton("PreviousPage", "ANTERIORES", -150f, -270f,
                    () => { _pageIndex--; BuildLevelGrid(); });
                _nextPage = CreateNavigationButton("NextPage", "SIGUIENTES", 150f, -270f,
                    () => { _pageIndex++; BuildLevelGrid(); });
            }
            BuildLevelGrid();
        }

        private Button CreateNavigationButton(string name, string label, float x, float y, UnityEngine.Events.UnityAction action)
        {
            var obj = Instantiate(BackButton.gameObject, BackButton.transform.parent);
            obj.name = name;
            var rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(180f, 42f);
            var button = obj.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            var text = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = label;
            return button;
        }

        private void BuildLevelGrid()
        {
            if (ContentParent == null || LevelButtonPrefab == null) return;

            // Clear existing
            foreach (Transform child in ContentParent)
                Destroy(child.gameObject);

            // Use a simple grid layout
            var grid = ContentParent.GetComponent<GridLayoutGroup>() ??
                ContentParent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(88, 88);
            grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;

            int firstVisible = _pageIndex * PageSize + 1;
            int lastVisible = System.Math.Min(DifficultySystem.TotalLevels, firstVisible + PageSize - 1);
            if (TitleLabel != null) TitleLabel.text = $"NIVELES {firstVisible}-{lastVisible}";
            if (_previousPage != null) _previousPage.interactable = _pageIndex > 0;
            if (_nextPage != null) _nextPage.interactable = lastVisible < DifficultySystem.TotalLevels &&
                lastVisible < PlayerProgress.CurrentLevel;
            for (int level = firstVisible; level <= lastVisible; level++)
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
            return PlayerProgress.IsUnlocked(levelNumber);
        }

        private int GetLevelStars(int levelNumber)
        {
            return PlayerProgress.GetLevelStars(levelNumber);
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
