using System;
using System.Collections.Generic;
using UnityEngine;
using FairPlay.Core;
using FairPlay.Utils;

namespace FairPlay.Menu
{
    /// <summary>
    /// The held panel: a slab that rises out of the hand you hold it in, flanked by two wings
    /// carrying the page arrows, with a panic bar across the top. You press it with your free hand.
    ///
    /// Two layout choices worth calling out:
    ///
    ///   * The slab is anchored to the hand but billboarded to the head, rather than rigidly
    ///     parented to hand rotation. Gorilla Tag hand transforms do not agree between versions on
    ///     which axis points out of the palm, so a rigid parent means the menu faces somewhere new
    ///     after every game update. Billboarding is version-proof and readable while moving.
    ///
    ///   * Rows stay visible when the gate is closed, dimmed and inert, with the reason spelled out
    ///     on the info line. Hiding them would be less honest about what the mod is doing.
    /// </summary>
    internal sealed class MenuController : MonoBehaviour
    {
        private const float FaceZ = MenuTheme.PanelDepth * 0.5f + MenuTheme.Relief * 0.5f;
        private const float TextZ = MenuTheme.PanelDepth * 0.5f + 0.0015f;
        private const float PressLockout = 0.12f;

        private Transform _root;
        private Transform _panel;
        private Transform _backplate;
        private float _panelHeight = MenuTheme.MaxPanelHeight;
        private GameObject _pointer;
        private Renderer _pointerRenderer;

        private readonly MenuButton[] _rows = new MenuButton[MenuTheme.RowsPerPage];
        private MenuButton _prevPage, _nextPage, _pageLabel, _panic;

        private Renderer _statusPlate;
        private TextMesh _statusLabel;
        private TextMesh _infoLabel;
        private TextMesh _fpsLabel;

        private readonly List<MenuPage> _pages = new List<MenuPage>();
        private int _pageIndex;
        private float _pressLockUntil;
        private bool _visible = true;
        private bool _built;

        private static bool MenuOnLeft => Settings.MenuHand.Value == Handedness.Left;

        // ------------------------------------------------------------------ life cycle

        private void OnDestroy() => Teardown();

        internal void Teardown()
        {
            if (_root != null) Destroy(_root.gameObject);
            if (_pointer != null) Destroy(_pointer);
            _root = null;
            _panel = null;
            _pointer = null;
            _built = false;
            _pages.Clear();
        }

        /// <summary>Rebuilds pages from the registry. Safe to call again after modules change.</summary>
        internal void Rebuild()
        {
            Teardown();
            BuildPages();
            BuildGeometry();
            _built = true;
            Plugin.Log.LogInfo("Menu built: " + _pages.Count + " page(s), " +
                               ModuleRegistry.All.Count + " module(s).");
        }

        private void Update()
        {
            if (!_built)
            {
                if (GameRefs.Ready) Rebuild();
                return;
            }

            XRInput.Sample();

            // The holding hand's face button stows and draws the panel.
            if (XRInput.SecondaryDown(MenuOnLeft))
            {
                _visible = !_visible;
                Haptic(MenuOnLeft, 0.4f, 0.04f);
            }

            bool showing = _visible && Anchor();

            if (_root != null && _root.gameObject.activeSelf != showing)
                _root.gameObject.SetActive(showing);
            if (!showing)
            {
                if (_pointer != null && _pointer.activeSelf) _pointer.SetActive(false);
                return;
            }

            bool gateOpen = LobbyGuard.Allowed;
            Vector3? tip = PointerLocal(out Vector3 tipWorld);

            // The dot follows the free hand, and vanishes with it. Leaving it parked at the last
            // tracked position reads as a stray bead floating in the map.
            if (_pointer != null)
            {
                if (tip.HasValue) _pointer.transform.position = tipWorld;
                if (_pointer.activeSelf != tip.HasValue) _pointer.SetActive(tip.HasValue);
            }

            bool hovering = false;
            string hoverText = null;

            foreach (var button in AllButtons())
            {
                if (button == null) continue;

                bool fired = button.Update(tip, gateOpen);

                if (button.Hovered)
                {
                    hovering = true;
                    if (!string.IsNullOrEmpty(button.Description)) hoverText = button.Description;
                }

                if (!fired || Time.unscaledTime < _pressLockUntil) continue;

                _pressLockUntil = Time.unscaledTime + PressLockout;
                Haptic(!MenuOnLeft, 0.6f, 0.05f);

                try { button.OnPress?.Invoke(); }
                catch (Exception e) { Plugin.Log.LogError("Menu press threw: " + e); }
            }

            if (_pointerRenderer != null)
                Draw.SetColor(_pointerRenderer, hovering ? MenuTheme.Accent : MenuTheme.TextMuted);

            PaintStatus(gateOpen, hoverText);
        }

