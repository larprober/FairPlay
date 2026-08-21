using UnityEngine;
using FairPlay.Core;
using FairPlay.Utils;

namespace FairPlay.Modules
{
    /// <summary>
    /// Amplifies the speed your arm swings generate, without amplifying the speed you already have.
    ///
    /// The naive version - multiply the whole velocity every physics step - looks right for one
    /// frame and is wrong forever after: at 50 steps a second even a 1.05x multiplier compounds you
    /// to terminal velocity within a second of moving, so "Sprint" silently becomes "always maximum
    /// speed". Instead this tracks last step's speed and scales only the NEW speed added since
    /// then, which is the part that actually came from a swing. Coasting and friction are untouched,
    /// so you still slow down normally.
    /// </summary>
    public sealed class SprintModule : Module
    {
        public override string Name => "Sprint";
        public override string Description => "Amplifies each arm swing";
        public override ModuleCategory Category => ModuleCategory.Movement;

        /// <summary>Below this the player is basically standing still; leave them alone.</summary>
        private const float Deadzone = 0.4f;

        /// <summary>Ignore sub-noise changes so resting hands do not accumulate a boost.</summary>
        private const float MinGain = 0.05f;

        /// <summary>Absolute ceiling in m/s, so a bad config value cannot slingshot you.</summary>
        private const float HardCap = 24f;

        private float _lastSpeed;

        protected override void OnEnabled() => _lastSpeed = 0f;

        public override void FixedTick()
        {
            var body = GameRefs.Body;
            if (body == null) return;

            Vector3 velocity = body.velocity;
            Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
            float speed = flat.magnitude;

            float gain = speed - _lastSpeed;
            _lastSpeed = speed;

            if (speed < Deadzone || gain < MinGain) return;

            // Only the newly generated slice is multiplied.
            float boosted = Mathf.Min(speed + gain * (Settings.SpeedMultiplier.Value - 1f), HardCap);
            Vector3 scaled = flat / speed * boosted;

            body.velocity = new Vector3(scaled.x, velocity.y, scaled.z);
            _lastSpeed = boosted;
        }

        /// <summary>No persistent game state is changed; just drop the tracked speed.</summary>
        public override void Revert() => _lastSpeed = 0f;
    }
}
