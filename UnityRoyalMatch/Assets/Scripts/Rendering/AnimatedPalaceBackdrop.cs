using UnityEngine;

namespace RoyalMatch.Rendering
{
    public sealed class AnimatedPalaceBackdrop : MonoBehaviour
    {
        public Camera BoardCamera;
        private Vector3 _origin;
        private int _lastWidth;
        private int _lastHeight;
        private float _lastCameraSize;
        private float _lastCameraZ;
        private Vector3 _baseScale;

        private void Start()
        {
            if (BoardCamera == null) BoardCamera = Camera.main;
            Resize();
        }

        private void LateUpdate()
        {
            if (Screen.width != _lastWidth || Screen.height != _lastHeight ||
                !Mathf.Approximately(BoardCamera.orthographicSize, _lastCameraSize) ||
                !Mathf.Approximately(BoardCamera.transform.position.z, _lastCameraZ)) Resize();
            float t = Time.unscaledTime;
            transform.position = _origin + new Vector3(Mathf.Sin(t * 0.22f) * 0.10f, 0f,
                Mathf.Sin(t * 0.32f) * 0.12f);
            transform.localScale = _baseScale * (1f + Mathf.Sin(t * 0.7f) * 0.005f);
        }

        private void Resize()
        {
            if (BoardCamera == null) return;
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            _lastCameraSize = BoardCamera.orthographicSize;
            _lastCameraZ = BoardCamera.transform.position.z;
            float height = BoardCamera.orthographicSize * 2f * 1.06f;
            float width = height * BoardCamera.aspect;
            _baseScale = new Vector3(width, height, 1f);
            transform.localScale = _baseScale;
            _origin = new Vector3(0f, -0.65f, BoardCamera.transform.position.z);
            transform.position = _origin;
        }
    }
}
