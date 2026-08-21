using UnityEngine;
using FairPlay.Core;
using FairPlay.Utils;

namespace FairPlay.Modules
{
    /// <summary>
    /// Extends reach by pushing the hand transforms further from the body.
    ///
    /// It runs in <see cref="LateTick"/> for a specific reason: Gorilla Tag writes hand positions
    /// from controller tracking during its own update, so the same work done in Tick is simply
    /// overwritten in the same frame. Nothing is stored either - stop running and the next tracked
    /// frame restores your real arms, which is why Revert has nothing to undo.
    ///
    /// This is the self-only module that comes closest to affecting other people. Reach is tag
    /// range, and tag range is the game, so the default multiplier is deliberately modest.
    /// </summary>
    public sealed class LongArmsModule : Module
    {
        public override string Name => "Long Arms";
        public override string Description => "Extends your reach";
        public override ModuleCategory Category => ModuleCategory.Body;

        public override string StatusText =>
            Enabled ? Settings.ArmLength.Value.ToString("0.##") + "x" : null;

        public override void LateTick()
        {
            Transform head = GameRefs.Head;
            if (head == null) return;

            float factor = Settings.ArmLength.Value;
            if (factor <= 1.001f) return;

            Stretch(GameRefs.LeftHand, head, factor);
            Stretch(GameRefs.RightHand, head, factor);
        }

        private static void Stretch(Transform hand, Transform head, float factor)
        {
            if (hand == null) return;

            // Measured from the head rather than a shoulder bone. The head is the one anchor that
            // resolves on every version of the game, and the difference from a true shoulder is a
            // fixed offset rather than anything you can feel while playing.
            Vector3 offset = hand.position - head.position;
            hand.position = head.position + offset * factor;
        }

        /// <summary>Tracking rewrites hand positions every frame, so there is nothing to restore.</summary>
        public override void Revert() { }
    }
}
