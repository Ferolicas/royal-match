// LevelSelectButton.cs — Button component for level selection grid

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RoyalMatch.Types;
using RoyalMatch.Levels;

namespace RoyalMatch.UI
{
    public class LevelSelectButton : MonoBehaviour
    {
        public System.Action<int> OnClicked;

        [Header("References")]
        public Button Button;
        public TextMeshProUGUI NumberLabel;
        public TextMeshProUGUI StarsLabel;
        public Image BorderImage;
        public Image BackgroundImage;

        private int _levelNumber;

        public void Setup(int levelNumber, int stars, Difficulty difficulty, bool unlocked)
        {
            _levelNumber = levelNumber;

            if (NumberLabel != null)
                NumberLabel.text = levelNumber.ToString();

            if (StarsLabel != null)
            {
                string starStr = "";
                for (int i = 0; i < stars; i++) starStr += "★";
                for (int i = stars; i < 3; i++) starStr += "☆";
                StarsLabel.text = starStr;
            }

            if (Button != null)
            {
                Button.interactable = unlocked;
                Button.onClick.AddListener(OnButtonClick);
            }

            // Color based on difficulty
            if (BorderImage != null)
            {
                if (difficulty == Difficulty.HARD)
                    BorderImage.color = new Color(1f, 0.596f, 0f); // Orange
                else if (difficulty == Difficulty.SUPER_HARD)
                    BorderImage.color = new Color(0.957f, 0.267f, 0.212f); // Red
                else
                    BorderImage.color = new Color(0.180f, 0.133f, 0.314f); // Default
            }

            // Locked visual state
            if (BackgroundImage != null)
                BackgroundImage.color = unlocked
                    ? new Color(1f, 1f, 1f, 0.1f)
                    : new Color(0f, 0f, 0f, 0.5f);
        }

        private void OnButtonClick()
        {
            OnClicked?.Invoke(_levelNumber);
        }
    }
}
