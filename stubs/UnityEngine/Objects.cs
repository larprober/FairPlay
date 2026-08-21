// Reference-only stubs. Signatures must match the real UnityEngine exactly; bodies never run.
using System;

namespace UnityEngine
{
    public class Object
    {
        public string name { get; set; }

        public static void Destroy(Object obj) { }
        public static void DestroyImmediate(Object obj) { }
        public static void DontDestroyOnLoad(Object target) { }

        public static bool operator ==(Object x, Object y) => ReferenceEquals(x, y);
        public static bool operator !=(Object x, Object y) => !ReferenceEquals(x, y);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => 0;
    }

    public class Component : Object
    {
        public Transform transform => null;
        public GameObject gameObject => null;
        public T GetComponent<T>() => default;
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
    }

    public class MonoBehaviour : Behaviour { }

    public sealed class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }

        public Transform transform => null;
        public bool activeSelf => false;
        public bool activeInHierarchy => false;

        public void SetActive(bool value) { }
        public T GetComponent<T>() => default;
        public T AddComponent<T>() where T : Component => default;

        public static GameObject CreatePrimitive(PrimitiveType type) => null;
    }

    public sealed class Transform : Component
    {
        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }
        public Vector3 localPosition { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 localScale { get; set; }
        public Transform parent { get; set; }

        public Vector3 up => Vector3.up;
        public Vector3 forward => Vector3.zero;

        public void SetParent(Transform parent, bool worldPositionStays) { }
        public Vector3 TransformPoint(Vector3 position) => position;
        public Vector3 InverseTransformPoint(Vector3 position) => position;
    }

    public class Texture : Object { }

    public sealed class Shader : Object
    {
        public static Shader Find(string name) => null;
    }

    public class Material : Object
    {
        public Material(Shader shader) { }
        public Material(Material source) { }

        public Color color { get; set; }
        public Texture mainTexture { get; set; }
        public Shader shader { get; set; }

        public bool HasProperty(string name) => false;
        public void SetColor(string name, Color value) { }
        public Color GetColor(string name) => Color.white;
        public void SetFloat(string name, float value) { }
    }

    public class Renderer : Component
    {
        public bool enabled { get; set; }
        public Material material { get; set; }
        public Material sharedMaterial { get; set; }
        public Rendering.ShadowCastingMode shadowCastingMode { get; set; }
        public bool receiveShadows { get; set; }
    }

    public sealed class MeshRenderer : Renderer { }

    public sealed class LineRenderer : Renderer
    {
        public float widthMultiplier { get; set; }
        public int positionCount { get; set; }
        public bool useWorldSpace { get; set; }
        public void SetPosition(int index, Vector3 position) { }
    }

    public sealed class TrailRenderer : Renderer
    {
        public float time { get; set; }
        public float widthMultiplier { get; set; }
        public int numCapVertices { get; set; }
        public Gradient colorGradient { get; set; }
    }

    public sealed class Font : Object
    {
        public Material material => null;
        public static Font CreateDynamicFontFromOSFont(string fontname, int size) => null;
    }

    public sealed class TextMesh : Component
    {
        public string text { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public float characterSize { get; set; }
        public Color color { get; set; }
        public TextAnchor anchor { get; set; }
        public TextAlignment alignment { get; set; }
        public bool richText { get; set; }
        public FontStyle fontStyle { get; set; }
    }

    public sealed class Camera : Component
    {
        public static Camera main => null;
    }

    public static class Resources
    {
        public static T GetBuiltinResource<T>(string path) where T : Object => default;
    }

    public class Collider : Component
    {
        public bool enabled { get; set; }
    }

    public sealed class CapsuleCollider : Collider { }
    public sealed class BoxCollider : Collider { }

    public sealed class Rigidbody : Component
    {
        public Vector3 velocity { get; set; }
        public Vector3 angularVelocity { get; set; }
        public bool useGravity { get; set; }
        public Vector3 position { get; set; }
    }

    public struct RaycastHit
    {
        public Vector3 point => Vector3.zero;
        public Collider collider => null;
    }

    public static class Physics
    {
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo,
                                   float maxDistance) { hitInfo = default; return false; }

        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo,
                                   float maxDistance, int layerMask,
                                   QueryTriggerInteraction queryTriggerInteraction)
        { hitInfo = default; return false; }
    }
}
