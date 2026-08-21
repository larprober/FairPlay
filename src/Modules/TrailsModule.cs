using UnityEngine;
using FairPlay.Core;
using FairPlay.Menu;
using FairPlay.Utils;

namespace FairPlay.Modules
{
    /// <summary>
    /// Ribbons behind your hands. Purely cosmetic, purely local, and genuinely useful for reading
    /// your own swing arc back when you are practising movement.
    /// </summary>
    public sealed class TrailsModule : Module
    {
        public override string Name => "Trails";
        public override string Description => "Ribbons on your hands";
        public override ModuleCategory Category => ModuleCategory.Visual;

        private const float Lifetime = 0.45f;

        private TrailRenderer _left;
        private TrailRenderer _right;

        public override void Tick()
        {
            _left  = Ensure(_left, true);
            _right = Ensure(_right, false);
        }

        private TrailRenderer Ensure(TrailRenderer trail, bool leftHand)
        {
            Transform hand = GameRefs.Hand(leftHand);
            if (hand == null) return trail;

            if (trail != null)
            {
                // Re-parent if the rig was rebuilt underneath us, e.g. after a scene change.
                if (trail.transform.parent != hand)
                {
                    trail.transform.SetParent(hand, false);
                    trail.transform.localPosition = Vector3.zero;
                }

                return trail;
            }

            var go = new GameObject(leftHand ? "FairPlayTrailL" : "FairPlayTrailR");
            go.transform.SetParent(hand, false);
            go.transform.localPosition = Vector3.zero;

            var created = go.AddComponent<TrailRenderer>();
            created.time = Lifetime;
            created.widthMultiplier = 0.035f;
            created.numCapVertices = 4;
            created.material = Draw.Flat(MenuTheme.Accent);
            created.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(MenuTheme.Accent, 0f), new GradientColorKey(MenuTheme.Accent, 1f) },
                new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });
            created.colorGradient = gradient;

            return created;
        }

        public override void Revert()
        {
            if (_left != null)  Object.Destroy(_left.gameObject);
            if (_right != null) Object.Destroy(_right.gameObject);

            _left = null;
            _right = null;
        }
    }
}