        // ------------------------------------------------------------------ anchoring

        /// <summary>
        /// Stands the slab up out of the holding hand and turns it toward the head.
        ///
        /// Order matters: the rotation is resolved first, because the slab hangs off the hand along
        /// its OWN up axis - that is what makes it read as held rather than worn. The panel origin
        /// is its top edge, so the rise is the full current height.
        /// </summary>
        private bool Anchor()
        {
            if (_root == null) return false;

            Transform hand = GameRefs.Hand(MenuOnLeft);
            Transform head = GameRefs.Head;
            if (hand == null || head == null) return false;

            float scale = Settings.MenuScale.Value;
            _root.localScale = Vector3.one * scale;

            Vector3 grip = hand.position;
            Vector3 toHead = head.position - grip;
            if (toHead.sqrMagnitude > 1e-6f)
                _root.rotation = Quaternion.LookRotation(toHead, head.up);

            Vector3 offset = Settings.MenuOffset.Value;
            if (!MenuOnLeft) offset.x = -offset.x;   // mirror when held in the other hand

            _root.position = grip
                           + _root.up * ((_panelHeight + offset.y) * scale)
                           + _root.rotation * new Vector3(offset.x, 0f, offset.z) * scale;

            return true;
        }

        /// <summary>Fingertip of the free hand in panel local space; null when it is nowhere near.</summary>
        private Vector3? PointerLocal(out Vector3 world)
        {
            world = Vector3.zero;

            Transform hand = GameRefs.Hand(!MenuOnLeft);
            if (hand == null || _panel == null) return null;

            world = hand.position;
            Vector3 local = _panel.InverseTransformPoint(world);

            // Cheap rejection so we skip per-button maths with a hand halfway across the map.
            // Bounds are generous enough to cover the wings and the bar above the top edge.
            if (Mathf.Abs(local.x) > MenuTheme.PanelWidth ||
                local.y > 0.12f || local.y < -MenuTheme.MaxPanelHeight - 0.06f ||
                Mathf.Abs(local.z) > 0.08f)
                return null;

            return local;
        }

        private static void Haptic(bool leftHand, float strength, float duration)
        {
            if (Settings.Haptics.Value) GameRefs.Vibrate(leftHand, strength, duration);
        }

        // ------------------------------------------------------------------ pages

        private void BuildPages()
        {
            AddCategoryPages("MOVE",  ModuleCategory.Movement);
            AddCategoryPages("BODY",  ModuleCategory.Body);
            AddCategoryPages("BUILD", ModuleCategory.Builder);
            AddCategoryPages("LOOK",  ModuleCategory.Visual);

            var info = new MenuPage("INFO");
            info.Rows.Add(MenuRow.Readout("Gate", () => MenuTheme.StatusLabel(LobbyGuard.State),
                                          "Why features are on or off"));
            info.Rows.Add(MenuRow.Readout("Gamemode", () => Trim(LobbyGuard.GameMode, 16),
                                          "Raw gamemode property of the room"));
            info.Rows.Add(MenuRow.Readout("Utilla", () => LobbyGuard.UtillaConfirmed ? "CONFIRMED" : "NO",
                                          "Second half of the lobby gate"));
            info.Rows.Add(MenuRow.Readout("Active",
                                          () => ModuleRegistry.EnabledCount + "/" + ModuleRegistry.All.Count));
            info.Rows.Add(MenuRow.Readout("Rig", GameRefs.Describe, "Resolved game types"));
            info.Rows.Add(MenuRow.Readout("Build", () => "v" + PluginInfo.Version, PluginInfo.Guid));
            _pages.Add(info);
        }

        private void AddCategoryPages(string title, ModuleCategory category)
        {
            var modules = new List<Module>(ModuleRegistry.InCategory(category));
            if (modules.Count == 0) return;

            int pageCount = Mathf.CeilToInt(modules.Count / (float)MenuTheme.RowsPerPage);

            for (int start = 0, page = 1; start < modules.Count; start += MenuTheme.RowsPerPage, page++)
            {
                string heading = pageCount > 1 ? title + " " + page + "/" + pageCount : title;
                var menuPage = new MenuPage(heading);

                int end = Mathf.Min(start + MenuTheme.RowsPerPage, modules.Count);
                for (int i = start; i < end; i++) menuPage.Rows.Add(MenuRow.Toggle(modules[i]));

                _pages.Add(menuPage);
            }
        }

