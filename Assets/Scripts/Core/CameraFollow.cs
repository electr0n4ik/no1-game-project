using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smooth = 6f;

    private Vector3 shakeOffset;
    private float shakeUntil;
    private float shakeAmplitude;

    private void LateUpdate()
    {
        if (target == null) return;
        Vector3 goal = target.position;
        Vector3 pos = transform.position;
        float t = smooth * Time.unscaledDeltaTime;
        pos.x = Mathf.Lerp(pos.x, goal.x, t) + shakeOffset.x;
        pos.y = Mathf.Lerp(pos.y, goal.y, t) + shakeOffset.y;
        transform.position = pos;
    }

    private void Update()
    {
        if (Time.unscaledTime >= shakeUntil)
        {
            shakeOffset = Vector3.zero;
            return;
        }
        shakeOffset = Random.insideUnitCircle * shakeAmplitude;
    }

    public void Punch(float amplitude, float duration)
    {
        shakeAmplitude = amplitude;
        shakeUntil = Time.unscaledTime + duration;
    }
}
