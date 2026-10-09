// GemView.cs — MonoBehaviour for rendering a single gem in the 2.5D scene
// Represents a gem piece with colored material and optional special effect glow

using UnityEngine;
using RoyalMatch.Types;
using RoyalMatch.Data;

namespace RoyalMatch.Rendering
{
    public enum GemMotionStyle { Swap, Drop, Return, Spawn, Fly }

    public class GemView : MonoBehaviour
    {
        [Header("Visuals")]
        public MeshRenderer MeshRenderer;
        public MeshFilter MeshFilter;
        public Mesh[] ColorMeshes;
        public MeshRenderer AccentRenderer;
        public MeshRenderer[] PremiumRenderers;
        public MeshRenderer[] SpecialRenderers;
        public ParticleSystem CollectVFX;
        public Light GlowLight;

        [Header("Animation")]
        public float MoveDuration = 0.25f;
        public float ScaleDuration = 0.3f;

        private int _row;
        private int _col;
        private PieceType _color;
        private SpecialType _special;
        private bool _isVisible = true;
        private MaterialPropertyBlock _propertyBlock;
        private Coroutine _motionRoutine;
        private Coroutine _pulseRoutine;
        private float _restScale = 1f;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        public void Initialize(int row, int col)
        {
            _row = row;
            _col = col;
        }

        public void SetData(PieceType color, SpecialType special, bool visible, float scale)
        {
            _color = color;
            _special = special;
            _isVisible = visible;

            SetColor(color);
            SetSpecial(special);
            SetVisibility(visible);
            SetScale(scale);
        }

        public int Row => _row;
        public int Col => _col;
        public PieceType Color => _color;
        public SpecialType Special => _special;
        public bool IsVisible => _isVisible;

        private void SetColor(PieceType color)
        {
            if (MeshRenderer == null) return;

            int meshIndex = (int)color;
            if (MeshFilter != null && ColorMeshes != null && meshIndex >= 0 &&
                meshIndex < ColorMeshes.Length && ColorMeshes[meshIndex] != null)
                MeshFilter.sharedMesh = ColorMeshes[meshIndex];
            bool hasPremium = HasPremiumRenderer(meshIndex);
            if (PremiumRenderers != null)
            {
                for (int i = 0; i < PremiumRenderers.Length; i++)
                    if (PremiumRenderers[i] != null)
                        PremiumRenderers[i].enabled = _isVisible && i == meshIndex;
            }
            if (AccentRenderer != null)
                AccentRenderer.enabled = _isVisible && color == PieceType.RED && !hasPremium;
            if (MeshRenderer != null)
                MeshRenderer.enabled = _isVisible && !hasPremium;

            Color gemColor = GemColors.GetColor(color);
            MeshRenderer.GetPropertyBlock(_propertyBlock);
            // URP/Lit reads _BaseColor; _Color is kept for compatibility with
            // built-in and custom shaders.
            _propertyBlock.SetColor("_BaseColor", gemColor);
            _propertyBlock.SetColor("_Color", gemColor);
            _propertyBlock.SetColor("_EmissionColor", gemColor * 0.3f);
            MeshRenderer.SetPropertyBlock(_propertyBlock);

            if (GlowLight != null)
            {
                GlowLight.color = gemColor;
                GlowLight.intensity = _special != SpecialType.NONE ? 1.5f : 0.8f;
            }
        }

        private void SetSpecial(SpecialType special)
        {
            if (special != SpecialType.NONE && CollectVFX != null)
            {
                var main = CollectVFX.main;
                Color specialColor = special switch
                {
                    SpecialType.BOMB => new Color(1f, 0.533f, 0f),
                    SpecialType.COLOR_BOMB => new Color(1f, 0.412f, 0.706f),
                    SpecialType.ROCKET => new Color(0f, 0.808f, 0.816f),
                    SpecialType.ROCKET_HORIZONTAL => new Color(0f, 0.808f, 0.816f),
                    SpecialType.ROCKET_VERTICAL => new Color(0f, 0.808f, 0.816f),
                    SpecialType.PROPELLER => new Color(0.45f, 0.90f, 1f),
                    SpecialType.LINE => UnityEngine.Color.white,
                    _ => UnityEngine.Color.white,
                };
                main.startColor = specialColor;
            }
        }

        private void SetVisibility(bool visible)
        {
            int meshIndex = (int)_color;
            bool isSpecial = _special != SpecialType.NONE;
            bool hasPremium = HasPremiumRenderer(meshIndex);
            if (MeshRenderer != null)
                MeshRenderer.enabled = visible && !hasPremium && !isSpecial;
            if (AccentRenderer != null)
                AccentRenderer.enabled = visible && _color == PieceType.RED && !hasPremium && !isSpecial;
            if (PremiumRenderers != null)
            {
                for (int i = 0; i < PremiumRenderers.Length; i++)
                    if (PremiumRenderers[i] != null)
                        PremiumRenderers[i].enabled = visible && !isSpecial && i == meshIndex;
            }
            if (SpecialRenderers != null)
            {
                int specialIndex = _special switch
                {
                    SpecialType.ROCKET_HORIZONTAL => 0,
                    SpecialType.ROCKET => 0,
                    SpecialType.LINE => 0,
                    SpecialType.ROCKET_VERTICAL => 1,
                    SpecialType.BOMB => 2,
                    SpecialType.COLOR_BOMB => 3,
                    SpecialType.PROPELLER => 4,
                    _ => -1,
                };
                for (int i = 0; i < SpecialRenderers.Length; i++)
                    if (SpecialRenderers[i] != null)
                        SpecialRenderers[i].enabled = visible && isSpecial && i == specialIndex;
            }
            if (CollectVFX != null)
                CollectVFX.gameObject.SetActive(visible);
            if (GlowLight != null)
                GlowLight.enabled = visible;
        }

