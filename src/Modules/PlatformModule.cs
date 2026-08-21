using System.Collections.Generic;
using UnityEngine;
using FairPlay.Core;
using FairPlay.Menu;
using FairPlay.Utils;

namespace FairPlay.Modules
{
    /// <summary>
    /// Spawns a solid pad under whichever hand pulls its trigger.
    ///
    /// The pads are plain local GameObjects. Nothing is instantiated over the network, so nobody
    /// else sees them or can stand on them - which is exactly the point: this is a movement aid for
    /// you, not terrain you impose on the lobby.
    ///
    /// Two limits keep the scene clean: a budget that recycles the oldest pad, and an optional
    /// lifetime. Both are in the config file.
    /// </summary>
    public sealed class PlatformModule : Module
    {
        public override string Name => "Platforms";
        public override string Description => "Trigger spawns a pad at your hand";
        public override ModuleCategory Category => ModuleCategory.Builder;

        private const float Size = 0.55f;
        private const float Thickness = 0.05f;
        private const float Cooldown = 0.22f;

        private readonly Queue<GameObject> _pads = new Queue<GameObject>();
        private readonly Dictionary<GameObject, float> _born = new Dictionary<GameObject, float>();

        /// <summary>One material for every pad; a fresh one per spawn is a leak by the dozen.</summary>
        private Material _padMaterial;

        private float _nextSpawnLeft;
        private float _nextSpawnRight;

        public override void Tick()
        {
            XRInput.Sample();

            TrySpawn(true, ref _nextSpawnLeft);
            TrySpawn(false, ref _nextSpawnRight);

            Expire();
        }

        private void TrySpawn(bool leftHand, ref float nextAllowed)
        {
            if (!XRInput.For(leftHand).TriggerHeld) return;
            if (Time.unscaledTime < nextAllowed) return;

            Transform hand = GameRefs.Hand(leftHand);
            if (hand == null) return;

            nextAllowed = Time.unscaledTime + Cooldown;
            Spawn(hand.position + Vector3.down * 0.12f);

            if (Settings.Haptics.Value) GameRefs.Vibrate(leftHand, 0.35f, 0.04f);
        }

        private void Spawn(Vector3 position)
        {
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "FairPlayPad";
            pad.transform.position = position;
            pad.transform.localScale = new Vector3(Size, Thickness, Size);

            if (_padMaterial == null) _padMaterial = Draw.Flat(MenuTheme.Accent);

            var renderer = pad.GetComponent<Renderer>();
            renderer.sharedMaterial = _padMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            _pads.Enqueue(pad);
            _born[pad] = Time.unscaledTime;

            while (_pads.Count > Settings.PlatformBudget.Value) DestroyOldest();
        }

        private void Expire()
        {
            float life = Settings.PlatformLife.Value;
            if (life <= 0f) return;

            while (_pads.Count > 0)
            {
                GameObject oldest = _pads.Peek();

                if (oldest != null && _born.TryGetValue(oldest, out float born) &&
                    Time.unscaledTime - born < life)
                    break;

                DestroyOldest();
            }
        }

        private void DestroyOldest()
        {
            if (_pads.Count == 0) return;

            GameObject pad = _pads.Dequeue();
            _born.Remove(pad);
            if (pad != null) Object.Destroy(pad);
        }

        public override void Revert()
        {
            while (_pads.Count > 0) DestroyOldest();
            _born.Clear();
        }
    }
}
