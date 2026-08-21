using System;
using System.Collections;

namespace FairPlay.Utils
{
    /// <summary>
    /// Read-only view of the Photon room, resolved by reflection.
    ///
    /// Nothing here writes. It exists so <see cref="Core.LobbyGuard"/> can ask "what gamemode is
    /// this room running" without the plugin taking a hard compile-time dependency on three Photon
    /// assemblies whose exact type layout varies by version.
    ///
    /// The room property is read through <see cref="IDictionary"/> rather than Photon's own
    /// Hashtable type, so the concrete collection class never has to be named.
    /// </summary>
    internal static class PhotonRoom
    {
        private static Type _photonNetwork;
        private static bool _searched;

        /// <summary>
        /// False when the Photon type could not be resolved at all.
        ///
        /// This matters more than it looks: the guard must treat "cannot read the network" as a
        /// closed gate, NOT as "not in a room". Collapsing the two would mean a renamed Photon
        /// class silently unlocked every feature while sitting in a public lobby.
        /// </summary>
        public static bool Available
        {
            get
            {
                Resolve();
                return _photonNetwork != null;
            }
        }

        public static bool InRoom
        {
            get
            {
                Resolve();
                return Reflect.GetStatic(_photonNetwork, "InRoom") is bool inRoom && inRoom;
            }
        }

        /// <summary>A room custom property as a string, or null when absent or unreadable.</summary>
        public static string Property(string key)
        {
            Resolve();

            object room = Reflect.GetStatic(_photonNetwork, "CurrentRoom");
            if (room == null) return null;

            if (!(Reflect.Get(room, "CustomProperties") is IDictionary properties)) return null;

            return properties[key]?.ToString();
        }

        private static void Resolve()
        {
            if (_searched && _photonNetwork != null) return;

            _searched = true;
            _photonNetwork ??= Reflect.FindType("Photon.Pun.PhotonNetwork");
        }
    }
}
