using BepInEx.Configuration;
using UnityEngine;

namespace FairPlay.Core
{
    public enum Handedness { Left, Right }

    /// <summary>
    /// Everything tunable, surfaced through BepInEx's config file so it can be edited without a
    /// rebuild: <c>BepInEx/config/com.larprober.gorillatag.fairplay.cfg</c>.
    /// </summary>
    public static class Settings
    {
        public static ConfigEntry<bool>       AllowOutsideRoom { get; private set; }
        public static ConfigEntry<Handedness> MenuHand         { get; private set; }
        public static ConfigEntry<float>      MenuScale        { get; private set; }
        public static ConfigEntry<Vector3>    MenuOffset       { get; private set; }
        public static ConfigEntry<bool>       Haptics          { get; private set; }
        public static ConfigEntry<KeyCode>    PanicKey         { get; private set; }

        public static ConfigEntry<float> FlySpeed        { get; private set; }
        public static ConfigEntry<float> SpeedMultiplier { get; private set; }
        public static ConfigEntry<float> JumpMultiplier  { get; private set; }
        public static ConfigEntry<float> ArmLength       { get; private set; }
        public static ConfigEntry<float> GrappleForce    { get; private set; }
        public static ConfigEntry<int>   PlatformBudget  { get; private set; }
        public static ConfigEntry<float> PlatformLife    { get; private set; }

        /// <summary>
        /// Null-safe read of <see cref="AllowOutsideRoom"/>.
        ///
        /// <see cref="LobbyGuard"/> reads this every frame from the very first one. If binding ever
        /// failed - a locked or malformed config file - a raw .Value would throw out of the update
        /// loop once per frame forever. Unbound means off, which is the safe direction.
        /// </summary>
        public static bool OfflineSandbox => AllowOutsideRoom != null && AllowOutsideRoom.Value;

        public static void Bind(ConfigFile config)
        {
            AllowOutsideRoom = config.Bind("General", "AllowOutsideRoom", true,
                "Allow features while not connected to any lobby, so you can test on your own. " +
                "This never affects public lobbies - the gate stays shut in any non-MODDED room " +
                "regardless of this setting.");

            MenuHand = config.Bind("Menu", "MenuHand", Handedness.Left,
                "Which hand holds the menu. You press it with the other one.");

            MenuScale = config.Bind("Menu", "Scale", 1.0f,
                new ConfigDescription("Overall menu size.", new AcceptableValueRange<float>(0.6f, 1.6f)));

            MenuOffset = config.Bind("Menu", "Offset", new Vector3(0f, 0.03f, 0.02f),
                "Where the slab sits relative to the holding hand, in metres. Y is the gap between " +
                "your hand and the bottom edge, Z pushes it away from you, X slides it sideways " +
                "and is mirrored automatically when you switch hands. Nudge this first if the " +
                "panel clips into your controller model.");

            Haptics = config.Bind("Menu", "Haptics", true, "Buzz the controller on a button press.");

            PanicKey = config.Bind("Menu", "PanicKey", KeyCode.F8,
                "Keyboard panic key: turns every module off and reverts. Handy while debugging on a flat screen.");

            FlySpeed = config.Bind("Movement", "FlySpeed", 9f,
                new ConfigDescription("Flight thrust in metres/second.", new AcceptableValueRange<float>(1f, 30f)));

            SpeedMultiplier = config.Bind("Movement", "SpeedMultiplier", 1.6f,
                new ConfigDescription("Horizontal velocity multiplier for Sprint.", new AcceptableValueRange<float>(1f, 4f)));

            JumpMultiplier = config.Bind("Movement", "JumpMultiplier", 1.7f,
                new ConfigDescription("Jump strength multiplier for Bounce.", new AcceptableValueRange<float>(1f, 4f)));

            ArmLength = config.Bind("Body", "ArmLength", 1.5f,
                new ConfigDescription(
                    "Reach multiplier for Long Arms. Modest by default on purpose - reach is tag " +
                    "range, and tag range is the whole game.",
                    new AcceptableValueRange<float>(1f, 4f)));

            GrappleForce = config.Bind("Builder", "GrappleForce", 12f,
                new ConfigDescription("Pull speed of the grapple.", new AcceptableValueRange<float>(2f, 30f)));

            PlatformBudget = config.Bind("Builder", "PlatformBudget", 12,
                new ConfigDescription("How many platforms exist at once before the oldest is recycled.",
                    new AcceptableValueRange<int>(1, 40)));

            PlatformLife = config.Bind("Builder", "PlatformLifetime", 14f,
                new ConfigDescription("Seconds a platform survives. 0 = until recycled by the budget.",
                    new AcceptableValueRange<float>(0f, 120f)));
        }
    }
}
