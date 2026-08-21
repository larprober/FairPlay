using System;
using UnityEngine;
using FairPlay.Utils;

namespace FairPlay.Core
{
    public enum GuardState
    {
        /// <summary>Not in a Photon room. Nothing is networked, so nobody else can be affected.</summary>
        NoRoom,
        /// <summary>In a room whose gamemode is not MODDED. Everything stays off.</summary>
        VanillaLobby,
        /// <summary>Room looks modded but Utilla has not confirmed it yet. Fails closed.</summary>
        AwaitingConfirmation,
        /// <summary>In a MODDED lobby, confirmed by both sources.</summary>
        Modded
    }

    /// <summary>
    /// The single authority on whether any feature is allowed to run.
    ///
    /// It requires two independent sources to agree before it goes green:
    ///
    ///   1. <b>The room itself.</b> Gorilla Tag stores the active gamemode in the Photon room's
    ///      custom properties under <see cref="GameModeKey"/>. Modded queues put the literal
    ///      token "MODDED" in that string (e.g. "infection_MODDED_", "MODDED_casual"). This is
    ///      read straight off the network state, so it reflects the room every other player is in.
    ///
    ///   2. <b>Utilla.</b> The <c>[ModdedGamemode]</c> attribute on <see cref="Plugin"/> makes
    ///      Utilla enable the behaviour on entering a modded room and disable it on leaving;
    ///      <see cref="Plugin"/> forwards those two edges here.
    ///
    /// Either source failing, being unreadable, or throwing means <see cref="Allowed"/> is false.
    /// There is deliberately no code path that opens the gate on a single signal, and no config
    /// option that opens it in a public lobby.
    /// </summary>
    public static class LobbyGuard
    {
        /// <summary>Room custom-property key Gorilla Tag stores the active gamemode under.</summary>
        public const string GameModeKey = "gameMode";

        /// <summary>The token modded queues carry in their gamemode string.</summary>
        public const string ModdedToken = "MODDED";

        private const float PollInterval = 0.25f;

        private static float _nextPoll;
        private static string _gameMode = string.Empty;

        /// <summary>Set by <see cref="Plugin"/> from the Utilla-driven OnEnable/OnDisable edges.</summary>
        internal static bool UtillaConfirmed { get; set; }

        public static GuardState State { get; private set; } = GuardState.VanillaLobby;

        /// <summary>The one flag every feature checks.</summary>
        public static bool Allowed { get; private set; }

        /// <summary>Human-readable explanation of the current state, shown on the menu status plate.</summary>
        public static string Reason { get; private set; } = "Not evaluated yet";

        /// <summary>Raised on every transition of <see cref="Allowed"/>.</summary>
        public static event Action<bool> Changed;

        /// <summary>Raw gamemode string from the room, for the menu's diagnostics row.</summary>
        public static string GameMode => string.IsNullOrEmpty(_gameMode) ? "-" : _gameMode;

        /// <summary>Cheap; safe to call every frame. Only re-reads network state on an interval.</summary>
        public static void Poll()
        {
            if (Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + PollInterval;
            Evaluate();
        }

        /// <summary>Forces an immediate re-evaluation, e.g. on a room-join edge.</summary>
        public static void Evaluate()
        {
            bool wasAllowed = Allowed;
            GuardState state;
            string reason;

            try
            {
                state = Classify(out reason);
            }
            catch (Exception e)
            {
                // Anything unexpected while reading network state closes the gate.
                state  = GuardState.VanillaLobby;
                reason = "Could not read lobby state";
                Plugin.Log.LogError($"LobbyGuard.Evaluate threw, failing closed: {e}");
            }

            State   = state;
            Reason  = reason;
            Allowed = state == GuardState.Modded ||
                      (state == GuardState.NoRoom && Settings.OfflineSandbox);

            if (Allowed != wasAllowed)
            {
                Plugin.Log.LogInfo($"Gate {(Allowed ? "OPEN" : "CLOSED")} - {Reason}");
                Changed?.Invoke(Allowed);
            }
        }

        private static GuardState Classify(out string reason)
        {
            if (!PhotonRoom.Available)
            {
                // Photon itself could not be found. That is not the same as being offline, and
                // must never be treated as such: it means we cannot see the lobby at all.
                _gameMode = string.Empty;
                reason = "Cannot read network state";
                return GuardState.VanillaLobby;
            }

            if (!PhotonRoom.InRoom)
            {
                _gameMode = string.Empty;
                reason = Settings.OfflineSandbox
                    ? "Not in a lobby - offline sandbox"
                    : "Not in a lobby - offline sandbox disabled";
                return GuardState.NoRoom;
            }

            _gameMode = PhotonRoom.Property(GameModeKey) ?? string.Empty;

            if (!HasModdedToken(_gameMode))
            {
                reason = "Vanilla lobby - features locked";
                return GuardState.VanillaLobby;
            }

            if (!UtillaConfirmed)
            {
                // The room says MODDED but Utilla has not raised its edge. Could be a frame of lag
                // on join, could be a spoofed property. Either way: stay shut.
                reason = "Waiting for Utilla to confirm";
                return GuardState.AwaitingConfirmation;
            }

            reason = "Modded lobby - features unlocked";
            return GuardState.Modded;
        }

        /// <summary>
        /// Looks for MODDED as a whole token, not as a substring.
        ///
        /// A plain case-insensitive Contains would happily match the word UNMODDED - the exact
        /// failure this project exists to prevent, and the kind of thing a future gamemode name
        /// could introduce silently. The match is case-sensitive because Utilla emits the token in
        /// upper case, and it must not be preceded by a letter, so "infection_MODDED_" and
        /// "MODDED_casual" pass while "UNMODDED" does not.
        /// </summary>
        internal static bool HasModdedToken(string gameMode)
        {
            if (string.IsNullOrEmpty(gameMode)) return false;

            int index = gameMode.IndexOf(ModdedToken, StringComparison.Ordinal);

            while (index >= 0)
            {
                if (index == 0 || !char.IsLetter(gameMode[index - 1])) return true;
                index = gameMode.IndexOf(ModdedToken, index + 1, StringComparison.Ordinal);
            }

            return false;
        }

        /// <summary>Called on plugin teardown so stale state cannot leak into the next session.</summary>
        internal static void Reset()
        {
            UtillaConfirmed = false;
            _gameMode = string.Empty;
            _nextPoll = 0f;
            State = GuardState.VanillaLobby;
            Reason = "Not evaluated yet";
            if (Allowed)
            {
                Allowed = false;
                Changed?.Invoke(false);
            }
        }
    }
}
