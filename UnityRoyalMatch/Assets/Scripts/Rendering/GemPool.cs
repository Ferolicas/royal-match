// GemPool.cs — Object pool for GemView objects

using System.Collections.Generic;
using UnityEngine;
using RoyalMatch.Rendering;

namespace RoyalMatch.Rendering
{
    public class GemPool
    {
        private readonly Queue<GameObject> _pool = new Queue<GameObject>();
        private readonly GameObject _gemPrefab;
        private readonly Transform _parent;
        private readonly int _maxSize;

        public GemPool(GameObject gemPrefab, Transform parent, int maxSize = 100)
        {
            _gemPrefab = gemPrefab;
            _parent = parent;
            _maxSize = maxSize;
        }

        public GameObject Acquire(Vector3 position, Quaternion rotation)
        {
            GameObject go;
            if (_pool.Count > 0)
            {
                go = _pool.Dequeue();
                go.transform.SetPositionAndRotation(position, rotation);
                go.transform.SetParent(_parent);
                go.SetActive(true);
            }
            else
            {
                go = Object.Instantiate(_gemPrefab, position, rotation, _parent);
            }
            return go;
        }

        public void Release(GameObject go)
        {
            if (go == null) return;
            go.SetActive(false);
            if (_parent != null)
                go.transform.SetParent(_parent);
            if (_pool.Count < _maxSize)
                _pool.Enqueue(go);
            else
                Object.Destroy(go);
        }

        public void Clear()
        {
            while (_pool.Count > 0)
            {
                Object.Destroy(_pool.Dequeue());
            }
        }
    }
}
