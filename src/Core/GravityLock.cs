using FairPlay.Utils;

namespace FairPlay.Core
{
    /// <summary>
    /// Shared owner of the local rigidbody's gravity flag.
    ///
    /// Flight, Airplane and Bubble all want gravity off, and each used to cache "what it was
    /// before" independently. That breaks the moment two of them overlap: the second one to start
    /// caches the value the first one already forced to false, and when the first is switched off
    /// the second restores false - leaving you floating with no module admitting responsibility.
    ///
    /// A claim count fixes it. The first claim records the real value; the last release puts it
    /// back. Claims are idempotent per module, so a module that claims twice still releases once.
    /// </summary>
    internal static class GravityLock
    {
        private static bool _original = true;
        private static int _claims;

        public static void Claim()
        {
            var body = GameRefs.Body;

            if (_claims == 0 && body != null) _original = body.useGravity;

            _claims++;
            if (body != null) body.useGravity = false;
        }

        public static void Release()
        {
            if (_claims == 0) return;

            _claims--;
            if (_claims > 0) return;

            var body = GameRefs.Body;
            if (body != null) body.useGravity = _original;
        }

        /// <summary>Drops every claim and restores gravity. Used on teardown and scene changes.</summary>
        public static void ForceRelease()
        {
            _claims = 0;

            var body = GameRefs.Body;
            if (body != null) body.useGravity = _original;
        }
    }
}
