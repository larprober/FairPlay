using UnityEngine;
using FairPlay.Core;
using FairPlay.Menu;
using FairPlay.Utils;

namespace FairPlay.Modules
{
    /// <summary>
    /// Throw a pearl, teleport to where it lands.
    ///
    /// The pearl is integrated by hand rather than handed to a Rigidbody: a few lines of ballistics
    /// plus a raycast along the step it just travelled is more predictable than a physics body, and
    /// it cannot tunnel through thin geometry at speed the way a fast Rigidbody does.
    /// </summary>
    public sealed class PearlModule : Module
    {
        public override string Name => "Pearl";
        public override string Description => "Trigger throws, you land where it does";
        public override ModuleCategory Category => ModuleCategory.Builder;

        private const float ThrowSpeed = 14f;
        private const float Gravity = -9.81f;
        private const float MaxLife = 6f;
        private const float LandOffset = 0.6f;

        /// <summary>How far in front of the hand the pearl appears, to clear your own collider.</summary>
        private const float MuzzleOffset = 0.35f;

        private GameObject _pearl;
        private Vector3 _velocity;
        private float _spawned;

        public override void Tick()
        {
            XRInput.Sample();

            if (_pearl == null)
            {
                if (InputRouter.TryTriggerDown(false)) Throw(false);
                else if (InputRouter.TryTriggerDown(true)) Throw(true);
                return;
            }

            Fly();
        }

        private void Throw(bool leftHand)
        {
            Transform hand = GameRefs.Hand(leftHand);
            Transform head = GameRefs.Head;
            if (hand == null || head == null) return;

            Vector3 aim = hand.position - head.position;
            if (aim.sqrMagnitude < 0.0004f) return;

            _pearl = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _pearl.name = "FairPlayPearl";
            Object.Destroy(_pearl.GetComponent<Collider>());
            Object.DontDestroyOnLoad(_pearl);

            // Spawned clear of the body. Starting exactly at the hand lets the very first sweep
            // hit your own collider, which teleports you to where you already stand.
            _pearl.transform.position = hand.position + aim.normalized * MuzzleOffset;
            _pearl.transform.localScale = Vector3.one * 0.12f;

            var renderer = _pearl.GetComponent<Renderer>();
            renderer.material = Draw.Flat(MenuTheme.Accent);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            _velocity = aim.normalized * ThrowSpeed;
            _spawned = Time.unscaledTime;

            if (Settings.Haptics.Value) GameRefs.Vibrate(leftHand, 0.5f, 0.05f);
        }

        private void Fly()
        {
            float step = Time.deltaTime;
            if (step <= 0f) return;

            Vector3 from = _pearl.transform.position;
            _velocity += new Vector3(0f, Gravity * step, 0f);
            Vector3 to = from + _velocity * step;

            Vector3 travel = to - from;
            float distance = travel.magnitude;

            if (distance > 0.0001f &&
                Physics.Raycast(from, travel.normalized, out RaycastHit hit, distance,
                                ~0, QueryTriggerInteraction.Ignore))
            {
                Land(hit.point);
                return;
            }

            _pearl.transform.position = to;

            // A pearl thrown off the map never lands, so it gets a lifetime.
            if (Time.unscaledTime - _spawned > MaxLife) Clear();
        }

        private void Land(Vector3 point)
        {
            GameRefs.TeleportTo(point + Vector3.up * LandOffset);

            if (Settings.Haptics.Value)
            {
                GameRefs.Vibrate(true, 0.7f, 0.07f);
                GameRefs.Vibrate(false, 0.7f, 0.07f);
            }

            Clear();
        }

        private void Clear()
        {
            if (_pearl != null) Object.Destroy(_pearl);
            _pearl = null;
            _velocity = Vector3.zero;
        }

        public override void Revert() => Clear();
    }
}
