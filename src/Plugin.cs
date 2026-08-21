using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using Utilla;
using FairPlay.Core;
using FairPlay.Modules;

namespace FairPlay
{
    /// <summary>
    /// BepInEx entry point, and one half of the lobby gate.
    ///
    /// The <c>[ModdedGamemode]</c> attribute hands control of this behaviour to Utilla: it enables
    /// the component on entering a modded room and disables it on leaving. Those two edges are all
    /// this class contributes - they set <see cref="LobbyGuard.UtillaConfirmed"/> and nothing else.
    ///
    /// The menu and the module loop deliberately live on a separate always-running object
    /// (<see cref="Runtime"/>). If they lived here they would stop ticking the moment Utilla
    /// disabled the plugin, and the panel could never tell you *why* it was locked.
    /// </summary>
    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    [BepInDependency(PluginInfo.UtillaGuid, PluginInfo.UtillaVersion)]
    [ModdedGamemode]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log { get; private set; }

        private static GameObject _runtimeObject;

        private void Awake()
        {
            Log = Logger;
            Settings.Bind(Config);

            ModuleRegistry.Register(
                new FlightModule(),
                new SprintModule(),
                new BounceModule(),
                new PhaseModule(),
                new AirplaneModule(),
                new BubbleModule(),
                new SizeModule(),
                new PlatformModule(),
                new GrappleModule(),
                new WaypointModule(),
                new PearlModule(),
                new NailgunModule(),
                new ChromaModule(),
                new TrailsModule(),
                new BeaconModule());

            if (_runtimeObject == null)
            {
                _runtimeObject = new GameObject("FairPlayRuntime");
                DontDestroyOnLoad(_runtimeObject);
                _runtimeObject.AddComponent<Runtime>();
            }

            Log.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loaded with " +
                        $"{ModuleRegistry.All.Count} module(s). Gate closed until a MODDED lobby.");
        }

        /// <summary>Utilla edge: a modded gamemode was entered.</summary>
        private void OnEnable()
        {
            LobbyGuard.UtillaConfirmed = true;
            LobbyGuard.Evaluate();
        }

        /// <summary>Utilla edge: a modded gamemode was left. Everything goes off immediately.</summary>
        private void OnDisable()
        {
            LobbyGuard.UtillaConfirmed = false;
            LobbyGuard.Evaluate();
            ModuleRegistry.DisableAll("left the modded gamemode");
        }

        private void OnDestroy()
        {
            ModuleRegistry.RevertAll();
            LobbyGuard.Reset();
        }
    }
}
