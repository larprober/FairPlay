using FairPlay.Core;
using FairPlay.Utils;

namespace FairPlay.Modules
{
    /// <summary>
    /// Turns up the two jump values the locomotion component already exposes.
    ///
    /// The originals are captured on enable and written back on revert. If the fields have been
    /// renamed by a game update, <see cref="GameRefs"/> returns null, nothing is cached and
    /// nothing is written - the feature quietly does nothing rather than half-applying.
    /// </summary>
    public sealed class BounceModule : Module
    {
        public override string Name => "Bounce";
        public override string Description => "Higher jumps";
        public override ModuleCategory Category => ModuleCategory.Movement;

        private float? _maxJumpSpeedWas;
        private float? _jumpMultiplierWas;

        protected override void OnEnabled()
        {
            _maxJumpSpeedWas   = GameRefs.MaxJumpSpeed;
            _jumpMultiplierWas = GameRefs.JumpMultiplier;

            float factor = Settings.JumpMultiplier.Value;

            if (_maxJumpSpeedWas.HasValue)   GameRefs.MaxJumpSpeed   = _maxJumpSpeedWas.Value * factor;
            if (_jumpMultiplierWas.HasValue) GameRefs.JumpMultiplier = _jumpMultiplierWas.Value * factor;

            if (!_maxJumpSpeedWas.HasValue && !_jumpMultiplierWas.HasValue)
                Plugin.Log.LogWarning("Bounce: jump fields not found on this game version.");
        }

        public override void Revert()
        {
            if (_maxJumpSpeedWas.HasValue)   GameRefs.MaxJumpSpeed   = _maxJumpSpeedWas.Value;
            if (_jumpMultiplierWas.HasValue) GameRefs.JumpMultiplier = _jumpMultiplierWas.Value;

            _maxJumpSpeedWas = null;
            _jumpMultiplierWas = null;
        }
    }
}
