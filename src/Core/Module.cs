namespace FairPlay.Core
{
    public enum ModuleCategory
    {
        Movement,
        /// <summary>Changes the shape of you, rather than where you go.</summary>
        Body,
        Builder,
        Visual
    }

    /// <summary>
    /// Base class for every feature in the menu.
    ///
    /// The contract every module signs:
    ///   * it may only ever change the *local* player's state,
    ///   * <see cref="Revert"/> must fully undo everything the module touched, and must be
    ///     safe to call at any time, including when the module was never enabled, and
    ///   * it must never send an RPC or write a Photon room/player property.
    ///
    /// <see cref="ModuleRegistry"/> enforces the first point at load time by refusing to
    /// register anything that declares <see cref="AffectsOtherPlayers"/>.
    /// </summary>
    public abstract class Module
    {
        public abstract string Name { get; }

        /// <summary>One short line, shown on the menu's info plate while the button is hovered.</summary>
        public virtual string Description => string.Empty;

        public virtual ModuleCategory Category => ModuleCategory.Movement;

        /// <summary>
        /// Declared, not detected. A module that reaches outside the local player sets this to
        /// true and is then rejected by the registry — the flag exists so that "does this touch
        /// anyone else?" is an explicit, reviewable answer in every file rather than an omission.
        /// </summary>
        public virtual bool AffectsOtherPlayers => false;

        public bool Enabled { get; private set; }

        /// <summary>
        /// Turns the module on or off. Enabling is refused unless <see cref="LobbyGuard"/> is green;
        /// disabling always succeeds and always reverts.
        /// </summary>
        public bool SetEnabled(bool value)
        {
            if (value == Enabled) return Enabled;

            if (value && !LobbyGuard.Allowed)
            {
                Plugin.Log.LogWarning($"Refused to enable '{Name}': {LobbyGuard.Reason}");
                return false;
            }

            Enabled = value;

            try
            {
                if (value) OnEnabled();
                else       { OnDisabled(); Revert(); }
            }
            catch (System.Exception e)
            {
                // A module that throws must never take the menu down with it.
                Plugin.Log.LogError($"'{Name}' threw while toggling to {value}: {e}");
                Enabled = false;
                SafeRevert();
            }

            return Enabled;
        }

        /// <summary>Force-off used by the guard and the panic button. Never throws.</summary>
        internal void ForceOff()
        {
            if (Enabled)
            {
                Enabled = false;
                try { OnDisabled(); } catch (System.Exception e) { Plugin.Log.LogError($"'{Name}'.OnDisabled: {e}"); }
            }
            SafeRevert();
        }

        internal void SafeRevert()
        {
            try { Revert(); }
            catch (System.Exception e) { Plugin.Log.LogError($"'{Name}'.Revert: {e}"); }
        }

        /// <summary>
        /// What pressing this module's row does. Toggling is the default; modules with more than
        /// two useful states (Size, for one) override this to cycle instead.
        /// </summary>
        public virtual void Press() => SetEnabled(!Enabled);

        /// <summary>
        /// Optional right-aligned text for the row. Null falls back to ON/OFF, so only modules
        /// with something better to say need to implement it.
        /// </summary>
        public virtual string StatusText => null;

        protected virtual void OnEnabled() { }
        protected virtual void OnDisabled() { }

        /// <summary>Per-frame work. Only called while enabled and while the guard is green.</summary>
        public virtual void Tick() { }

        /// <summary>Physics-step work. Only called while enabled and while the guard is green.</summary>
        public virtual void FixedTick() { }

        /// <summary>
        /// End-of-frame work, after Gorilla Tag has written the rig from controller tracking.
        /// Anything repositioning hands or bones belongs here - do it in Tick and the game
        /// overwrites you in the same frame.
        /// </summary>
        public virtual void LateTick() { }

        /// <summary>
        /// Restore every piece of game state this module changed. Called on disable, on leaving a
        /// modded lobby, on scene changes and on plugin unload. Must be idempotent.
        /// </summary>
        public abstract void Revert();
    }
}
