// Reference-only stubs. Signatures must match the real UnityEngine exactly; bodies never run.
using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace UnityEngine.SceneManagement
{
    public enum LoadSceneMode { Single = 0, Additive = 1 }

    public struct Scene
    {
        public string name => string.Empty;
        public int buildIndex => 0;
    }

    public static class SceneManager
    {
        public static event UnityAction<Scene, LoadSceneMode> sceneLoaded;
    }
}

namespace UnityEngine.XR
{
    public enum XRNode
    {
        LeftEye = 0, RightEye = 1, CenterEye = 2, Head = 3,
        LeftHand = 4, RightHand = 5, GameController = 6, TrackingReference = 7, HardwareTracker = 8
    }

    public struct InputFeatureUsage<T>
    {
        public InputFeatureUsage(string name) { }
        public string name => string.Empty;
    }

    public static class CommonUsages
    {
        public static InputFeatureUsage<bool> primaryButton => default;
        public static InputFeatureUsage<bool> secondaryButton => default;
        public static InputFeatureUsage<bool> gripButton => default;
        public static InputFeatureUsage<bool> triggerButton => default;
        public static InputFeatureUsage<float> grip => default;
        public static InputFeatureUsage<float> trigger => default;
        public static InputFeatureUsage<Vector2> primary2DAxis => default;
    }

    public struct InputDevice
    {
        public bool isValid => false;

        public bool TryGetFeatureValue(InputFeatureUsage<bool> usage, out bool value)
        { value = false; return false; }

        public bool TryGetFeatureValue(InputFeatureUsage<float> usage, out float value)
        { value = 0f; return false; }

        public bool TryGetFeatureValue(InputFeatureUsage<Vector2> usage, out Vector2 value)
        { value = default; return false; }
    }

    public static class InputDevices
    {
        public static InputDevice GetDeviceAtXRNode(XRNode node) => default;
        public static void GetDevicesAtXRNode(XRNode node, List<InputDevice> inputDevices) { }
    }
}
