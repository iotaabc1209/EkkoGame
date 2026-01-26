using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private int prewarm = 128;

    private readonly Queue<Bullet> _pool = new();

    private void Awake()
    {
        for (int i = 0; i < prewarm; i++)
            CreateOne();
    }

    private Bullet CreateOne()
    {
        var b = Instantiate(bulletPrefab, transform);
        b.gameObject.SetActive(false);
        _pool.Enqueue(b);
        return b;
    }

    public Bullet Spawn(Vector3 pos, Vector2 velocity)
    {
        if (_pool.Count == 0) CreateOne();

        var b = _pool.Dequeue();
        b.transform.position = pos;
        b.transform.rotation = Quaternion.identity;
        b.Init(velocity, Release);
        return b;
    }

    private void Release(Bullet b)
    {
        _pool.Enqueue(b);
    }

    public void DespawnAll()
    {
    foreach (Transform child in transform)
    	{
        	if (child.gameObject.activeSelf)
        	{
            	child.gameObject.SetActive(false);
        	}
        }
    }

}
