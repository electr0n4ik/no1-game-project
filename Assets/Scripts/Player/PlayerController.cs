using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float acceleration = 24f;
    [SerializeField] private Health health;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private float hurtShakeAmplitude = 0.15f;
    [SerializeField] private float hurtShakeDuration = 0.12f;

    private Rigidbody2D body;
    private Vector2 input;

    private static Vector2 touchOrigin;
    private static bool dragging;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    private void OnDestroy()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
    }

    private void Update()
    {
        input = ReadInput();
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }
        float sizeSlowdown = 1f / Mathf.Sqrt(transform.localScale.x);
        Vector2 targetVelocity = input.sqrMagnitude > 0.01f ? input.normalized * (moveSpeed * sizeSlowdown) : Vector2.zero;
        body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
    }

    public void Revive()
    {
        health.FullHeal();
    }

    private void OnDamaged(Health _)
    {
        if (cameraFollow != null) cameraFollow.Punch(hurtShakeAmplitude, hurtShakeDuration);
    }

    private void OnDied(Health _)
    {
        GameManager.Instance.HandleDeath();
    }

    private static Vector2 ReadInput()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        Vector2 keys = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (keys.sqrMagnitude > 0.01f) return keys;
#endif
        if (Input.touchCount == 0) return Vector2.zero;
        return TouchDirection(Input.GetTouch(0));
    }

    private static Vector2 TouchDirection(Touch touch)
    {
        const float maxPixels = 120f;
        switch (touch.phase)
        {
            case TouchPhase.Began:
                dragging = true;
                touchOrigin = touch.position;
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                dragging = false;
                break;
        }
        if (!dragging) return Vector2.zero;
        return Vector2.ClampMagnitude((touch.position - touchOrigin) / maxPixels, 1f);
    }
}
