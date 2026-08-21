// Reference-only stubs. Signatures must match the real UnityEngine exactly; bodies never run.
using System;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 operator *(Vector2 a, float d) => new Vector2(a.x * d, a.y * d);
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 zero => new Vector2(0f, 0f);
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

        public static Vector3 zero => new Vector3(0f, 0f, 0f);
        public static Vector3 one  => new Vector3(1f, 1f, 1f);
        public static Vector3 up   => new Vector3(0f, 1f, 0f);
        public static Vector3 down => new Vector3(0f, -1f, 0f);

        public float magnitude => 0f;
        public float sqrMagnitude => 0f;
        public Vector3 normalized => this;

        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator *(float d, Vector3 a) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator /(Vector3 a, float d) => new Vector3(a.x / d, a.y / d, a.z / d);

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a;
        public override string ToString() => string.Empty;
        public string ToString(string format) => string.Empty;
    }

    public struct Quaternion
    {
        public static Quaternion identity => new Quaternion();
        public static Quaternion LookRotation(Vector3 forward, Vector3 upwards) => identity;
        public static Quaternion LookRotation(Vector3 forward) => identity;
        public static Vector3 operator *(Quaternion rotation, Vector3 point) => point;
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1f, 1f, 1f, 1f);
        public static Color HSVToRGB(float h, float s, float v) => white;
        public static Color operator *(Color a, float d) => a;
    }

    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color(Color32 c) => new Color(0f, 0f, 0f, 0f);
        public static implicit operator Color32(Color c) => new Color32(0, 0, 0, 0);
    }

    public struct GradientColorKey
    {
        public GradientColorKey(Color color, float time) { }
    }

    public struct GradientAlphaKey
    {
        public GradientAlphaKey(float alpha, float time) { }
    }

    public class Gradient
    {
        public void SetKeys(GradientColorKey[] colorKeys, GradientAlphaKey[] alphaKeys) { }
    }

    public static class Mathf
    {
        public static float Abs(float f) => f;
        public static int Abs(int value) => value;
        public static float Min(float a, float b) => a;
        public static int Min(int a, int b) => a;
        public static float Max(float a, float b) => a;
        public static int Max(int a, int b) => a;
        public static float Repeat(float t, float length) => t;
        public static float Sin(float f) => f;
        public static int RoundToInt(float f) => 0;
        public static int CeilToInt(float f) => 0;
        public static float Lerp(float a, float b, float t) => a;
    }

    public static class Time
    {
        public static float unscaledTime => 0f;
        public static float deltaTime => 0f;
        public static float fixedDeltaTime => 0f;
        public static float smoothDeltaTime => 0f;
        public static int frameCount => 0;
    }

    public static class Input
    {
        public static bool GetKeyDown(KeyCode key) => false;
    }

    public enum KeyCode { None = 0, F8 = 289 }

    public enum PrimitiveType { Sphere = 0, Capsule = 1, Cylinder = 2, Cube = 3, Plane = 4, Quad = 5 }

    public enum TextAnchor
    {
        UpperLeft = 0, UpperCenter = 1, UpperRight = 2,
        MiddleLeft = 3, MiddleCenter = 4, MiddleRight = 5,
        LowerLeft = 6, LowerCenter = 7, LowerRight = 8
    }

    public enum TextAlignment { Left = 0, Center = 1, Right = 2 }

    public enum FontStyle { Normal = 0, Bold = 1, Italic = 2, BoldAndItalic = 3 }

    public enum QueryTriggerInteraction { UseGlobal = 0, Ignore = 1, Collide = 2 }
}

namespace UnityEngine.Rendering
{
    public enum ShadowCastingMode { Off = 0, On = 1, TwoSided = 2, ShadowsOnly = 3 }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction<T0, T1>(T0 arg0, T1 arg1);
}
