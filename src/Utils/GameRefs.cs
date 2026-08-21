using System;
using UnityEngine;

namespace FairPlay.Utils
{
    /// <summary>
    /// Every touch point between FairPlay and Gorilla Tag's own code, in one file.
    ///
    /// It is all reflection on purpose. Gorilla Tag ships frequent updates that rename things —
    /// <c>GorillaLocomotion.Player</c> became <c>GorillaLocomotion.GTPlayer</c>, for instance — and
    /// resolving lazily by name means a rename degrades into "that one feature is unavailable"
    /// instead of a plugin that fails to load at all. It also keeps the compile-time surface down
    /// to Unity, Photon and BepInEx.
    ///
    /// Nothing in here writes network state. The only members touched are local rig transforms,
    /// the local rigidbody, local colliders and the local haptics call.
    /// </summary>
    public static class GameRefs
    {
        private static Type _playerType;
        private static Type _taggerType;

        private static Component _player;
        private static Component _tagger;
        private static Rigidbody _body;

        private static float _nextResolve;

        /// <summary>True when the local player rig has been found and is still alive.</summary>
        public static bool Ready
        {
            get
            {
                Resolve();
                return _player != null;
            }
        }

        /// <summary>Root transform of the local player. Moving this is how you teleport yourself.</summary>
        public static Transform Root => Ready ? _player.transform : null;

        public static Rigidbody Body
        {
            get
            {
                if (!Ready) return null;
                if (_body == null) _body = _player.GetComponent<Rigidbody>();
                return _body;
            }
        }

        /// <summary>The local head. Used as the forward reference for flight and phase.</summary>
        public static Transform Head
        {
            get
            {
                Resolve();

                var collider = Reflect.Get<Collider>(_tagger, "headCollider");
                if (collider != null) return collider.transform;

                var camera = Camera.main;
                return camera != null ? camera.transform : Root;
            }
        }

        public static Transform LeftHand  => HandTransform("leftHandTransform",  "leftHandFollower");
        public static Transform RightHand => HandTransform("rightHandTransform", "rightHandFollower");

        public static Transform Hand(bool left) => left ? LeftHand : RightHand;

        /// <summary>The local player's body capsule. Phase toggles this; nothing else touches it.</summary>
        public static Collider BodyCollider
        {
            get
            {
                if (!Ready) return null;
                var collider = Reflect.Get<Collider>(_player, "bodyCollider");
                return collider != null ? collider : _player.GetComponent<CapsuleCollider>();
            }
        }

        /// <summary>The local rig's main skin renderer, for local-only colour effects.</summary>
        public static Renderer LocalSkin
        {
            get
            {
                Resolve();

                object rig = Reflect.Get(_tagger, "offlineVRRig");
                return Reflect.Get<Renderer>(rig, "mainSkin");
            }
        }

        /// <summary>
        /// The local player scale.
        ///
        /// Gorilla Tag owns a scale value of its own and drives locomotion from it, so that field
        /// is preferred; writing the root transform directly is the fallback for versions that do
        /// not expose one, and is less well behaved because arm reach no longer matches the world.
        /// </summary>
        public static float PlayerScale
        {
            get
            {
                if (!Ready) return 1f;

                if (Reflect.Get(_player, "scale") is float scale && scale > 0f) return scale;

                Transform root = Root;
                return root != null ? root.localScale.y : 1f;
            }
            set
            {
                if (!Ready || value <= 0f) return;

                if (Reflect.Set(_player, value, "scale")) return;

                Transform root = Root;
                if (root != null) root.localScale = Vector3.one * value;
            }
        }

        public static float? MaxJumpSpeed
        {
            get => Ready ? Reflect.Get(_player, "maxJumpSpeed") as float? : null;
            set { if (Ready && value.HasValue) Reflect.Set(_player, value.Value, "maxJumpSpeed"); }
        }

        public static float? JumpMultiplier
        {
            get => Ready ? Reflect.Get(_player, "jumpMultiplier") as float? : null;
            set { if (Ready && value.HasValue) Reflect.Set(_player, value.Value, "jumpMultiplier"); }
        }

        /// <summary>Controller haptics. Silently does nothing if the game's signature changed.</summary>
        public static void Vibrate(bool leftHand, float strength, float duration)
        {
            if (_tagger == null) Resolve();
            Reflect.Invoke(_tagger, "StartVibration", leftHand, strength, duration);
        }

        /// <summary>Moves the local player, cancelling momentum so you do not arrive mid-flight.</summary>
        public static void TeleportTo(Vector3 position)
        {
            var root = Root;
            if (root == null) return;

            root.position = position;

            var body = Body;
            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private static Transform HandTransform(params string[] names)
        {
            if (_tagger == null) Resolve();

            foreach (string name in names)
            {
                var transform = Reflect.Get<Transform>(_tagger, name);
                if (transform != null) return transform;

                var component = Reflect.Get<Component>(_tagger, name);
                if (component != null) return component.transform;
            }

            return null;
        }

        /// <summary>Re-resolves the singletons at most a few times a second; cheap to call often.</summary>
        private static void Resolve()
        {
            if (_player != null && _tagger != null) return;
            if (Time.unscaledTime < _nextResolve) return;
            _nextResolve = Time.unscaledTime + 0.5f;

            _playerType ??= Reflect.FindType("GorillaLocomotion.GTPlayer", "GorillaLocomotion.Player");
            _taggerType ??= Reflect.FindType("GorillaTagger");

            if (_player == null)
            {
                _player = Reflect.GetStatic(_playerType, "Instance", "instance") as Component;
                _body = null;
            }

            if (_tagger == null)
                _tagger = Reflect.GetStatic(_taggerType, "Instance", "instance") as Component;
        }

        /// <summary>Drops cached references, e.g. across a scene load.</summary>
        internal static void Invalidate()
        {
            _player = null;
            _tagger = null;
            _body = null;
            _nextResolve = 0f;
        }

        /// <summary>One-line diagnostic for the menu's status page.</summary>
        internal static string Describe() =>
            _player == null ? "rig: not found" : $"rig: {_playerType?.Name ?? "?"}";
    }
}
