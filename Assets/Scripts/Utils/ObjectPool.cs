using System.Collections.Generic;
using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Prewarmed component pool. Prefabs carry all components; no AddComponent at runtime (CODE_RULES 5.3).</summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Stack<T> _free;

        public ObjectPool(T prefab, Transform parent, int prewarmCount)
        {
            _prefab = prefab;
            _parent = parent;
            _free = new Stack<T>(prewarmCount);
            for (int i = 0; i < prewarmCount; i++)
            {
                T instance = Object.Instantiate(_prefab, _parent);
                instance.gameObject.SetActive(false);
                _free.Push(instance);
            }
        }

        public T Get()
        {
            T instance;
            if (_free.Count > 0)
            {
                instance = _free.Pop();
            }
            else
            {
                Debug.LogWarning("[Pool] Expanding pool for " + _prefab.name + " - consider raising its prewarm size.");
                instance = Object.Instantiate(_prefab, _parent);
            }
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Release(T instance)
        {
            instance.gameObject.SetActive(false);
            _free.Push(instance);
        }
    }
}
