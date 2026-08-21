using UnityEngine;
using FairPlay.Core;
using FairPlay.Utils;

namespace FairPlay.Modules
{
    /// <summary>
    /// Cycles the colour of your own rig, on your screen only.
    ///
    /// Gorilla Tag colour is a networked player property; this module never writes it. It tints the
    /// local skin material and puts the original colour back on revert, so everyone else keeps
    /// seeing the colour you actually chose in the game. It is a toy for your own view, not a way
    /// to change how you appear to the lobby.
    /// </summary>
    public sealed class ChromaModule : Module
    {
        public override string Name => "Chroma";
        public override string Description => "Local-only colour cycle";
        public override ModuleCategory Category => ModuleCategory.Visual;

        private const float CyclesPerSecond = 0.25f;

        private Renderer _skin;
        private Color _original;
        private bool _cached;

        protected override void OnEnabled()
        {
            _skin = GameRefs.LocalSkin;

            if (_skin == null || _skin.material == null)
            {
                Plugin.Log.LogWarning("Chroma: local skin renderer not found on this game version.");
                return;
            }

            _original = Draw.GetColor(_skin, Color.white);
            _cached = true;
        }

        public override void Tick()
        {
            if (!_cached || _skin == null) return;

            float hue = Mathf.Repeat(Time.unscaledTime * CyclesPerSecond, 1f);
            Draw.SetColor(_skin, Color.HSVToRGB(hue, 0.65f, 1f));
        }

        public override void Revert()
        {
            if (!_cached || _skin == null)
            {
                _skin = null;
                _cached = false;
                return;
            }

            Draw.SetColor(_skin, _original);
            _skin = null;
            _cached = false;
        }
    }
}