        private void SetScale(float scale)
        {
            _restScale = scale;
            transform.localScale = new Vector3(scale, scale, scale);
        }

        private bool HasPremiumRenderer(int meshIndex)
        {
            return PremiumRenderers != null && meshIndex >= 0 &&
                meshIndex < PremiumRenderers.Length && PremiumRenderers[meshIndex] != null;
        }

        public void MoveTo(Vector3 targetPosition, float duration, System.Action onComplete = null,
            GemMotionStyle style = GemMotionStyle.Swap)
        {
            if (_motionRoutine != null) StopCoroutine(_motionRoutine);
            if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
            if (duration <= 0f)
            {
                transform.position = targetPosition;
                transform.localScale = Vector3.one * _restScale;
                transform.localRotation = Quaternion.identity;
                _motionRoutine = null;
                onComplete?.Invoke();
                return;
            }
            _motionRoutine = StartCoroutine(MoveCoroutine(targetPosition, duration, onComplete, style));
        }

        private System.Collections.IEnumerator MoveCoroutine(Vector3 target, float duration,
            System.Action onComplete, GemMotionStyle style)
        {
            Vector3 startPos = transform.position;
            Vector3 direction = target - startPos;
            float startScale = transform.localScale.x;
            float lift = style == GemMotionStyle.Fly ? 0.65f :
                style == GemMotionStyle.Swap ? 0.18f :
                style == GemMotionStyle.Drop ? 0.12f : 0.06f;
            float pulse = style == GemMotionStyle.Swap ? 0.11f : 0.07f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                float arc = Mathf.Sin(t * Mathf.PI);
                transform.position = Vector3.LerpUnclamped(startPos, target, eased) + Vector3.up * (arc * lift);
                transform.localScale = Vector3.one *
                    (Mathf.Lerp(startScale, _restScale, t) * (1f + arc * pulse));
                transform.localRotation = style == GemMotionStyle.Fly
                    ? Quaternion.Euler(0f, t * 720f, 0f)
                    : Quaternion.Euler(direction.z * arc * 6f, 0f, -direction.x * arc * 6f);
                yield return null;
            }

            transform.position = target;
            transform.localScale = Vector3.one * _restScale;
            transform.localRotation = Quaternion.identity;
            _motionRoutine = null;
            onComplete?.Invoke();
        }

        public void PopScale(float targetScale = 1.2f, float duration = 0.3f)
        {
            if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
            if (duration <= 0f)
            {
                transform.localScale = Vector3.one * _restScale;
                _pulseRoutine = null;
                return;
            }
            _pulseRoutine = StartCoroutine(PulseCoroutine(targetScale, duration));
        }

        private System.Collections.IEnumerator PulseCoroutine(float target, float duration)
        {
            float startScale = transform.localScale.x;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float rise = t < 0.35f ? Mathf.SmoothStep(0f, 1f, t / 0.35f) :
                    1f - Mathf.SmoothStep(0f, 1f, (t - 0.35f) / 0.65f);
                float scale = Mathf.Lerp(startScale, target, rise);
                transform.localScale = Vector3.one * scale;
                yield return null;
            }

            transform.localScale = Vector3.one * _restScale;
            _pulseRoutine = null;
        }

        public void PlayCollectVFX()
        {
            if (CollectVFX != null && _isVisible)
                CollectVFX.Play();
            if (_motionRoutine != null) StopCoroutine(_motionRoutine);
            if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
            _motionRoutine = StartCoroutine(CollectCoroutine());
        }

        private System.Collections.IEnumerator CollectCoroutine()
        {
            Vector3 startPosition = transform.position;
            float startScale = transform.localScale.x;
            float elapsed = 0f;
            const float duration = 0.22f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = t < 0.3f
                    ? Mathf.Lerp(startScale, startScale * 1.22f, t / 0.3f)
                    : Mathf.Lerp(startScale * 1.22f, 0f, (t - 0.3f) / 0.7f);
                transform.localScale = Vector3.one * scale;
                transform.position = startPosition + Vector3.up * (0.16f * t);
                transform.localRotation = Quaternion.Euler(0f, 0f, 10f * t);
                yield return null;
            }
            _motionRoutine = null;
        }

        public void ResetVisual()
        {
            if (_motionRoutine != null) StopCoroutine(_motionRoutine);
            if (_pulseRoutine != null) StopCoroutine(_pulseRoutine);
            _motionRoutine = null;
            _pulseRoutine = null;
            transform.localScale = Vector3.one * _restScale;
            transform.localRotation = Quaternion.identity;
            if (CollectVFX != null) CollectVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