        private void ShowPage(int index)
        {
            if (_pages.Count == 0) return;

            _pageIndex = ((index % _pages.Count) + _pages.Count) % _pages.Count;
            var page = _pages[_pageIndex];

            for (int i = 0; i < _rows.Length; i++)
            {
                var button = _rows[i];
                if (button == null) continue;

                if (i >= page.Rows.Count)
                {
                    button.SetActive(false);
                    continue;
                }

                MenuRow row = page.Rows[i];
                button.SetActive(true);
                button.SetText(row.Label);
                button.Description = row.Description;
                button.Interactable = row.Interactable;
                button.IsOn = row.IsOn;
                button.ValueText = row.Value;
                button.OnPress = row.OnPress;
                button.Repaint(LobbyGuard.Allowed);
            }

            ResizeTo(page.Rows.Count);
        }

        private IEnumerable<MenuButton> AllButtons()
        {
            foreach (var row in _rows) yield return row;
            yield return _prevPage;
            yield return _nextPage;
            yield return _pageLabel;
            yield return _panic;
        }

        private static string Trim(string value, int max) =>
            string.IsNullOrEmpty(value) ? "-"
            : value.Length <= max ? value
            : value.Substring(0, max - 1) + "~";

        // ------------------------------------------------------------------ geometry

        // The panel origin is its TOP edge, so rows never move when the slab is resized for a
        // page with fewer features - only the backplate, the bottom strip and the wings do.
        private static float Left  => -MenuTheme.PanelWidth * 0.5f + MenuTheme.Pad;
        private static float Right =>  MenuTheme.PanelWidth * 0.5f - MenuTheme.Pad;
        private static float RowsTop => -(MenuTheme.Pad + MenuTheme.HeaderH + MenuTheme.RowGap);

        private void BuildGeometry()
        {
            var rootGo = new GameObject("FairPlayMenu");
            DontDestroyOnLoad(rootGo);
            _root = rootGo.transform;
            _panel = _root;

            // Built at full height and repositioned by ResizeTo. Vector3.one here would leave a
            // one-metre cube in the world for any frame where ResizeTo has not run yet.
            _backplate = Draw.Plate(_root, "backplate",
                new Vector3(0f, -MenuTheme.MaxPanelHeight * 0.5f, 0f),
                new Vector3(MenuTheme.PanelWidth, MenuTheme.MaxPanelHeight, MenuTheme.PanelDepth),
                MenuTheme.Backplate).transform;

            // ---- header: name, live readout underneath, gate pill on the right
            float headerY = -MenuTheme.Pad - MenuTheme.HeaderH * 0.5f;

            var title = Draw.Label(_root, "title", new Vector3(0f, headerY + 0.008f, TextZ),
                                   PluginInfo.Name.ToUpperInvariant(), MenuTheme.TitleSize,
                                   MenuTheme.Text, TextAnchor.MiddleCenter);
            title.fontStyle = FontStyle.Bold;

            _fpsLabel = Draw.Label(_root, "fps", new Vector3(0f, headerY - 0.012f, TextZ),
                                   string.Empty, MenuTheme.SmallSize * 0.9f,
                                   MenuTheme.TextMuted, TextAnchor.MiddleCenter);

            var statusPlate = Draw.Plate(_root, "status",
                new Vector3(Right - 0.022f, headerY + 0.008f, FaceZ),
                new Vector3(0.044f, 0.017f, MenuTheme.Relief), MenuTheme.Danger);
            _statusPlate = statusPlate.GetComponent<Renderer>();

            _statusLabel = Draw.Label(statusPlate.transform, "statusText", new Vector3(0f, 0f, 0.6f),
                                      "LOCKED", MenuTheme.SmallSize * 0.8f,
                                      MenuTheme.TextOnAccent, TextAnchor.MiddleCenter);
            _statusLabel.transform.localScale =
                new Vector3(1f / 0.044f, 1f / 0.017f, 1f / MenuTheme.Relief);

            // ---- rows, built at the maximum and hidden per page
            float rowW = MenuTheme.PanelWidth - MenuTheme.Pad * 2f;
            for (int i = 0; i < _rows.Length; i++)
            {
                float y = RowsTop - MenuTheme.RowH * 0.5f - i * (MenuTheme.RowH + MenuTheme.RowGap);
                _rows[i] = new MenuButton(_root, "row" + i, new Vector3(0f, y, FaceZ),
                                          new Vector2(rowW, MenuTheme.RowH), string.Empty);
            }

            // ---- bottom strip: hovered description on the left, page name on the right
            _infoLabel = Draw.Label(_root, "info", new Vector3(Left, 0f, TextZ),
                                    string.Empty, MenuTheme.SmallSize * 0.9f,
                                    MenuTheme.TextMuted, TextAnchor.MiddleLeft);

            const float pageW = 0.058f;
            _pageLabel = new MenuButton(_root, "page",
                                        new Vector3(Right - pageW * 0.5f, 0f, FaceZ),
                                        new Vector2(pageW, MenuTheme.FooterH), "-",
                                        TextAnchor.MiddleCenter);
            _pageLabel.Interactable = false;

            // ---- wings: the arrows sit off the slab so a near-miss cannot hit a feature row
            float wingX = MenuTheme.PanelWidth * 0.5f + MenuTheme.WingGap + MenuTheme.WingWidth * 0.5f;
            var wingSize = new Vector2(MenuTheme.WingWidth, MenuTheme.WingHeight);

            _prevPage = new MenuButton(_root, "prev", new Vector3(-wingX, 0f, 0f), wingSize,
                                       "<", TextAnchor.MiddleCenter);
            _prevPage.OnPress = () => ShowPage(_pageIndex - 1);
            _prevPage.Description = "Previous page";

            _nextPage = new MenuButton(_root, "next", new Vector3(wingX, 0f, 0f), wingSize,
                                       ">", TextAnchor.MiddleCenter);
            _nextPage.OnPress = () => ShowPage(_pageIndex + 1);
            _nextPage.Description = "Next page";

            // ---- top bar: the panic switch, kept away from everything else on purpose
            float barY = MenuTheme.TopBarGap + MenuTheme.TopBarHeight * 0.5f;

            _panic = new MenuButton(_root, "panic", new Vector3(0f, barY, 0f),
                                    new Vector2(MenuTheme.TopBarWidth, MenuTheme.TopBarHeight),
                                    "ALL OFF", TextAnchor.MiddleCenter);
            _panic.Description = "Turn every module off and revert";
            _panic.OnPress = () =>
            {
                ModuleRegistry.DisableAll("panic button");
                Haptic(true, 0.8f, 0.1f);
                Haptic(false, 0.8f, 0.1f);
            };

            // ---- fingertip dot
            _pointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _pointer.name = "FairPlayPointer";
            Destroy(_pointer.GetComponent<Collider>());
            DontDestroyOnLoad(_pointer);
            _pointer.transform.localScale = Vector3.one * 0.016f;
            _pointerRenderer = _pointer.GetComponent<Renderer>();
            _pointerRenderer.material = Draw.Flat(MenuTheme.TextMuted);

            // ShowPage is what calls ResizeTo, so an empty registry has to size the slab itself.
            if (_pages.Count > 0) ShowPage(0);
            else ResizeTo(0);

            PaintStatus(LobbyGuard.Allowed, null);
        }

