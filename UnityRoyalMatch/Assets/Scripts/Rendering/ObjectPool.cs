// ObjectPool.cs — Generic object pool for reusable objects
// Ported from src/engine/EntityPool.ts

using System.Collections.Generic;
using UnityEngine;

namespace RoyalMatch.Rendering
{
    public class ObjectPool<T> where T : Component
    {
        public delegate T FactoryMethod();

        private readonly Queue<T> _pool = new Queue<T>();
        private readonly FactoryMethod _factory;
        private readonly System.Action<T> _resetAction;
        private readonly Transform _parent;

        public ObjectPool(FactoryMethod factory, System.Action<T> resetAction, Transform parent = null)
        {
            _factory = factory;
            _resetAction = resetAction;
            _parent = parent;
        }

        public T Acquire()
        {
            T obj = _pool.Count > 0 ? _pool.Dequeue() : _factory();
            _resetAction?.Invoke(obj);
            return obj;
        }

        public void Release(T obj)
        {
            if (obj == null) return;
            obj.gameObject.SetActive(false);
            if (_parent != null)
                obj.transform.SetParent(_parent);
            _pool.Enqueue(obj);
        }

        public void ReleaseAll(IEnumerable<T> objects)
        {
            foreach (var obj in objects)
                Release(obj);
        }

        public int PoolSize => _pool.Count;

        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var obj = _pool.Dequeue();
                if (obj != null)
                    Object.Destroy(obj.gameObject);
            }
        }
    }
}
