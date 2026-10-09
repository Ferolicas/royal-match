using System.Collections;
using UnityEngine;
using RoyalMatch.Types;

namespace RoyalMatch.Rendering
{
    public class ObstacleView : MonoBehaviour
    {
        [Header("References")]
        public MeshRenderer MeshRenderer;
        public GameObject[] HealthStages;
        public GameObject BoxDecor;
        public GameObject CageDecor;
        public GameObject IceDecor;
        public GameObject StoneDecor;
        public Transform[] BreakPieces;

        private int _row;
        private int _col;
        private CellContent _content;
        private int _health;
        private MaterialPropertyBlock _propertyBlock;
        private Coroutine _hitRoutine;
        private Vector3 _normalScale;

        public int Row => _row;
        public int Col => _col;
        public int Health => _health;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _normalScale = transform.localScale;
            if (MeshRenderer == null) MeshRenderer = GetComponent<MeshRenderer>();
        }

        public void Initialize(int row, int col)
        {
            _row = row;
            _col = col;
        }

        public void SetData(CellContent content, int health)
        {
            _content = content;
            _health = health;
            SetVisual();
        }

        public void PlayHit(int healthAfter, float duration)
        {
            if (_hitRoutine != null) StopCoroutine(_hitRoutine);
            _hitRoutine = StartCoroutine(HitRoutine(healthAfter, Mathf.Max(0.12f, duration)));
        }

        private IEnumerator HitRoutine(int healthAfter, float duration)
        {
            bool destroyed = healthAfter <= 0;
            var origin = transform.localPosition;
            var baseScale = _normalScale;
            var piecePositions = new Vector3[BreakPieces != null ? BreakPieces.Length : 0];
            for (int i = 0; i < piecePositions.Length; i++)
                if (BreakPieces[i] != null) piecePositions[i] = BreakPieces[i].localPosition;

            if (!destroyed)
            {
                _health = healthAfter;
                SetVisual();
            }
            else
            {
                var collider = GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float envelope = Mathf.Sin(t * Mathf.PI);
                transform.localPosition = origin + new Vector3(
                    Mathf.Sin(t * 42f) * 0.055f * envelope,
                    0.06f * envelope,
                    Mathf.Cos(t * 38f) * 0.045f * envelope);
                float scale = destroyed ? Mathf.Lerp(1.08f, 0.18f, t * t) : 1f + 0.12f * envelope;
                transform.localScale = baseScale * scale;

                if (destroyed)
                {
                    for (int i = 0; i < piecePositions.Length; i++)
                    {
                        var piece = BreakPieces[i];
                        if (piece == null) continue;
                        float angle = (i + 0.5f) * Mathf.PI * 2f / piecePositions.Length;
                        piece.localPosition = piecePositions[i] + new Vector3(
                            Mathf.Cos(angle) * t * 0.37f,
                            t * 0.30f,
                            Mathf.Sin(angle) * t * 0.37f);
                        piece.localRotation = Quaternion.Euler(0f, t * (i % 2 == 0 ? 90f : -90f), t * 25f);
                    }
                }
                yield return null;
            }

            transform.localPosition = origin;
            transform.localScale = baseScale;
            _hitRoutine = null;
        }

        private void OnDisable()
        {
            if (_hitRoutine != null) StopCoroutine(_hitRoutine);
            _hitRoutine = null;
            transform.localScale = _normalScale == Vector3.zero ? Vector3.one : _normalScale;
        }

        private void SetVisual()
        {
            if (MeshRenderer == null) return;
            Color color = _content switch
            {
                CellContent.OBSTACLE_BOX => new Color(0.29f, 0.08f, 0.47f),
                CellContent.OBSTACLE_CAGE => new Color(0.31f, 0.07f, 0.22f),
                CellContent.OBSTACLE_ICE => new Color(0.10f, 0.42f, 0.78f),
                CellContent.OBSTACLE_STONE => new Color(0.23f, 0.20f, 0.42f),
                _ => new Color(0.16f, 0.14f, 0.20f),
            };

            MeshRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", color);
            _propertyBlock.SetColor("_Color", color);
            _propertyBlock.SetColor("_EmissionColor", color * 0.18f);
            MeshRenderer.SetPropertyBlock(_propertyBlock);

            if (BoxDecor != null) BoxDecor.SetActive(_content == CellContent.OBSTACLE_BOX);
            if (CageDecor != null) CageDecor.SetActive(_content == CellContent.OBSTACLE_CAGE);
            if (IceDecor != null) IceDecor.SetActive(_content == CellContent.OBSTACLE_ICE);
            if (StoneDecor != null) StoneDecor.SetActive(_content == CellContent.OBSTACLE_STONE);

            int maxHealth = _content == CellContent.OBSTACLE_ICE ? 3 :
                _content == CellContent.OBSTACLE_CAGE ? 2 : 1;
            int damage = Mathf.Clamp(maxHealth - _health, 0, maxHealth - 1);
            if (HealthStages == null) return;
            for (int i = 0; i < HealthStages.Length; i++)
                if (HealthStages[i] != null) HealthStages[i].SetActive(i < damage);
        }
    }
}
