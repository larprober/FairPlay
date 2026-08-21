using UnityEngine;
using FairPlay.Core;
using FairPlay.Utils;

namespace FairPlay.Modules
{
    /// <summary>
    /// T-pose to fly, the way Bark popularised it.
    ///
    /// The pose test is deliberately loose: both hands well away from the head, and both roughly
    /// level with it. Tighter tests feel broken in VR because nobody holds a perfect T-pose: arms sag
    /// and controllers drift. Steering is your gaze, so you bank by looking.
    /// </summary>
    public sealed class AirplaneModule : Module
    {
        public override string Name => "Airplane";
        public override string Description => "T-pose to fly where you look";
        public override ModuleCategory Category => ModuleCategory.Movement;

        /// <summary>How far a hand must be from the head to count as extended.</summary>
        private const float ArmSpan = 0.35f;

        /// <summary>How far off head height a hand may drift and still count as level.</summary>
        private const float LevelTolerance = 0.30f;

        private bool _holdsGravity;
        private bool _flying;

        public override void FixedTick()
        {
            var body = GameRefs.Body;
            Transform head = GameRefs.Head;
            Transform left = GameRefs.LeftHand;
            Transform right = GameRefs.RightHand;

            if (body == null || head == null || left == null || right == null) return;

            _flying = IsPosed(head, left) && IsPosed(head, right);

            if (_flying)
            {
                if (!_holdsGravity)
                {
                    GravityLock.Claim();
                    _holdsGravity = true;
                }

                body.velocity = head.forward * Settings.FlySpeed.Value;
            }
            else if (_holdsGravity)
            {
                // Drop out of the pose and you drop out of the sky, which is the fun of it.
                GravityLock.Release();
                _holdsGravity = false;
            }
        }

        private static bool IsPosed(Transform head, Transform hand)
        {
            Vector3 offset = hand.position - head.position;
            Vector3 flat = new Vector3(offset.x, 0f, offset.z);

            return flat.magnitude > ArmSpan && Mathf.Abs(offset.y) < LevelTolerance;
        }

        public override void Revert()
        {
            _flying = false;
            if (!_holdsGravity) return;

            GravityLock.Release();
            _holdsGravity = false;
        }
    }
}
