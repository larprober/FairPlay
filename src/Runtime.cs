using UnityEngine;
using UnityEngine.SceneManagement;
using FairPlay.Core;
using FairPlay.Menu;
using FairPlay.Utils;

namespace FairPlay
{
    /// <summary>
    /// The always-on heartbeat: polls the gate, ticks whatever is enabled, and owns the panel.
    ///
    /// It runs whether or not the gate is open, because a locked menu that explains itself is more
    /// useful than a menu that vanishes. <see cref="ModuleRegistry"/> is what refuses to tick
    /// modules while the gate is shut, so there is exactly one place that decision is made.
    /// </summary>
    internal sealed class Runtime : MonoBehaviour
    {
        private MenuController _menu;

        private void Awake()
        {
            _menu = gameObject.AddComponent<MenuController>();

            LobbyGuard.Changed += OnGateChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;

            LobbyGuard.Evaluate();
        }

        private void OnDestroy()
        {
            LobbyGuard.Changed -= OnGateChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;

            ModuleRegistry.RevertAll();
            if (_menu != null) _menu.Teardown();
        }

        private void Update()
        {
            XRInput.Sample();
            ModuleRegistry.Tick();

            if (Input.GetKeyDown(Settings.PanicKey.Value))
                ModuleRegistry.DisableAll("panic key");
        }

        private void FixedUpdate() => ModuleRegistry.FixedTick();

        private void OnGateChanged(bool allowed)
        {
            if (!allowed) ModuleRegistry.DisableAll("gate closed");
        }

        /// <summary>
        /// A scene load invalidates every cached rig reference and orphans anything we spawned,
        /// so the whole runtime state is dropped and rebuilt lazily on the next frame.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ModuleRegistry.RevertAll();
            GravityLock.ForceRelease();
            Modules.WaypointModule.Forget();
            GameRefs.Invalidate();
            XRInput.Invalidate();

            if (_menu != null) _menu.Teardown();
        }
    }
}
