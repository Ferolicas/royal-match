// VFXManager.cs — pooled, layered board effects

using System.Collections.Generic;
using UnityEngine;

namespace RoyalMatch.Rendering
{
    public enum VFXType
    {
        MatchPop,
        CascadeSparkle,
        SpecialExplosion,
        LevelComplete,
        GemSpawn,
    }

    public class VFXManager : MonoBehaviour
    {
        [Header("Prefabs")]
        public ParticleSystem MatchPopVFX;
        public ParticleSystem CascadeSparkleVFX;
        public ParticleSystem SpecialExplosionVFX;
        public ParticleSystem LevelCompleteVFX;
        public ParticleSystem GemSpawnVFX;

        private readonly Dictionary<VFXType, Queue<ParticleSystem>> _pools =
            new Dictionary<VFXType, Queue<ParticleSystem>>();
        private readonly Dictionary<ParticleSystem, VFXType> _activeEffects =
            new Dictionary<ParticleSystem, VFXType>();
        private const int MaxPooledPerType = 24;

        public void Emit(VFXType type, Vector3 position, Color? tintColor = null)
        {
            ParticleSystem prefab = type switch
            {
                VFXType.MatchPop => MatchPopVFX,
                VFXType.CascadeSparkle => CascadeSparkleVFX,
                VFXType.SpecialExplosion => SpecialExplosionVFX,
                VFXType.LevelComplete => LevelCompleteVFX,
                VFXType.GemSpawn => GemSpawnVFX,
                _ => MatchPopVFX,
            };

            if (prefab == null) return;

            if (!_pools.TryGetValue(type, out var pool))
            {
                pool = new Queue<ParticleSystem>();
                _pools[type] = pool;
            }

            var instance = pool.Count > 0
                ? pool.Dequeue()
                : Instantiate(prefab, transform);
            instance.transform.SetPositionAndRotation(position + Vector3.up * 0.28f, Quaternion.identity);
            instance.gameObject.SetActive(true);

            var rootMain = instance.main;
            rootMain.startColor = tintColor ?? prefab.main.startColor.color;
            var ring = instance.transform.Find("TintRing");
            var prefabRing = prefab.transform.Find("TintRing");
            if (ring != null && prefabRing != null)
            {
                var ringSystem = ring.GetComponent<ParticleSystem>();
                var prefabRingSystem = prefabRing.GetComponent<ParticleSystem>();
                var ringMain = ringSystem.main;
                ringMain.startColor = tintColor ?? prefabRingSystem.main.startColor.color;
            }

            foreach (var system in instance.GetComponentsInChildren<ParticleSystem>(true))
                system.Clear(true);
            _activeEffects[instance] = type;
            instance.Play(true);
            StartCoroutine(ReleaseWhenFinished(instance, type));
        }

        private System.Collections.IEnumerator ReleaseWhenFinished(ParticleSystem instance, VFXType type)
        {
            while (instance != null && instance.IsAlive(true))
                yield return null;
            if (instance == null || !_activeEffects.Remove(instance)) yield break;

            instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            instance.gameObject.SetActive(false);
            var pool = _pools[type];
            if (pool.Count < MaxPooledPerType) pool.Enqueue(instance);
            else Destroy(instance.gameObject);
        }

        public void Clear()
        {
            foreach (var kvp in _activeEffects)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    Destroy(kvp.Key.gameObject);
                }
            }
            _activeEffects.Clear();
            foreach (var pool in _pools.Values)
                while (pool.Count > 0)
                {
                    var instance = pool.Dequeue();
                    if (instance != null) Destroy(instance.gameObject);
                }
        }

        public int GetActiveCount() => _activeEffects.Count;
    }
}
