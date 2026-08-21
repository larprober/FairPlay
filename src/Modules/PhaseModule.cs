using UnityEngine;
using FairPlay.Core;
using FairPlay.Utils;

namespace FairPlay.Modules
{
    /// <summary>
    /// Disables your own body collider so geometry stops stopping you.
    ///
    /// Only the local collider is touched - other players still collide with you exactly as before,
    /// because their client owns that side of the interaction. Pair it with Flight, otherwise you
    /// simply fall through the floor, which is why the description says so on the panel.
    /// </summary>
    public sealed class PhaseModule : Module
    {
        public override string Name => "Phase";
        public override string Description => "No collision (use with Flight)";
        public override ModuleCategory Category => ModuleCategory.Movement;

        private Collider _collider;

        protected override void OnEnabled()
        {
            _collider = GameRefs.BodyCollider;

            if (_collider == null)
            {
                Plugin.Log.LogWarning("Phase: body collider not found on this game version.");
                return;
            }

            _collider.enabled = false;
        }

        public override void Revert()
        {
            if (_collider == null) return;

            // Always hand the collider back on, even if it somehow arrived disabled.
            _collider.enabled = true;
            _collider = null;
        }
    }
}
