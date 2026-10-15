// GameOverPanel.cs — Win/lose overlay with star rating

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RoyalMatch.Data;
using RoyalMatch.UI;
using RoyalMatch.Core;

namespace RoyalMatch.Rendering
{
    public class GameOverPanel : MonoBehaviour
    {
        [Header("References")]
        public GameObject PanelRoot;
        public TextMeshProUGUI TitleLabel;
        public Transform StarsContainer;
        public GameObject StarIconPrefab;
        public Button NextLevelButton;
        public Button RetryButton;
        public Button MainMenuButton;
        public TextMeshProUGUI RewardLabel;
        public RectTransform CardRoot;
        public RawImage GuardianCrest;
        public Sprite RatingStarSprite;

        private int _currentLevel = 1;
        private float _showTime;

        private void Awake()
        {
            if (PanelRoot == null) PanelRoot = gameObject;
        }

        public void SetCurrentLevel(int level) => _currentLevel = level;

        public void ShowWin(int stars, int levelNumber)
        {
            _currentLevel = levelNumber;
            if (PanelRoot != null) PanelRoot.SetActive(true);
            _showTime = Time.unscaledTime;
            if (TitleLabel != null) TitleLabel.text = "¡NIVEL SUPERADO!";
            if (GuardianCrest != null) GuardianCrest.uvRect = new Rect(0.5f, 0f, 0.5f, 0.5f);
            ConfigureOutcomeLayout(true);

            SetupStars(stars);

            if (RewardLabel != null)
                RewardLabel.text = $"Nivel {levelNumber}  •  {stars} de 3 estrellas";

            if (NextLevelButton != null)
            {
                NextLevelButton.onClick.RemoveAllListeners();
                NextLevelButton.onClick.AddListener(OnNextLevel);
                NextLevelButton.gameObject.SetActive(true);
                var label = NextLevelButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = "CONTINUAR";
            }
            if (RetryButton != null)
            {
                RetryButton.onClick.RemoveAllListeners();
                RetryButton.onClick.AddListener(OnRetry);
            }
            if (MainMenuButton != null)
            {
                MainMenuButton.onClick.RemoveAllListeners();
                MainMenuButton.onClick.AddListener(OnMainMenu);
            }

        }

        public void ShowLose(int levelNumber)
        {
            _currentLevel = levelNumber;
            if (PanelRoot != null) PanelRoot.SetActive(true);
            _showTime = Time.unscaledTime;
            if (TitleLabel != null) TitleLabel.text = "¡CASI LO LOGRAS!";
            if (GuardianCrest != null) GuardianCrest.uvRect = new Rect(0f, 0f, 0.5f, 0.5f);
            ConfigureOutcomeLayout(false);

            ClearStars();

            if (RewardLabel != null) RewardLabel.text = $"Nivel {levelNumber}  •  Vuelve a intentarlo";
            if (NextLevelButton != null)
            {
                NextLevelButton.gameObject.SetActive(true);
                NextLevelButton.onClick.RemoveAllListeners();
                NextLevelButton.onClick.AddListener(OnContinue);
                var label = NextLevelButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = "+5 MOV. · 900";
            }
            if (RetryButton != null)
            {
                RetryButton.onClick.RemoveAllListeners();
                RetryButton.onClick.AddListener(OnRetry);
            }
            if (MainMenuButton != null)
            {
                MainMenuButton.onClick.RemoveAllListeners();
                MainMenuButton.onClick.AddListener(OnMainMenu);
            }
        }

        public void Hide()
        {
            if (PanelRoot != null) PanelRoot.SetActive(false);
        }

        private void Update()
        {
            if (CardRoot == null || !gameObject.activeInHierarchy) return;
            float t = Mathf.Clamp01((Time.unscaledTime - _showTime) / 0.42f);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            CardRoot.localScale = Vector3.one * Mathf.Lerp(0.74f, 1f, ease);
        }

        private void SetupStars(int stars)
        {
            ClearStars();

            for (int i = 0; i < 3; i++)
            {
                if (StarsContainer == null) break;
                var starObj = new GameObject("RatingStar", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image));
                starObj.transform.SetParent(StarsContainer, false);
                var rect = starObj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(61f, 61f);
                var image = starObj.GetComponent<Image>();
                image.sprite = RatingStarSprite;
                image.preserveAspect = true;
                image.color = i < stars ? Color.white : new Color(0.55f, 0.47f, 0.66f, 0.42f);
                image.raycastTarget = false;
            }
        }

        private void ConfigureOutcomeLayout(bool won)
        {
            if (CardRoot != null)
                CardRoot.sizeDelta = new Vector2(535f, won ? 505f : 560f);
            if (GuardianCrest != null)
                GuardianCrest.rectTransform.anchoredPosition = new Vector2(0f, won ? 154f : 126f);
            if (TitleLabel != null)
                TitleLabel.rectTransform.anchoredPosition = new Vector2(0f, won ? 67f : 36f);
            if (StarsContainer != null)
                StarsContainer.gameObject.SetActive(won);
            if (!won && NextLevelButton != null)
            {
                var rect = NextLevelButton.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(0f, -95f);
                rect.sizeDelta = new Vector2(360f, 65f);
            }
            if (RewardLabel != null)
                RewardLabel.rectTransform.anchoredPosition = new Vector2(0f, won ? -60f : -36f);
            if (RetryButton != null)
            {
                var rect = RetryButton.GetComponent<RectTransform>();
                rect.anchoredPosition = won ? new Vector2(-104f, -206f) : new Vector2(0f, -170f);
                rect.sizeDelta = won ? new Vector2(195f, 51f) : new Vector2(360f, 70f);
                RetryButton.GetComponent<Image>().color = won
                    ? new Color(0.41f, 0.22f, 0.69f) : new Color(1f, 0.76f, 0.24f);
                var label = RetryButton.GetComponentInChildren<TextMeshProUGUI>();
                label.text = won ? "REPETIR" : "REINTENTAR";
                label.fontSize = won ? 20f : 28f;
            }
            if (MainMenuButton != null)
            {
                var rect = MainMenuButton.GetComponent<RectTransform>();
                rect.anchoredPosition = won ? new Vector2(104f, -206f) : new Vector2(0f, -235f);
                rect.sizeDelta = won ? new Vector2(195f, 51f) : new Vector2(220f, 50f);
            }
        }

        private void ClearStars()
        {
            if (StarsContainer == null) return;
            foreach (Transform child in StarsContainer)
                Destroy(child.gameObject);
        }

        private void OnNextLevel()
        {
            Hide();
            GameSceneManager.Instance?.NextLevel();
        }

        private void OnRetry()
        {
            Hide();
            GameSceneManager.Instance?.RetryLevel();
        }

        private void OnContinue()
        {
            var controller = FindFirstObjectByType<GameController>();
            if (controller != null && controller.ContinueWithCoins()) Hide();
            else if (RewardLabel != null) RewardLabel.text = "No tienes monedas suficientes";
        }

        private void OnMainMenu()
        {
            Hide();
            GameSceneManager.Instance?.LoadMenuScene();
        }
    }
}
