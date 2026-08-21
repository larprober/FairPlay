using System;
using UnityEngine;
using FairPlay.Utils;

namespace FairPlay.Menu
{
    /// <summary>
    /// One touchable row.
    ///
    /// Deliberately not a MonoBehaviour: <see cref="MenuController"/> owns the update order, which
    /// keeps presses deterministic and means a destroyed panel cannot leave orphaned components
    /// ticking away in the scene.
    ///
    /// Press model: the fingertip must LEAVE a row before that row can fire again. A plain
    /// distance test re-triggers every frame you rest a hand on the panel, which in VR reads as
    /// the menu toggling itself at random.
    /// </summary>
    internal sealed class MenuButton
    {
        private readonly GameObject _plate;
        private readonly Renderer _renderer;
        private readonly TextMesh _label;
        private readonly TextMesh _value;
        private readonly Vector2 _halfSize;

        private bool _fingerInside;
        private bool _hovered;

        public Action OnPress;

        /// <summary>Lit state, or null for a button that is not a toggle.</summary>
        public Func<bool?> IsOn;

        /// <summary>Optional right-aligned value text, e.g. a page name or a live number.</summary>
        public Func<string> ValueText;

        public string Description = string.Empty;
        public bool Interactable = true;
        public bool Hovered => _hovered;

        public MenuButton(Transform panel, string name, Vector3 localPosition, Vector2 size,
                          string text, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            _halfSize = size * 0.5f;

            _plate = Draw.Plate(panel, "btn_" + name, localPosition,
                                new Vector3(size.x, size.y, MenuTheme.Relief), MenuTheme.Surface);
            _renderer = _plate.GetComponent<Renderer>();

            // Children live in the plate scaled space, so a local offset of 1 equals one plate width.
            float labelX = anchor == TextAnchor.MiddleCenter ? 0f : (-_halfSize.x + MenuTheme.Pad * 0.6f) / size.x;

            _label = Draw.Label(_plate.transform, "label", new Vector3(labelX, 0f, 0.6f),
                                text, MenuTheme.RowSize, MenuTheme.Text, anchor);
            Unscale(_label.transform, size);

            _value = Draw.Label(_plate.transform, "value",
                                new Vector3((_halfSize.x - MenuTheme.Pad * 0.6f) / size.x, 0f, 0.6f),
                                string.Empty, MenuTheme.SmallSize, MenuTheme.TextMuted, TextAnchor.MiddleRight);
            Unscale(_value.transform, size);
        }

        /// <summary>Cancels the parent plate non-uniform scale so glyphs stay square.</summary>
        private static void Unscale(Transform child, Vector2 plateSize)
        {
            child.localScale = new Vector3(1f / plateSize.x, 1f / plateSize.y, 1f / MenuTheme.Relief);
        }

        public void SetText(string text)
        {
            if (_label != null) _label.text = text;
        }

        /// <summary>Used when the slab is resized for a page with fewer rows.</summary>
        public void SetLocalY(float y)
        {
            if (_plate == null) return;

            Vector3 position = _plate.transform.localPosition;
            _plate.transform.localPosition = new Vector3(position.x, y, position.z);
        }

        public void SetActive(bool active)
        {
            if (_plate == null) return;
            _plate.SetActive(active);
            if (!active)
            {
                _fingerInside = false;
                _hovered = false;
            }
        }

        public bool IsVisible => _plate != null && _plate.activeInHierarchy;

        /// <summary>
        /// <paramref name="tip">tip</paramref> is the fingertip expressed in the PANEL local space,
        /// or null when no hand is near. Returns true on the single frame the button fires.
        ///
        /// Presses are deliberately NOT gated on <paramref name="gateOpen">gateOpen</paramref>: the
        /// page arrows and the panic bar have to work while the lobby gate is shut, and that is
        /// exactly when you want to page to INFO and read why. A press on a feature row still goes
        /// nowhere, because Module.SetEnabled is the thing that refuses. gateOpen only dims.
        /// </summary>
        public bool Update(Vector3? tip, bool gateOpen)
        {
            if (_plate == null || !_plate.activeInHierarchy) return false;

            bool inside = false;

            if (tip.HasValue && Interactable)
            {
                Vector3 delta = tip.Value - _plate.transform.localPosition;

                // Slack is capped at half the gap between rows. Anything more and neighbouring hit
                // zones overlap, which in practice means pressing the row above the one you aimed
                // at - the fingertip is a fat target and rows are 4 mm apart.
                float slack = Mathf.Min(MenuTheme.TipRadius * 0.35f, MenuTheme.RowGap * 0.5f);

                inside = Mathf.Abs(delta.x) <= _halfSize.x + slack &&
                         Mathf.Abs(delta.y) <= _halfSize.y + slack &&
                         Mathf.Abs(delta.z) <= MenuTheme.TipRadius;
            }

            _hovered = inside;
            bool fired = inside && !_fingerInside;
            _fingerInside = inside;

            Repaint(gateOpen);
            return fired;
        }

        public void Repaint(bool gateOpen)
        {
            if (_renderer == null) return;

            bool? on = IsOn?.Invoke();
            Color plate;
            Color text;

            if (!Interactable || !gateOpen)
            {
                plate = MenuTheme.Surface;
                text  = MenuTheme.TextMuted;
            }
            else if (on == true)
            {
                plate = _hovered ? MenuTheme.Accent : MenuTheme.AccentDim;
                text  = _hovered ? MenuTheme.TextOnAccent : MenuTheme.Text;
            }
            else
            {
                plate = _hovered ? MenuTheme.SurfaceHi : MenuTheme.Surface;
                text  = MenuTheme.Text;
            }

            Draw.SetColor(_renderer, plate);
            Draw.SetColor(_label, text);

            if (_value == null) return;

            string suffix = ValueText != null
                ? ValueText()
                : on == true ? "ON" : on == false ? "OFF" : string.Empty;

            _value.text = suffix;
            Draw.SetColor(_value, on == true && _hovered ? MenuTheme.TextOnAccent : MenuTheme.TextMuted);
        }

        public void Destroy()
        {
            if (_plate != null) UnityEngine.Object.Destroy(_plate);
        }
    }
}
