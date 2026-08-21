using UnityEngine;
using FairPlay.Core;

namespace FairPlay.Menu
{
    /// <summary>
    /// One palette and one layout table for the whole menu, so no module file hardcodes a colour
    /// or a magic offset. Sizes are metres, in the panel's local space.
    /// </summary>
    internal static class MenuTheme
    {
        // ---- palette -------------------------------------------------------
        public static readonly Color Backplate = new Color32(0x14, 0x16, 0x1C, 0xFF);
        public static readonly Color Surface   = new Color32(0x1E, 0x21, 0x2B, 0xFF);
        public static readonly Color SurfaceHi = new Color32(0x2A, 0x2F, 0x3D, 0xFF);
        public static readonly Color Accent    = new Color32(0x4F, 0xE3, 0xA1, 0xFF); // module is on
        public static readonly Color AccentDim = new Color32(0x24, 0x6B, 0x51, 0xFF);
        public static readonly Color Danger    = new Color32(0xFF, 0x5C, 0x5C, 0xFF); // gate closed
        public static readonly Color Caution   = new Color32(0xFF, 0xB8, 0x4D, 0xFF); // gate pending
        public static readonly Color Text      = new Color32(0xEA, 0xEE, 0xF5, 0xFF);
        public static readonly Color TextMuted = new Color32(0x8A, 0x93, 0xA6, 0xFF);
        public static readonly Color TextOnAccent = new Color32(0x0B, 0x1A, 0x14, 0xFF);

        // ---- layout --------------------------------------------------------
        // A held slab, not a wristwatch: it rises out of the hand you hold it in, roughly the size
        // of a large tablet at Gorilla Tag scale, with the page arrows on separate wings either
        // side so a thumb-width miss does not hit a feature row.
        public const float PanelWidth = 0.200f;
        public const float PanelDepth = 0.008f;

        public const float Pad     = 0.010f;
        public const float HeaderH = 0.046f;
        public const float RowH    = 0.032f;
        public const float RowGap  = 0.004f;
        public const float FooterH = 0.022f;
        /// <summary>Most rows a page may hold. The slab is sized to the page, not to this.</summary>
        public const int RowsPerPage = 7;

        /// <summary>
        /// Height of a slab showing <paramref name="rows">rows</paramref> rows. Pages hold different
        /// numbers of features, and a fixed slab would leave dead space under the short ones, so
        /// the backplate is resized per page instead.
        /// </summary>
        public static float HeightForRows(int rows) =>
            Pad + HeaderH + RowGap + rows * (RowH + RowGap) + FooterH + Pad;

        /// <summary>Tallest the slab ever gets; used for cheap proximity rejection.</summary>
        public static readonly float MaxPanelHeight = HeightForRows(RowsPerPage);

        // Side wings carrying the page arrows.
        public const float WingWidth  = 0.048f;
        public const float WingHeight = 0.150f;
        public const float WingGap    = 0.014f;

        // Top bar above the slab, carrying the panic button.
        public const float TopBarWidth  = 0.140f;
        public const float TopBarHeight = 0.030f;
        public const float TopBarGap    = 0.012f;

        /// <summary>How far the interactive parts stand off the backplate.</summary>
        public const float Relief = 0.006f;

        /// <summary>Radius of the fingertip sphere used for press tests.</summary>
        public const float TipRadius = 0.020f;

        // ---- type ----------------------------------------------------------
        public const float TitleSize = 0.0026f;
        public const float RowSize   = 0.0022f;
        public const float SmallSize = 0.0016f;


        public static Color StatusColor(GuardState state)
        {
            switch (state)
            {
                case GuardState.Modded:               return Accent;
                case GuardState.NoRoom:               return Settings.OfflineSandbox ? Caution : Danger;
                case GuardState.AwaitingConfirmation: return Caution;
                default:                              return Danger;
            }
        }

        public static string StatusLabel(GuardState state)
        {
            switch (state)
            {
                case GuardState.Modded:               return "MODDED";
                case GuardState.NoRoom:               return "OFFLINE";
                case GuardState.AwaitingConfirmation: return "CHECKING";
                default:                              return "LOCKED";
            }
        }
    }
}
