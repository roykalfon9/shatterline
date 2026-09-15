using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    /// <summary>
    /// Fixed-size pool: Get()/Return() only. If exhausted, recycles the
    /// longest-active instance instead of instantiating a new one, so pooled
    /// objects never Instantiate/Destroy once warmed up.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        readonly Queue<T> free = new Queue<T>();
        readonly LinkedList<T> active = new LinkedList<T>();
        readonly Dictionary<T, LinkedListNode<T>> activeNodes = new Dictionary<T, LinkedListNode<T>>();

        public ObjectPool(T prefab, int size, Transform parent = null)
        {
            for (int i = 0; i < size; i++)
            {
                T instance = Object.Instantiate(prefab, parent);
                instance.gameObject.SetActive(false);
                free.Enqueue(instance);
            }
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

        public void Return(T instance)
        {
            if (activeNodes.TryGetValue(instance, out var node))
            {
                active.Remove(node);
                activeNodes.Remove(instance);
            }
            instance.gameObject.SetActive(false);
            free.Enqueue(instance);
        }
    }
}
