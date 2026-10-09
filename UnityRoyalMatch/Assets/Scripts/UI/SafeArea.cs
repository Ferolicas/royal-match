// SafeArea.cs — Handles mobile safe area for UI elements

using UnityEngine;

namespace RoyalMatch.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Canvas _canvas;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            ApplySafeArea();
        }

        private void Start()
        {
            ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            if (_rectTransform == null) return;

            Rect safeArea = Screen.safeArea;

            // Convert screen safe area to canvas space
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            // Normalize to screen size
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            if (screenWidth > 0 && screenHeight > 0)
            {
                anchorMin.x /= screenWidth;
                anchorMin.y /= screenHeight;
                anchorMax.x /= screenWidth;
                anchorMax.y /= screenHeight;
            }

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }

        // Call this from a public method if screen orientation changes
        public void RefreshSafeArea()
        {
            ApplySafeArea();
        }
    }
}
