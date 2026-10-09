using RoyalMatch.Data;
// HUD.cs — In-game HUD showing moves, score, level, stars, and objectives
// Ported from src/ui/components/HUD.tsx

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RoyalMatch.Core;
using RoyalMatch.Types;
using RoyalMatch.Levels;

namespace RoyalMatch.Rendering
{
    public class HUD : MonoBehaviour
    {
        [Header("Top Bar")]
        public TextMeshProUGUI LevelLabel;
        public TextMeshProUGUI LevelValue;
        public TextMeshProUGUI MovesLabel;
        public TextMeshProUGUI MovesValue;
        public TextMeshProUGUI ScoreLabel;
        public TextMeshProUGUI ScoreValue;
        public Transform StarsContainer;
        public GameObject StarIconPrefab;

        [Header("Objectives")]
        public Transform ObjectivesContainer;
        public GameObject ObjectiveItemPrefab;

        [Header("Overlays")]
        public GameObject LoadingOverlay;
        public Slider LoadingBar;

        private GameStateData _currentState;
        private List<GameObject> _starIcons = new List<GameObject>();
        private List<GameObject> _objectiveItems = new List<GameObject>();

        public void UpdateDisplay(GameStateData state)
        {
            _currentState = state;

            // Top bar
            if (LevelValue != null) LevelValue.text = state.LevelNumber.ToString();
            if (MovesValue != null) MovesValue.text = $"{state.MovesRemaining}/{state.MoveLimit}";
            if (ScoreLabel != null) ScoreLabel.text = "OBSTÁCULOS";
            if (ScoreValue != null)
            {
                var goal = state.Objectives.Find(o => o.Type == ObjectiveType.REMOVE_BLOCKER);
                ScoreValue.text = goal == null ? "0" :
                    Mathf.Max(0, goal.Target - goal.Current).ToString();
            }

            // Stars
            UpdateStars();

            // Objectives
            UpdateObjectives();
        }

        private void UpdateStars()
        {
            // Clear existing
            foreach (var star in _starIcons)
                Destroy(star);
            _starIcons.Clear();

            int stars = GameState.GetStarsEarned(_currentState);

            for (int i = 0; i < 3; i++)
            {
                if (StarIconPrefab == null) break;
                var starObj = Instantiate(StarIconPrefab, StarsContainer);
                var image = starObj.GetComponent<Image>();
                if (image != null)
                {
                    image.color = i < stars ? GemColors.StarGold : new Color(1f, 1f, 1f, 0.2f);
                }
                _starIcons.Add(starObj);
            }
        }

        private void UpdateObjectives()
        {
            // Clear existing
            foreach (var item in _objectiveItems)
                Destroy(item);
            _objectiveItems.Clear();

            foreach (var obj in _currentState.Objectives)
            {
                if (ObjectiveItemPrefab == null) break;
                var itemObj = Instantiate(ObjectiveItemPrefab, ObjectivesContainer);
                var item = itemObj.GetComponent<ObjectiveItemView>();
                if (item != null)
                    item.Setup(obj);
                _objectiveItems.Add(itemObj);
            }
        }

        public void ShowLoading(bool show)
        {
            if (LoadingOverlay != null)
                LoadingOverlay.SetActive(show);
        }
    }
}
