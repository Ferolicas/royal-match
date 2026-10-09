// MainMenuUI.cs — Main menu screen with Play and Settings buttons

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using RoyalMatch.Audio;

namespace RoyalMatch.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("References")]
        public Button PlayButton;
        public Button SettingsButton;
        public Button LevelSelectButton;
        public TextMeshProUGUI VersionLabel;
        public CanvasGroup MenuGroup;
        public RectTransform TitleRoot;
        public RectTransform ActionsRoot;

        private Coroutine _entranceRoutine;

        private void OnEnable()
        {
            if (_entranceRoutine != null) StopCoroutine(_entranceRoutine);
            _entranceRoutine = StartCoroutine(PlayEntrance());
        }

        private void Start()
        {
            if (PlayButton != null) PlayButton.onClick.AddListener(OnPlay);
            if (SettingsButton != null) SettingsButton.onClick.AddListener(OnSettings);
            if (LevelSelectButton != null) LevelSelectButton.onClick.AddListener(OnLevelSelect);

            if (VersionLabel != null)
                VersionLabel.text = "OLCAS GAMES  •  2026";
        }

        private IEnumerator PlayEntrance()
        {
            if (MenuGroup != null) MenuGroup.alpha = 0f;
            if (TitleRoot != null) TitleRoot.localScale = Vector3.one * 0.84f;
            if (ActionsRoot != null) ActionsRoot.anchoredPosition += new Vector2(0f, -24f);
            Vector2 actionTarget = ActionsRoot != null ? ActionsRoot.anchoredPosition + new Vector2(0f, 24f) : Vector2.zero;
            float elapsed = 0f;
            while (elapsed < 0.65f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.65f));
                if (MenuGroup != null) MenuGroup.alpha = t;
                if (TitleRoot != null) TitleRoot.localScale = Vector3.one * Mathf.Lerp(0.84f, 1f, t);
                if (ActionsRoot != null) ActionsRoot.anchoredPosition = Vector2.Lerp(actionTarget - new Vector2(0f, 24f), actionTarget, t);
                yield return null;
            }
            _entranceRoutine = null;
        }

        private void OnPlay()
        {
            GameAudioManager.Instance?.PlayUi();
            // Play the first unlocked level or level 1
            GameSceneManager.Instance?.StartLevel(1);
        }

        private void OnSettings()
        {
            GameAudioManager.Instance?.PlayUi();
            GameSceneManager.Instance?.ShowSettings();
        }

        private void OnLevelSelect()
        {
            GameAudioManager.Instance?.PlayUi();
            GameSceneManager.Instance?.ShowLevelSelect();
        }
    }
}
