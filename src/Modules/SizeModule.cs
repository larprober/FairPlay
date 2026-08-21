using UnityEngine;
using FairPlay.Core;
using FairPlay.Utils;

namespace FairPlay.Modules
{
    /// <summary>
    /// Grow and shrink yourself, Bark-potion style.
    ///
    /// This is a cycling row rather than a toggle: pressing it steps through the sizes and wraps
    /// back to normal, with the current multiplier shown on the right of the row. Off means exactly
    /// normal size, so the panic button and the lobby gate both restore you correctly.
    ///
    /// It writes Gorilla Tag's own scale value where one exists, because the game drives locomotion
    /// from it - jump force, reach and step height all follow. Scaling the root transform directly
    /// is only the fallback, and feels worse, because your arms no longer match the world.
    /// </summary>
    public sealed class SizeModule : Module
    {
        public override string Name => "Size";
        public override string Description => "Press to cycle tiny to huge";
        public override ModuleCategory Category => ModuleCategory.Body;

        /// <summary>Index 0 is normal, which is also the "off" state.</summary>
        private static readonly float[] Steps = { 1f, 0.5f, 0.75f, 1.5f, 2f, 3f };

        private int _step;
        private float _original = 1f;
        private bool _cached;

        public override string StatusText => Steps[_step].ToString("0.##") + "x";

        public override void Press()
        {
            _step = (_step + 1) % Steps.Length;

            if (_step == 0)
            {
                SetEnabled(false);
                return;
            }

            if (Enabled)
            {
                Apply();
                return;
            }

            // A refused enable (gate shut) must not leave the row advertising a size that was
            // never applied.
            if (!SetEnabled(true)) _step = 0;
        }

        protected override void OnEnabled()
        {
            if (!_cached)
            {
                _original = GameRefs.PlayerScale;
                _cached = true;
            }

            Apply();
        }

        private void Apply()
        {
            if (!GameRefs.Ready) return;

            GameRefs.PlayerScale = _original * Steps[_step];
        }

        public override void Tick()
        {
            // The game resets scale on respawn and round changes, so it is reasserted rather than
            // set once. Cheap, and it stops the size silently reverting mid-round.
            if (Enabled) Apply();
        }

        public override void Revert()
        {
            _step = 0;

            if (!_cached) return;

            GameRefs.PlayerScale = _original;
            _cached = false;
        }
    }
}
