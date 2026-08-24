using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public string name;
        public static void Destroy(Object o) { }
        public static void DontDestroyOnLoad(Object o) { }
        public static T Instantiate<T>(T original, Transform parent = null) where T : Object => null;
        public static T FindFirstByType<T>() where T : Object => null;
        public bool TryGetComponent<T>(out T c) where T : class { c = null; return false; }
    }

    public class Component : Object
    {
        public GameObject gameObject => null;
        public Transform transform => null;
        public T GetComponent<T>() => default;
        public T AddComponent<T>() where T : Component, new() => new T();
        public Coroutine StartCoroutine(IEnumerator r) => null;
        public void StopCoroutine(Coroutine c) { }
        public void StopAllCoroutines() { }
        public bool CompareTag(string tag) => false;
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
    }

    public class MonoBehaviour : Behaviour { }

    [AttributeUsage(AttributeTargets.Class)]
    public class DefaultExecutionOrderAttribute : Attribute
    {
        public DefaultExecutionOrderAttribute(int order) { }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeFieldAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
    public class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class RequireComponentAttribute : Attribute
    {
        public RequireComponentAttribute(Type t) { }
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float magnitude => 0f;
        public float sqrMagnitude => 0f;
        public Vector2 normalized => this;
        public static Vector2 zero => new(0, 0);
        public static Vector2 one => new(1, 1);
        public static Vector2 up => new(0, 1);
        public static Vector2 down => new(0, -1);
        public static Vector2 right => new(1, 0);
        public static Vector2 left => new(-1, 0);
        public static float Distance(Vector2 a, Vector2 b) => 0f;
        public static Vector2 MoveTowards(Vector2 c, Vector2 t, float d) => t;
        public static Vector2 ClampMagnitude(Vector2 v, float m) => v;
        public static Vector2 operator +(Vector2 a, Vector2 b) => a;
        public static Vector2 operator -(Vector2 a, Vector2 b) => a;
        public static Vector2 operator *(Vector2 a, float d) => a;
        public static Vector2 operator /(Vector2 a, float d) => a;
        public static Vector2 operator -(Vector2 a) => a;
        public static bool operator ==(Vector2 a, Vector2 b) => true;
        public static bool operator !=(Vector2 a, Vector2 b) => false;
        public override bool Equals(object o) => true;
        public override int GetHashCode() => 0;
        public static implicit operator Vector3(Vector2 v) => default;
        public static explicit operator Vector2(Vector3 v) => default;
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; z = 0f; }
        public static Vector3 zero => new(0, 0, 0);
        public static Vector3 one => new(1, 1, 1);
        public static Vector3 up => new(0, 1, 0);
        public static Vector3 MoveTowards(Vector3 c, Vector3 t, float d) => t;
        public static float Distance(Vector3 a, Vector3 b) => 0f;
        public Vector3 normalized => this;
        public static Vector3 operator +(Vector3 a, Vector3 b) => a;
        public static Vector3 operator -(Vector3 a, Vector3 b) => a;
        public static Vector3 operator *(Vector3 a, float d) => a;
        public static implicit operator Vector2(Vector3 v) => default;
    }

    public static class Mathf
    {
        public const float PI = 3.14159265f;
        public const float Deg2Rad = 0.01745329f;
        public const float Rad2Deg = 57.29578f;
        public static float Min(float a, float b) => 0f;
        public static int Min(int a, int b) => 0;
        public static float Max(float a, float b) => 0f;
        public static int Max(int a, int b) => 0;
        public static float Abs(float f) => 0f;
        public static int Abs(int i) => 0;
        public static float Sqrt(float f) => 0f;
        public static int CeilToInt(float f) => 0;
        public static int FloorToInt(float f) => 0;
        public static int RoundToInt(float f) => 0;
        public static float Repeat(float t, float len) => 0f;
        public static float Clamp(float v, float min, float max) => 0f;
        public static int Clamp(int v, int min, int max) => 0;
        public static float Clamp01(float v) => 0f;
        public static float Lerp(float a, float b, float t) => 0f;
        public static bool Approximately(float a, float b) => false;
        public static float Atan2(float y, float x) => 0f;
        public static float Cos(float f) => 0f;
        public static float Sin(float f) => 0f;
        public static float Exp(float f) => 0f;
        public static float Pow(float f, float p) => 0f;
    }

    public static class Random
    {
        public static float Range(float min, float max) => 0f;
        public static int Range(int min, int max) => 0;
        public static Vector2 insideUnitCircle => default;
    }

    public static class Time
    {
        public static float deltaTime => 0f;
        public static float fixedDeltaTime => 0f;
        public static float unscaledTime => 0f;
        public static float unscaledDeltaTime => 0f;
        public static float timeScale { get; set; }
        public static float time => 0f;
        public static float realtimeSinceStartup => 0f;
    }

    public class Coroutine { }

    public class YieldInstruction { }

    public class WaitForSeconds : YieldInstruction
    {
        public WaitForSeconds(float s) { }
    }

    public class GameObject : Object
    {
        public bool activeInHierarchy => false;
        public Transform transform => null;
        public GameObject(string name) { }
        public T GetComponent<T>() => default;
        public T AddComponent<T>() where T : Component, new() => new T();
        public void SetActive(bool value) { }
        public static GameObject FindWithTag(string tag) => null;
    }

    public class Transform : Component
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localScale { get; set; }
        public new GameObject gameObject => null;
        public void SetParent(Transform parent, bool worldPositionStays) { }
        public void SetParent(Transform parent) { }
    }

    public class Rigidbody2D : Behaviour
    {
        public Vector2 position { get; set; }
        public Vector2 linearVelocity { get; set; }
        public void MovePosition(Vector2 pos) { }
    }

    public class Collider2D : Behaviour
    {
        public bool isTrigger { get; set; }
        public bool IsTouching(Collider2D other) => false;
    }

    public class CircleCollider2D : Collider2D
    {
        public float radius { get; set; }
    }

    public static class Physics2D
    {
        public static Collider2D[] OverlapCircleAll(Vector2 center, float radius) => Array.Empty<Collider2D>();
        public static Collider2D[] OverlapBoxAll(Vector2 center, Vector2 size, float angle) => Array.Empty<Collider2D>();
    }

    public class SpriteRenderer : Behaviour
    {
        public Color color { get; set; }
    }

    public struct Color
    {
        public Color(float r, float g, float b) { }
        public Color(float r, float g, float b, float a) { }
        public static Color white => new(1, 1, 1);
    }

    public class Camera : Behaviour
    {
        public float orthographicSize { get; set; }
        public float aspect => 1f;
    }

    public static class Input
    {
        public static int touchCount => 0;
        public static Touch GetTouch(int index) => default;
        public static float GetAxisRaw(string axis) => 0f;
    }

    public struct Touch
    {
        public Vector2 position;
        public TouchPhase phase;
    }

    public enum TouchPhase { Began, Moved, Stationary, Ended, Canceled }

    public static class Debug
    {
        public static void Log(object m) { }
        public static void LogWarning(object m) { }
    }

    public static class PlayerPrefs
    {
        public static string GetString(string k, string d = "") => "";
        public static void SetString(string k, string v) { }
        public static void Save() { }
    }

    public static class JsonUtility
    {
        public static T FromJson<T>(string json) => default;
        public static string ToJson<T>(T obj) => "";
    }

    public static class Application
    {
        public static string persistentDataPath => "";
        public static bool isEditor => true;
    }
}

namespace UnityEngine.SceneManagement
{
    using System;

    public struct Scene
    {
        public int buildIndex => 0;
    }

    public enum LoadSceneMode { Single, Additive }

    public static class SceneManager
    {
        public static event Action<Scene, LoadSceneMode> sceneLoaded;
        public static Scene GetActiveScene() => default;
        public static void LoadScene(int index) { }
        public static void LoadScene(string name) { }
    }
}

namespace TMPro
{
    public class TMP_Text : UnityEngine.MonoBehaviour
    {
        public string text { get; set; }
    }
}

namespace UnityEngine.UI
{
    public class Button : UnityEngine.MonoBehaviour
    {
        public bool interactable { get; set; }
        public ButtonClickedEvent onClick { get; } = new();

        public class ButtonClickedEvent
        {
            public void AddListener(Action call) { }
            public void RemoveListener(Action call) { }
            public void RemoveAllListeners() { }
        }
    }

    public class Image : UnityEngine.MonoBehaviour
    {
        public float fillAmount { get; set; }
    }
}
