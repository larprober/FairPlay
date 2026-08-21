using UnityEngine;
using FairPlay.Core;
using FairPlay.Menu;
using FairPlay.Utils;

namespace FairPlay.Modules
{
    /// <summary>
    /// Float inside a bubble and punch to steer it.
    ///
    /// Hand velocity is measured here rather than read off the game, because the rig exposes it
    /// inconsistently between versions and a position delta over a frame is both trivial and exact.
    /// A punch pushes you the way you punched, which is backwards from how thrusters work and
    /// forwards from how people expect punching to work.
    /// </summary>
    public sealed class BubbleModule : Module
    {
        public override string Name => "Bubble";
        public override string Description => "Float, punch to steer";
        public override ModuleCategory Category => ModuleCategory.Movement;

        private const float PunchThreshold = 1.6f;   // m/s of hand movement
        private const float PunchPower = 0.55f;      // how much of the punch becomes velocity
        private const float Drag = 0.03f;            // per fixed step
        private const float MaxSpeed = 9f;

        private bool _holdsGravity;

        private GameObject _shell;
        private Vector3 _lastLeft, _lastRight;
        private bool _haveLast;

        protected override void OnEnabled()
        {
            if (GameRefs.Body != null)
            {
                GravityLock.Claim();
                _holdsGravity = true;
            }

            _haveLast = false;
            BuildShell();
        }

        public override void Tick()
        {
            if (_shell == null) BuildShell();

            Transform root = GameRefs.Root;
            if (_shell != null && root != null) _shell.transform.position = root.position;
        }

        public override void FixedTick()
        {
            var body = GameRefs.Body;
            Transform left = GameRefs.LeftHand;
            Transform right = GameRefs.RightHand;
            if (body == null || left == null || right == null) return;

            if (!_holdsGravity)
            {
                GravityLock.Claim();
                _holdsGravity = true;
            }

            float step = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : 0.02f;

            if (_haveLast)
            {
                Push(body, (left.position - _lastLeft) / step);
                Push(body, (right.position - _lastRight) / step);
            }

            _lastLeft = left.position;
            _lastRight = right.position;
            _haveLast = true;

            // Gentle drag so the bubble drifts to a stop instead of pinballing forever.
            body.velocity = Vector3.Lerp(body.velocity, Vector3.zero, Drag);
        }

        private static void Push(Rigidbody body, Vector3 handVelocity)
        {
            float speed = handVelocity.magnitude;
            if (speed < PunchThreshold) return;

            Vector3 pushed = body.velocity + handVelocity * PunchPower;
            if (pushed.magnitude > MaxSpeed) pushed = pushed.normalized * MaxSpeed;

            body.velocity = pushed;
        }

        private void BuildShell()
        {
            Transform root = GameRefs.Root;
            if (root == null) return;

            _shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _shell.name = "FairPlayBubble";
            Object.Destroy(_shell.GetComponent<Collider>());
            Object.DontDestroyOnLoad(_shell);

            _shell.transform.localScale = Vector3.one * 1.5f;

            var renderer = _shell.GetComponent<Renderer>();
            renderer.material = Draw.Flat(MenuTheme.Accent);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public override void Revert()
        {
            if (_shell != null)
            {
                Object.Destroy(_shell);
                _shell = null;
            }

            _haveLast = false;

            if (!_holdsGravity) return;

            GravityLock.Release();
            _holdsGravity = false;
        }
    }
}
