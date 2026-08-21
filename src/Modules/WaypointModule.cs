using UnityEngine;
using FairPlay.Core;
using FairPlay.Utils;

namespace FairPlay.Modules
{
    /// <summary>
    /// Saves one spot and teleports you back to it.
    ///
    /// Menu-hand face button saves, free-hand face button returns. Only your own transform is
    /// moved, and only to a place you personally stood, so this cannot be used to reach anywhere
    /// you could not already walk to.
    /// </summary>
    public sealed class WaypointModule : Module
    {
        public override string Name => "Waypoint";
        public override string Description => "A/X saves, other A/X returns";
        public override ModuleCategory Category => ModuleCategory.Builder;

        /// <summary>Shared with <see cref="BeaconModule"/> so the marker can be drawn.</summary>
        public static Vector3? Saved { get; private set; }

        /// <summary>
        /// Dropped on a scene change. Coordinates do not survive one, and a stale waypoint means
        /// Beacon plants a light column at a spot that no longer means anything.
        /// </summary>
        internal static void Forget() => Saved = null;

        private static bool MenuHandIsLeft => Settings.MenuHand.Value == Handedness.Left;

        public override void Tick()
        {
            XRInput.Sample();

            if (XRInput.PrimaryDown(MenuHandIsLeft)) Save();
            if (XRInput.PrimaryDown(!MenuHandIsLeft)) Return();
        }

        private void Save()
        {
            Transform root = GameRefs.Root;
            if (root == null) return;

            Saved = root.position;
            if (Settings.Haptics.Value) GameRefs.Vibrate(MenuHandIsLeft, 0.5f, 0.05f);

            Plugin.Log.LogInfo("Waypoint saved at " + Saved.Value.ToString("F1"));
        }

        private void Return()
        {
            if (!Saved.HasValue) return;

            GameRefs.TeleportTo(Saved.Value);
            if (Settings.Haptics.Value) GameRefs.Vibrate(!MenuHandIsLeft, 0.8f, 0.08f);
        }

        /// <summary>The saved point is deliberately kept across a toggle; nothing to undo.</summary>
        public override void Revert() { }
    }
}
