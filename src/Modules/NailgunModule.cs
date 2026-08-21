using System.Collections.Generic;
using UnityEngine;
using FairPlay.Core;
using FairPlay.Menu;
using FairPlay.Utils;

namespace FairPlay.Modules
{
    /// <summary>
    /// Platforms at range: point somewhere and nail a small pad to it.
    ///
    /// Same budget-and-lifetime discipline as <see cref="PlatformModule"/>, and the same local-only
    /// story - nobody else sees these or can stand on them.
    /// </summary>
    public sealed class NailgunModule : Module
    {
        public override string Name => "Nailgun";
        public override string Description => "Trigger nails a pad where you point";
        public override ModuleCategory Category => ModuleCategory.Builder;

        private const float Range = 25f;
        private const float Size = 0.4f;
        private const float Thickness = 0.05f;

        private readonly Queue<GameObject> _nails = new Queue<GameObject>();
        private Material _material;

        public override void Tick()
        {
            XRInput.Sample();

            if (XRInput.TriggerDown(false)) Fire(false);
            if (XRInput.TriggerDown(true)) Fire(true);
        }

        private void Fire(bool leftHand)
        {
            Transform hand = GameRefs.Hand(leftHand);
            Transform head = GameRefs.Head;
            if (hand == null || head == null) return;

            Vector3 aim = hand.position - head.position;
            if (aim.sqrMagnitude < 0.0004f) return;

            if (!Physics.Raycast(hand.position, aim.normalized, out RaycastHit hit, Range,
                                 ~0, QueryTriggerInteraction.Ignore))
                return;

            Place(hit.point);

            if (Settings.Haptics.Value) GameRefs.Vibrate(leftHand, 0.45f, 0.05f);
        }

        private void Place(Vector3 point)
        {
            if (_material == null) _material = Draw.Flat(MenuTheme.Accent);

            var nail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nail.name = "FairPlayNail";
            nail.transform.position = point;
            nail.transform.localScale = new Vector3(Size, Thickness, Size);

            var renderer = nail.GetComponent<Renderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            _nails.Enqueue(nail);

            while (_nails.Count > Settings.PlatformBudget.Value) DestroyOldest();
        }

        private void DestroyOldest()
        {
            if (_nails.Count == 0) return;

            GameObject nail = _nails.Dequeue();
            if (nail != null) Object.Destroy(nail);
        }

        public override void Revert()
        {
            while (_nails.Count > 0) DestroyOldest();
        }
    }
}
