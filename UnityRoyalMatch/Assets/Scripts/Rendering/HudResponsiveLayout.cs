using TMPro;
using UnityEngine;

namespace RoyalMatch.Rendering
{
    public sealed class HudResponsiveLayout : MonoBehaviour
    {
        public RectTransform TopBar;
        public RectTransform Portrait;
        public HUD Hud;
        private float _lastWidth;

        private void OnEnable() => Apply();

        private void LateUpdate()
        {
            if (TopBar != null && !Mathf.Approximately(_lastWidth, TopBar.rect.width)) Apply();
        }

        private void Apply()
        {
            if (TopBar == null || Hud == null || Portrait == null) return;
            float width = TopBar.rect.width;
            if (width <= 0f) return;
            _lastWidth = width;
            if (width < 900f)
            {
                TopBar.sizeDelta = new Vector2(0f, 112f);
                Portrait.anchoredPosition = new Vector2(8f, -7f);
                Portrait.sizeDelta = new Vector2(98f, 98f);
                float center = width * 0.5f;
                Position(Hud.LevelLabel, 122f - center, 17f, 15f);
                Position(Hud.LevelValue, 122f - center, -16f, 25f);
                Position(Hud.MovesLabel, width * 0.49f - center, 17f, 15f);
                Position(Hud.MovesValue, width * 0.49f - center, -16f, 25f);
                Position(Hud.ScoreLabel, width * 0.76f - center, 17f, 15f);
                Position(Hud.ScoreValue, width * 0.76f - center, -16f, 25f);
            }
            else
            {
                TopBar.sizeDelta = new Vector2(0f, 82f);
                Portrait.anchoredPosition = new Vector2(15f, -12f);
                Portrait.sizeDelta = new Vector2(154f, 154f);
                Position(Hud.LevelLabel, -285f, 19f, 19f);
                Position(Hud.LevelValue, -285f, -15f, 27f);
                Position(Hud.MovesLabel, -105f, 19f, 19f);
                Position(Hud.MovesValue, -105f, -15f, 27f);
                Position(Hud.ScoreLabel, 105f, 19f, 19f);
                Position(Hud.ScoreValue, 105f, -15f, 27f);
            }
        }

        private static void Position(TextMeshProUGUI text, float x, float y, float fontSize)
        {
            if (text == null) return;
            text.rectTransform.anchoredPosition = new Vector2(x, y);
            text.fontSize = fontSize;
        }
    }
}
