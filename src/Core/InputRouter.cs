using UnityEngine;
using FairPlay.Utils;

namespace FairPlay.Core
{
    /// <summary>
    /// Hands out one-shot controller events to a single claimant per frame.
    ///
    /// Without this, every module that wants "trigger pressed" gets it: turn on Pearl and Nailgun
    /// together and one pull of the trigger throws a pearl AND nails a platform, which reads as the
    /// menu being broken. Discrete actions now go to the first enabled module that asks, in
    /// registration order, and the rest see nothing that frame.
    ///
    /// Continuous holds (Platforms, Grapple) deliberately do not route through here - holding a
    /// trigger to place platforms while holding it to grapple is a combination worth allowing.
    /// </summary>
    internal static class InputRouter
    {
        private static int _frame = -1;
        private static bool _leftTaken;
        private static bool _rightTaken;

        /// <summary>True at most once per frame per hand, for the first module that asks.</summary>
        public static bool TryTriggerDown(bool leftHand)
        {
            Sync();

            if (!XRInput.TriggerDown(leftHand)) return false;

            if (leftHand)
            {
                if (_leftTaken) return false;
                _leftTaken = true;
            }
            else
            {
                if (_rightTaken) return false;
                _rightTaken = true;
            }

            return true;
        }

        private static void Sync()
        {
            if (Time.frameCount == _frame) return;

            _frame = Time.frameCount;
            _leftTaken = false;
            _rightTaken = false;
        }
    }
}
