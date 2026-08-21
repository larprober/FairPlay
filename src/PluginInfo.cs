namespace FairPlay
{
    /// <summary>Single source of truth for identity strings used by BepInEx, the menu header and logs.</summary>
    internal static class PluginInfo
    {
        public const string Guid    = "com.larprober.gorillatag.fairplay";
        public const string Name    = "FairPlay";
        public const string Version = "1.0.0";

        /// <summary>Shown on the menu header plate.</summary>
        public const string Tagline = "MODDED LOBBIES ONLY";

        /// <summary>BepInEx GUID of Utilla, which owns the MODDED gamemode registration.</summary>
        public const string UtillaGuid    = "org.legoandmars.gorillatag.utilla";
        public const string UtillaVersion = "1.6.14";
    }
}
