using UnityEngine;
using FairPlay.Core;
using FairPlay.Utils;

namespace FairPlay.Modules
{
    /// <summary>
    /// Grip-driven flight.
    ///
    /// Controls: right grip thrusts along your gaze, left grip lifts, both do both, and letting go
    /// holds you still instead of dropping you. Direction comes from the head transform rather than
    /// a hand axis because head forward is the one vector that means the same thing on every
    /// version of the game.
    /// </summary>
    public sealed class FlightModule : Module
    {
        public override string Name => "Flight";
        public override string Description => "Grip to fly, both to rise";
        public override ModuleCategory Category => ModuleCategory.Movement;

        private bool _holdsGravity;

        protected override void OnEnabled() => TakeGravity();

        public override void FixedTick()
        {
            var body = GameRefs.Body;
            var head = GameRefs.Head;
            if (body == null || head == null) return;

            // The rig is often not resolved yet on the frame the module is switched on, so gravity
            // is claimed lazily. Without this you would be flying with gravity still fighting you.
            if (!_holdsGravity) TakeGravity();

            XRInput.Sample();

            Vector3 thrust = Vector3.zero;
            if (XRInput.Right.GripHeld) thrust += head.forward;
            if (XRInput.Left.GripHeld)  thrust += Vector3.up;

            if (thrust.sqrMagnitude > 0.001f)
            {
                body.velocity = thrust.normalized * Settings.FlySpeed.Value;
            }
            else
            {
                // Idle hover. Damping rather than a hard zero keeps arm-swing nudges from feeling
                // like the game froze, while still parking you in mid-air.
                body.velocity = Vector3.Lerp(body.velocity, Vector3.zero, 0.25f);
            }
        }

        private void TakeGravity()
        {
            if (GameRefs.Body == null) return;

            GravityLock.Claim();
            _holdsGravity = true;
        }

        public override void Revert()
        {
            if (!_holdsGravity) return;

            GravityLock.Release();
            _holdsGravity = false;
        }
    }
}
