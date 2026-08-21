using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace FairPlay.Utils
{
    /// <summary>
    /// Controller input read straight from Unity's XR layer rather than from Gorilla Tag's own
    /// input poller. Same reasoning as <see cref="GameRefs"/>: <c>UnityEngine.XR</c> is a stable
    /// engine API, the game's poller is not, and this way an update to the game cannot silently
    /// change what a grip press means.
    ///
    /// Call <see cref="Sample"/> once per frame; everything else reads the sampled snapshot so a
    /// module that checks a button three times in a frame sees one consistent answer.
    /// </summary>
    public static class XRInput
    {
        public struct HandState
        {
            public float Grip;
            public float Trigger;
            public bool  Primary;      // X / A
            public bool  Secondary;    // Y / B
            public Vector2 Stick;

            public bool GripHeld    => Grip    > 0.55f;
            public bool TriggerHeld => Trigger > 0.55f;
        }

        // Feature usages are constructed by name rather than read off UnityEngine.XR.CommonUsages.
        // CommonUsages exposes these as static members whose exact form (field or property) differs
        // between Unity versions, and getting that wrong is a runtime MissingMethodException rather
        // than a compile error. The usage names themselves are part of the XR input spec and stable.
        private static readonly InputFeatureUsage<float>   GripUsage      = new InputFeatureUsage<float>("Grip");
        private static readonly InputFeatureUsage<float>   TriggerUsage   = new InputFeatureUsage<float>("Trigger");
        private static readonly InputFeatureUsage<bool>    PrimaryUsage   = new InputFeatureUsage<bool>("PrimaryButton");
        private static readonly InputFeatureUsage<bool>    SecondaryUsage = new InputFeatureUsage<bool>("SecondaryButton");
        private static readonly InputFeatureUsage<Vector2> StickUsage     = new InputFeatureUsage<Vector2>("Primary2DAxis");

        private static InputDevice _left, _right;
        private static readonly List<InputDevice> _scratch = new List<InputDevice>();
        private static int _lastSampledFrame = -1;

        public static HandState Left  { get; private set; }
        public static HandState Right { get; private set; }

        private static HandState _prevLeft, _prevRight;

        public static HandState For(bool left) => left ? Left : Right;

        public static bool PrimaryDown(bool left) =>
            left ? Left.Primary && !_prevLeft.Primary
                 : Right.Primary && !_prevRight.Primary;

        public static bool SecondaryDown(bool left) =>
            left ? Left.Secondary && !_prevLeft.Secondary
                 : Right.Secondary && !_prevRight.Secondary;

        public static bool TriggerDown(bool left) =>
            left ? Left.TriggerHeld && !_prevLeft.TriggerHeld
                 : Right.TriggerHeld && !_prevRight.TriggerHeld;

        public static bool GripDown(bool left) =>
            left ? Left.GripHeld && !_prevLeft.GripHeld
                 : Right.GripHeld && !_prevRight.GripHeld;

        /// <summary>Idempotent per frame — safe to call from several places.</summary>
        public static void Sample()
        {
            if (Time.frameCount == _lastSampledFrame) return;
            _lastSampledFrame = Time.frameCount;

            _prevLeft = Left;
            _prevRight = Right;

            EnsureDevice(ref _left, XRNode.LeftHand);
            EnsureDevice(ref _right, XRNode.RightHand);

            Left  = Read(_left);
            Right = Read(_right);
        }

        private static void EnsureDevice(ref InputDevice device, XRNode node)
        {
            if (device.isValid) return;

            _scratch.Clear();
            InputDevices.GetDevicesAtXRNode(node, _scratch);
            if (_scratch.Count > 0) device = _scratch[0];
        }

        private static HandState Read(InputDevice device)
        {
            var state = new HandState();
            if (!device.isValid) return state;

            device.TryGetFeatureValue(GripUsage, out float grip);
            device.TryGetFeatureValue(TriggerUsage, out float trigger);
            device.TryGetFeatureValue(PrimaryUsage, out bool primary);
            device.TryGetFeatureValue(SecondaryUsage, out bool secondary);
            device.TryGetFeatureValue(StickUsage, out Vector2 stick);

            state.Grip = grip;
            state.Trigger = trigger;
            state.Primary = primary;
            state.Secondary = secondary;
            state.Stick = stick;
            return state;
        }

        internal static void Invalidate()
        {
            _left = default;
            _right = default;
            Left = default;
            Right = default;
            _prevLeft = default;
            _prevRight = default;
            _lastSampledFrame = -1;
        }
    }
}
