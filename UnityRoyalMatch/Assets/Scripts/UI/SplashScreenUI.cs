// SplashScreenUI.cs — Initial splash screen with loading progress

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RoyalMatch.UI;

namespace RoyalMatch.UI
{
    public class SplashScreenUI : MonoBehaviour
    {
        [Header("References")]
        public Slider ProgressBar;
        public TextMeshProUGUI ProgressText;
        public TextMeshProUGUI TitleLabel;
        public TextMeshProUGUI SubtitleLabel;
        public CanvasGroup DeveloperGroup;
        public CanvasGroup GameTitleGroup;
        public RectTransform DeveloperMark;
        public RectTransform GameTitleMark;

        [Header("Settings")]
        public float Duration = 3.8f;

        private Coroutine _splashRoutine;

        private void OnEnable()
        {
            // GameSceneManager normalizes panel visibility during its own Start.
            // OnEnable makes the splash deterministic regardless of component
            // Start order; a coroutine stopped by deactivation is restarted here.
            if (_splashRoutine != null)
                StopCoroutine(_splashRoutine);
            _splashRoutine = StartCoroutine(PlaySplash());
        }

        private IEnumerator PlaySplash()
        {
            if (TitleLabel != null) TitleLabel.text = "OLCAS";
            if (SubtitleLabel != null) SubtitleLabel.text = "GAMES";
            if (DeveloperGroup != null) DeveloperGroup.alpha = 0f;
            if (GameTitleGroup != null) GameTitleGroup.alpha = 0f;
            if (DeveloperMark != null) DeveloperMark.localScale = Vector3.one * 0.78f;
            if (GameTitleMark != null) GameTitleMark.localScale = Vector3.one * 0.86f;

            float introEnd = Mathf.Min(0.85f, Duration * 0.23f);
            float developerOutStart = Mathf.Min(1.35f, Duration * 0.36f);
            float developerOutEnd = Mathf.Min(1.75f, Duration * 0.47f);
            float titleInEnd = Mathf.Min(2.35f, Duration * 0.63f);
            float titleOutStart = Mathf.Max(titleInEnd + 0.4f, Duration - 0.5f);
            float elapsed = 0f;

            while (elapsed < Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (DeveloperGroup != null)
                {
                    if (elapsed <= introEnd)
                        DeveloperGroup.alpha = Mathf.SmoothStep(0f, 1f, elapsed / introEnd);
                    else if (elapsed >= developerOutStart)
                        DeveloperGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f,
                            Mathf.InverseLerp(developerOutStart, developerOutEnd, elapsed));
                }
                if (DeveloperMark != null && elapsed <= introEnd)
                    DeveloperMark.localScale = Vector3.one * Mathf.Lerp(0.78f, 1f,
                        Mathf.SmoothStep(0f, 1f, elapsed / introEnd));

                if (GameTitleGroup != null)
                {
                    if (elapsed >= developerOutStart && elapsed <= titleInEnd)
                        GameTitleGroup.alpha = Mathf.SmoothStep(0f, 1f,
                            Mathf.InverseLerp(developerOutStart, titleInEnd, elapsed));
                    else if (elapsed >= titleOutStart)
                        GameTitleGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f,
                            Mathf.InverseLerp(titleOutStart, Duration, elapsed));
                }
                if (GameTitleMark != null && elapsed >= developerOutStart && elapsed <= titleInEnd)
                    GameTitleMark.localScale = Vector3.one * Mathf.Lerp(0.86f, 1f,
                        Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(developerOutStart, titleInEnd, elapsed)));

                SetProgress(Mathf.Clamp01(elapsed / Duration));
                yield return null;
            }

            SetProgress(1f);

            // Transition to main menu via GameSceneManager
            GameSceneManager.Instance?.LoadMenuScene();
            _splashRoutine = null;
        }

        private void SetProgress(float progress)
        {
            if (ProgressBar != null) ProgressBar.value = progress;
            if (ProgressText != null) ProgressText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";
        }
    }
}