        /// <summary>Grows or shrinks the slab so the bottom strip always sits under the last row.</summary>
        private void ResizeTo(int rows)
        {
            _panelHeight = MenuTheme.HeightForRows(rows);

            if (_backplate != null)
            {
                _backplate.localPosition = new Vector3(0f, -_panelHeight * 0.5f, 0f);
                _backplate.localScale =
                    new Vector3(MenuTheme.PanelWidth, _panelHeight, MenuTheme.PanelDepth);
            }

            float stripY = RowsTop - rows * (MenuTheme.RowH + MenuTheme.RowGap)
                                   - MenuTheme.FooterH * 0.5f;

            if (_infoLabel != null)
                _infoLabel.transform.localPosition = new Vector3(Left, stripY, TextZ);

            _pageLabel?.SetLocalY(stripY);
            _prevPage?.SetLocalY(-_panelHeight * 0.5f);
            _nextPage?.SetLocalY(-_panelHeight * 0.5f);
        }

        private void PaintStatus(bool gateOpen, string hoverText)
        {
            if (_statusPlate != null)
                Draw.SetColor(_statusPlate, MenuTheme.StatusColor(LobbyGuard.State));

            if (_statusLabel != null)
                _statusLabel.text = MenuTheme.StatusLabel(LobbyGuard.State);

            if (_fpsLabel != null)
            {
                int fps = Mathf.RoundToInt(1f / Mathf.Max(Time.smoothDeltaTime, 1e-4f));
                _fpsLabel.text = "FPS " + fps + "    " + ModuleRegistry.EnabledCount + " ACTIVE";
            }

            if (_pageLabel != null && _pages.Count > 0)
                _pageLabel.SetText(_pages[_pageIndex].Title);

            if (_infoLabel == null) return;

            string text = !gateOpen ? LobbyGuard.Reason
                        : !string.IsNullOrEmpty(hoverText) ? hoverText
                        : ModuleRegistry.EnabledCount + " active";

            _infoLabel.text = Trim(text, 30);
            Draw.SetColor(_infoLabel, gateOpen ? MenuTheme.TextMuted : MenuTheme.Danger);
        }
    }
}
