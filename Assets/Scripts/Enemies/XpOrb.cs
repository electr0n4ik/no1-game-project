using System;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class XpOrb : MonoBehaviour
{
    [SerializeField] private float attractSpeed = 10f;

    private float value = 1f;
    private Action<XpOrb> recycle;
    private Transform player;
    private PlayerGrowth growth;

    public void Init(Transform playerTransform, float xpValue, Action<XpOrb> returnToPool)
    {
        player = playerTransform;
        value = xpValue;
        recycle = returnToPool;
        growth = player != null ? player.GetComponent<PlayerGrowth>() : null;
    }

    private void Awake()
    {
        var found = GameObject.FindWithTag("Player");
        if (found != null)
        {
            player = found.transform;
            growth = player.GetComponent<PlayerGrowth>();
        }
    }

    private void Update()
    {
        if (player == null || growth == null) return;
        if (Vector2.Distance(transform.position, player.position) > growth.MagnetRadius) return;
        transform.position = Vector3.MoveTowards(transform.position, player.position, attractSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out PlayerGrowth target)) return;
        target.AddXp(value);
        recycle?.Invoke(this);
        recycle = null;
    }
}
