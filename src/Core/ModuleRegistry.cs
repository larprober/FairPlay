using System.Collections.Generic;
using System.Linq;

namespace FairPlay.Core
{
    /// <summary>
    /// Owns every module instance, drives their ticks, and is the thing the guard shuts off.
    ///
    /// Two invariants live here:
    ///   * a module that declares <see cref="Module.AffectsOtherPlayers"/> is never registered, and
    ///   * the instant <see cref="LobbyGuard.Allowed"/> goes false, every module is forced off and
    ///     reverted — before any module gets another tick.
    /// </summary>
    public static class ModuleRegistry
    {
        private static readonly List<Module> _modules = new List<Module>();
        private static readonly List<Module> _tickBuffer = new List<Module>();

        public static IReadOnlyList<Module> All => _modules;

        public static IEnumerable<Module> InCategory(ModuleCategory category) =>
            _modules.Where(m => m.Category == category);

        public static int EnabledCount => _modules.Count(m => m.Enabled);

        public static void Register(params Module[] modules)
        {
            foreach (var module in modules)
            {
                if (module == null) continue;

                if (module.AffectsOtherPlayers)
                {
                    // Not a warning - a hard rejection. This is the line the project does not cross.
                    Plugin.Log.LogError(
                        $"Rejected module '{module.Name}': it declares that it affects other players. " +
                        "FairPlay only ships self-only features.");
                    continue;
                }

                if (_modules.Any(m => m.Name == module.Name))
                {
                    Plugin.Log.LogWarning($"Duplicate module name '{module.Name}' ignored.");
                    continue;
                }

                _modules.Add(module);
            }
        }

        public static void Tick()
        {
            LobbyGuard.Poll();

            if (!LobbyGuard.Allowed)
            {
                if (EnabledCount > 0) DisableAll("lobby gate closed");
                return;
            }

            _tickBuffer.Clear();
            _tickBuffer.AddRange(_modules);

            foreach (var module in _tickBuffer)
            {
                if (!module.Enabled) continue;
                try { module.Tick(); }
                catch (System.Exception e)
                {
                    Plugin.Log.LogError($"'{module.Name}'.Tick threw, disabling it: {e}");
                    module.ForceOff();
                }
            }
        }

        public static void LateTick()
        {
            if (!LobbyGuard.Allowed) return;

            _tickBuffer.Clear();
            _tickBuffer.AddRange(_modules);

            foreach (var module in _tickBuffer)
            {
                if (!module.Enabled) continue;
                try { module.LateTick(); }
                catch (System.Exception e)
                {
                    Plugin.Log.LogError($"Module {module.Name}.LateTick threw, disabling it: {e}");
                    module.ForceOff();
                }
            }
        }

        public static void FixedTick()
        {
            if (!LobbyGuard.Allowed) return;

            _tickBuffer.Clear();
            _tickBuffer.AddRange(_modules);

            foreach (var module in _tickBuffer)
            {
                if (!module.Enabled) continue;
                try { module.FixedTick(); }
                catch (System.Exception e)
                {
                    Plugin.Log.LogError($"'{module.Name}'.FixedTick threw, disabling it: {e}");
                    module.ForceOff();
                }
            }
        }

        /// <summary>Turns everything off and reverts. Used by the guard, the panic button and unload.</summary>
        public static void DisableAll(string why)
        {
            int count = EnabledCount;
            foreach (var module in _modules) module.ForceOff();
            if (count > 0) Plugin.Log.LogInfo($"Disabled {count} module(s): {why}");
        }

        /// <summary>Reverts every module whether or not it was on. Used on teardown.</summary>
        public static void RevertAll()
        {
            foreach (var module in _modules) module.ForceOff();
        }

        public static void Clear()
        {
            RevertAll();
            _modules.Clear();
        }
    }
}
