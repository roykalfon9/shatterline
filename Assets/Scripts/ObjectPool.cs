using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    /// <summary>
    /// Fixed-size pool. Get recycles the oldest active effect when full;
    /// TryGet refuses acquisition so active gameplay objects keep their leases.
    /// Return is idempotent. No instances are allocated after warmup.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        readonly Queue<T> free = new Queue<T>();
        readonly LinkedList<T> active = new LinkedList<T>();
        readonly Dictionary<T, LinkedListNode<T>> activeNodes = new Dictionary<T, LinkedListNode<T>>();

        public ObjectPool(T prefab, int size, Transform parent = null)
        {
            if (prefab == null) throw new System.ArgumentNullException(nameof(prefab));
            if (size < 1) throw new System.ArgumentOutOfRangeException(nameof(size));

            for (int i = 0; i < size; i++)
            {
                T instance = Object.Instantiate(prefab, parent);
                instance.gameObject.SetActive(false);
                free.Enqueue(instance);
            }
        }

        // Gameplay objects must not steal an active lease when the pool is full.
        public bool TryGet(out T instance)
        {
            instance = null;
            if (free.Count == 0) return false;
            instance = Get();
            return true;
        }

        public T Get()
        {
            T instance;
            if (free.Count > 0)
            {
                instance = free.Dequeue();
            }
            else
            {
                var oldest = active.First;
                instance = oldest.Value;
                active.Remove(oldest);
                activeNodes.Remove(instance);
            }

            instance.gameObject.SetActive(true);
            activeNodes[instance] = active.AddLast(instance);
            return instance;
        }

        public void ReturnAll()
        {
            while (active.First != null)
                Return(active.First.Value);
        }

        public void Return(T instance)
        {
            if (instance == null || !activeNodes.TryGetValue(instance, out var node))
                return;

            active.Remove(node);
            activeNodes.Remove(instance);
            instance.gameObject.SetActive(false);
            free.Enqueue(instance);
        }
    }
}
