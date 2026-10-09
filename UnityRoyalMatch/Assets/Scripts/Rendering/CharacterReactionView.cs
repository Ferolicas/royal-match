using UnityEngine;
using UnityEngine.UI;

namespace RoyalMatch.Rendering
{
    // A single transparent atlas keeps every expression of the same character consistent.
    public sealed class CharacterReactionView : MonoBehaviour
    {
        public RawImage Portrait;
        public RectTransform PortraitTransform;

        private int _baseExpression;
        private int _expression = -1;
        private float _reactionUntil;
        private float _pulseUntil;
        private bool _victory;

        private void Awake()
        {
            if (Portrait == null) Portrait = GetComponentInChildren<RawImage>();
            if (PortraitTransform == null && Portrait != null)
                PortraitTransform = Portrait.rectTransform;
            SetExpression(0);
        }

        public void SetMoves(int remaining)
        {
            if (_victory) return;
            _baseExpression = remaining <= 3 ? 2 : 0;
            if (Time.unscaledTime >= _reactionUntil) SetExpression(_baseExpression);
        }

        public void ResetForLevel()
        {
            _victory = false;
            _baseExpression = 0;
            _reactionUntil = 0f;
            SetExpression(0);
        }

        public void CelebrateMatch(int count)
        {
            if (_victory) return;
            SetExpression(1);
            _reactionUntil = Time.unscaledTime + (count >= 5 ? 1.35f : 0.9f);
            _pulseUntil = Time.unscaledTime + 0.55f;
        }

        public void CelebrateVictory()
        {
            _victory = true;
            SetExpression(3);
            _pulseUntil = Time.unscaledTime + 1.5f;
        }

        public void ShowDefeat()
        {
            _victory = false;
            _baseExpression = 2;
            _reactionUntil = 0f;
            SetExpression(2);
        }

        private void OnEnable()
        {
            _victory = false;
            _baseExpression = 0;
            _reactionUntil = 0f;
            SetExpression(0);
        }

        private void Update()
        {
            if (!_victory && _expression == 1 && Time.unscaledTime >= _reactionUntil)
                SetExpression(_baseExpression);
            if (PortraitTransform == null) return;
            float t = Time.unscaledTime;
            float idleBob = Mathf.Sin(t * 2.1f) * 2.5f;
            float cheer = _victory ? Mathf.Abs(Mathf.Sin(t * 7.5f)) * 10f : 0f;
            PortraitTransform.anchoredPosition = new Vector2(0f, idleBob + cheer);
            float pulse = t < _pulseUntil ? 1f + Mathf.Sin(t * 22f) * 0.045f : 1f;
            PortraitTransform.localScale = Vector3.one * pulse;
        }

        private void SetExpression(int expression)
        {
            if (Portrait == null || _expression == expression) return;
            _expression = expression;
            // Atlas rows in UV space are bottom-to-top.
            Portrait.uvRect = expression switch
            {
                0 => new Rect(0f, 0.5f, 0.5f, 0.5f),
                1 => new Rect(0.5f, 0.5f, 0.5f, 0.5f),
                2 => new Rect(0f, 0f, 0.5f, 0.5f),
                _ => new Rect(0.5f, 0f, 0.5f, 0.5f)
            };
        }
    }
}
