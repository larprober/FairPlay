using UnityEngine;
using FairPlay.Menu;
using FairPlay.Core;
using FairPlay.Utils;

namespace FairPlay.Modules
{
    /// <summary>
    /// Draws a column of light at your saved waypoint so you can see it across the map.
    ///
    /// It marks a spot you chose, not a player. That distinction is the whole reason this module
    /// exists in the Look category while player highlighting does not exist at all - see
    /// docs/FAIR-PLAY.md.
    /// </summary>
    public sealed class BeaconModule : Module
    {
        public override string Name => "Beacon";
        public override string Description => "Light column on your waypoint";
        public override ModuleCategory Category => ModuleCategory.Visual;

        private const float Height = 40f;
        private const float Width = 0.18f;

        private GameObject _column;
        private Renderer _columnRenderer;

        public override void Tick()
        {
            if (!WaypointModule.Saved.HasValue)
            {
                if (_column != null) _column.SetActive(false);
                return;
            }

            if (_column == null) Build();

            _column.SetActive(true);
            _column.transform.position = WaypointModule.Saved.Value + Vector3.up * (Height * 0.5f);

            // Slow pulse, so it reads as a marker rather than a lump of scenery.
            float pulse = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 2f);
            Draw.SetColor(_columnRenderer, MenuTheme.Accent * pulse);
        }

        private void Build()
        {
            _column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _column.name = "FairPlayBeacon";
            Object.Destroy(_column.GetComponent<Collider>());
            Object.DontDestroyOnLoad(_column);

            _column.transform.localScale = new Vector3(Width, Height * 0.5f, Width);

            _columnRenderer = _column.GetComponent<Renderer>();
            _columnRenderer.material = Draw.Flat(MenuTheme.Accent);
            _columnRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public override void Revert()
        {
            if (_column == null) return;

            Object.Destroy(_column);
            _column = null;
            _columnRenderer = null;
        }
    }
}
