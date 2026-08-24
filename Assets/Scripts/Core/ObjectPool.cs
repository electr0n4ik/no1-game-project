using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : Component
{
    private readonly T prefab;
    private readonly Transform parent;
    private readonly Queue<T> free = new();
    private readonly HashSet<T> activeSet = new();

    public ObjectPool(T prefab, int prewarm, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent;
        for (int i = 0; i < prewarm; i++)
        {
            free.Enqueue(Create());
        }
    }

    public int ActiveCount => activeSet.Count;

    public List<T> ActiveSnapshot() => new(activeSet);

    public T Get()
    {
        T item = free.Count > 0 ? free.Dequeue() : Create();
        item.gameObject.SetActive(true);
        activeSet.Add(item);
        return item;
    }

    public void Release(T item)
    {
        if (!activeSet.Remove(item)) return;
        item.gameObject.SetActive(false);
        free.Enqueue(item);
    }

    public void ReleaseAll()
    {
        foreach (T item in new List<T>(activeSet))
        {
            Release(item);
        }
    }

    private T Create()
    {
        T item = UnityEngine.Object.Instantiate(prefab, parent);
        item.gameObject.SetActive(false);
        return item;
    }
}
