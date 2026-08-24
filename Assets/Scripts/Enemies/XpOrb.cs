using System;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class XpOrb : MonoBehaviour
{
    [SerializeField] private float pickupRadiusBase = 1.7f;
    [SerializeField] private float attractSpeed = 10f;

    private float value = 1f;
    private Action<XpOrb> recycle;
    private Transform player;

    public void Init(Transform playerTransform, float xpValue, Action<XpOrb> returnToPool)
    {
        player = playerTransform;
        value = xpValue;
        recycle = returnToPool;
    }

    private void Awake()
    {
        var found = GameObject.FindGameObjectWithTag("Player");
        if (found != null) player = found.transform;
    }

    private void Update()
    {
        if (player == null) return;
        float pickupRadius = pickupRadiusBase * player.localScale.x;
        if (Vector2.Distance(transform.position, player.position) > pickupRadius) return;
        transform.position = Vector3.MoveTowards(transform.position, player.position, attractSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out PlayerGrowth growth)) return;
        growth.AddXp(value);
        recycle?.Invoke(this);
        recycle = null;
    }
}
