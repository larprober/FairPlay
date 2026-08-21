using UnityEngine;
using FairPlay.Core;
using FairPlay.Menu;
using FairPlay.Utils;

namespace FairPlay.Modules
{
    /// <summary>
    /// Point and pull.
    ///
    /// Aim is the vector from your head to your hand, which is how people naturally point and,
    /// conveniently, avoids depending on any particular hand axis. Hold a trigger to cast; while
    /// the rope is attached your velocity is steered toward the anchor and the line is drawn
    /// locally, for you only.
    /// </summary>
    public sealed class GrappleModule : Module
    {
        public override string Name => "Grapple";
        public override string Description => "Trigger to pull toward where you point";
        public override ModuleCategory Category => ModuleCategory.Builder;

        private const float MaxRange = 30f;
        private const float ReleaseDistance = 1.1f;

        private LineRenderer _rope;
        private bool _attached;
        private Vector3 _anchor;
        private bool _anchorHand;

        public override void Tick()
        {
            XRInput.Sample();

            bool leftHeld  = XRInput.Left.TriggerHeld;
            bool rightHeld = XRInput.Right.TriggerHeld;

            if (!_attached)
            {
                if (rightHeld) TryAttach(false);
                else if (leftHeld) TryAttach(true);
            }
            else if (!XRInput.For(_anchorHand).TriggerHeld)
            {
                Detach();
            }

            DrawRope();
        }

        public override void FixedTick()
        {
            if (!_attached) return;

            var body = GameRefs.Body;
            if (body == null) return;

            Vector3 toAnchor = _anchor - body.position;

            if (toAnchor.magnitude <= ReleaseDistance)
            {
                Detach();
                return;
            }

            body.velocity = toAnchor.normalized * Settings.GrappleForce.Value;
        }

        private void TryAttach(bool leftHand)
        {
            Transform hand = GameRefs.Hand(leftHand);
            Transform head = GameRefs.Head;
            if (hand == null || head == null) return;

            Vector3 aim = hand.position - head.position;
            if (aim.sqrMagnitude < 0.0004f) return;

            // Gorilla Tag is full of trigger volumes; roping onto one would anchor you to thin air.
            if (!Physics.Raycast(hand.position, aim.normalized, out RaycastHit hit, MaxRange,
                                 ~0, QueryTriggerInteraction.Ignore)) return;

            _anchor = hit.point;
            _anchorHand = leftHand;
            _attached = true;

            if (Settings.Haptics.Value) GameRefs.Vibrate(leftHand, 0.7f, 0.06f);
        }

        private void Detach()
        {
            _attached = false;

            var body = GameRefs.Body;
            if (body == null) return;

            // Keep a slice of the momentum so releasing feels like a swing, not a wall.
            body.velocity *= 0.6f;
        }

        private void DrawRope()
        {
            if (!_attached)
            {
                if (_rope != null) _rope.enabled = false;
                return;
            }

            Transform hand = GameRefs.Hand(_anchorHand);
            if (hand == null) return;

            if (_rope == null)
            {
                var go = new GameObject("FairPlayRope");
                Object.DontDestroyOnLoad(go);

                _rope = go.AddComponent<LineRenderer>();
                _rope.material = Draw.Flat(MenuTheme.Accent);
                _rope.widthMultiplier = 0.012f;
                _rope.positionCount = 2;
                _rope.useWorldSpace = true;
                _rope.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            _rope.enabled = true;
            _rope.SetPosition(0, hand.position);
            _rope.SetPosition(1, _anchor);
        }

        public override void Revert()
        {
            _attached = false;

            if (_rope != null)
            {
                Object.Destroy(_rope.gameObject);
                _rope = null;
            }
        }
    }
}
