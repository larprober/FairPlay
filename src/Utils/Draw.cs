using UnityEngine;

namespace FairPlay.Utils
{
    /// <summary>
    /// Builds the menu's geometry out of Unity primitives at runtime.
    ///
    /// No asset bundle on purpose: a bundle has to be rebuilt against the exact Unity version the
    /// game ships, which is one more thing to break on patch day. Primitives plus legacy
    /// <see cref="TextMesh"/> render on every version the game has shipped, and the whole menu is
    /// then readable as code in this repository rather than as a binary blob.
    /// </summary>
    internal static class Draw
    {
        private static Shader _unlit;
        private static Font _font;
        private static Shader _textShader;

        /// <summary>An unlit-ish material, resolved once against whatever the build actually contains.</summary>
        public static Material Flat(Color color)
        {
            if (_unlit == null) _unlit = ResolveShader();

            var material = new Material(_unlit);

            // Set whichever colour slot the winning shader actually has. Assigning Material.color
            // blind logs a warning per call on shaders without _Color, and this runs every frame.
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))     material.SetColor("_Color", color);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0f);

            return material;
        }

        private static Shader ResolveShader()
        {
            string[] candidates =
            {
                "Unlit/Color",
                "GorillaTag/UberShader",
                "Universal Render Pipeline/Unlit",
                "Sprites/Default",
                "Standard"
            };

            foreach (string name in candidates)
            {
                var shader = Shader.Find(name);
                if (shader != null) return shader;
            }

            // Last resort: whatever a fresh primitive is using is guaranteed to exist in the build.
            // Deactivated before anything can see or collide with it - a live cube at the world
            // origin for one frame is enough to shove a player standing there.
            var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            probe.SetActive(false);
            var fallback = probe.GetComponent<Renderer>().sharedMaterial.shader;
            Object.Destroy(probe);
            return fallback;
        }

        public static Font UiFont
        {
            get
            {
                if (_font != null) return _font;

                // Resources.GetBuiltinResource is largely an editor-side API and commonly returns
                // null in a shipped player - which would leave the panel rendering as blank
                // coloured bars with no labels at all. Hence four fallbacks, ending at fonts the
                // game itself already loaded and then at the OS.
                //
                // The winner is logged. If text is missing in game, the log names the stage that
                // failed instead of leaving you staring at an empty panel guessing why.
                string source = "builtin LegacyRuntime";
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                if (_font == null)
                {
                    source = "builtin Arial";
                    _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                if (_font == null)
                {
                    source = "font already loaded by the game";
                    _font = FirstLoadedFont();
                }

                if (_font == null)
                {
                    source = "OS Arial";
                    _font = Font.CreateDynamicFontFromOSFont("Arial", 48);
                }

                if (_font == null)
                {
                    source = "OS Segoe UI";
                    _font = Font.CreateDynamicFontFromOSFont("Segoe UI", 48);
                }

                Plugin.Log.LogInfo(_font != null
                    ? "Menu font resolved from: " + source
                    : "Menu font: NONE FOUND - labels will render blank");

                return _font;
            }
        }

        /// <summary>Any Font the game has already loaded, as a last resort before the OS.</summary>
        private static Font FirstLoadedFont()
        {
            Font[] loaded = Resources.FindObjectsOfTypeAll<Font>();
            if (loaded == null) return null;

            foreach (Font font in loaded)
                if (font != null) return font;

            return null;
        }

        /// <summary>A flat panel. Colliders are stripped so the menu can never shove the player.</summary>
        public static GameObject Plate(Transform parent, string name, Vector3 localPosition,
                                       Vector3 localScale, Color color)
        {
            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = name;

            var collider = plate.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);

            plate.transform.SetParent(parent, false);
            plate.transform.localPosition = localPosition;
            plate.transform.localRotation = Quaternion.identity;
            plate.transform.localScale = localScale;

            var renderer = plate.GetComponent<Renderer>();
            renderer.material = Flat(color);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return plate;
        }

        /// <summary>World-space label. <paramref name="size"/> is in metres of cap height, roughly.</summary>
        public static TextMesh Label(Transform parent, string name, Vector3 localPosition,
                                     string text, float size, Color color,
                                     TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;

            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.font = UiFont;
            mesh.fontSize = 64;                 // render at high res, then scale down
            mesh.characterSize = size;
            mesh.color = color;
            mesh.anchor = anchor;
            mesh.alignment = anchor == TextAnchor.MiddleCenter ? TextAlignment.Center : TextAlignment.Left;
            mesh.richText = false;

            if (_textShader == null) _textShader = Shader.Find("GUI/Text Shader");

            var renderer = go.GetComponent<MeshRenderer>();
            Material fontMaterial = UiFont != null ? UiFont.material : null;

            renderer.material = _textShader != null   ? new Material(_textShader)
                              : fontMaterial != null  ? new Material(fontMaterial)
                              : renderer.material;

            if (fontMaterial != null) renderer.material.mainTexture = fontMaterial.mainTexture;
            renderer.material.color = color;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return mesh;
        }

        /// <summary>Reads back a renderer colour without touching a slot the shader lacks.</summary>
        public static Color GetColor(Renderer renderer, Color fallback)
        {
            if (renderer == null || renderer.material == null) return fallback;

            if (renderer.material.HasProperty("_Color"))     return renderer.material.GetColor("_Color");
            if (renderer.material.HasProperty("_BaseColor")) return renderer.material.GetColor("_BaseColor");

            return fallback;
        }

        public static void SetColor(Renderer renderer, Color color)
        {
            if (renderer == null || renderer.material == null) return;

            if (renderer.material.HasProperty("_Color"))     renderer.material.SetColor("_Color", color);
            if (renderer.material.HasProperty("_BaseColor")) renderer.material.SetColor("_BaseColor", color);
        }

        public static void SetColor(TextMesh label, Color color)
        {
            if (label == null) return;
            label.color = color;

            var renderer = label.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.material != null) renderer.material.color = color;
        }
    }
}
