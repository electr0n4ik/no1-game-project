using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [SerializeField] private Transform target;
    [SerializeField] private float smooth = 6f;

    private Vector3 shakeOffset;
    private float shakeUntil;
    private float shakeAmplitude;

    private void Awake()
    {
        Instance = this;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        float scale = target.localScale.x;
        float goalOrtho = Rubilovo.Logic.Kinematics.OrthoTarget(scale);
        main.orthographicSize = Rubilovo.Logic.Kinematics.OrthoLerp(main.orthographicSize, goalOrtho, Time.unscaledDeltaTime);
        Vector3 pos = transform.position;
        Vector3 goal = target.position;
        float t = smooth * Time.unscaledDeltaTime;
        pos.x = Mathf.Lerp(pos.x, goal.x, t) + shakeOffset.x;
        pos.y = Mathf.Lerp(pos.y, goal.y, t) + shakeOffset.y;
        transform.position = pos;
    }

    private Camera main;

    private void Start()
    {
        main = GetComponent<Camera>();
    }

    private void Update()
    {
        if (Time.unscaledTime >= shakeUntil)
        {
            shakeOffset = Vector3.zero;
            return;
        }
        shakeOffset = UnityEngine.Random.insideUnitCircle * shakeAmplitude;
    }

    public void Punch(float amplitude, float duration)
    {
        shakeAmplitude = amplitude;
        shakeUntil = Time.unscaledTime + duration;
    }
}
