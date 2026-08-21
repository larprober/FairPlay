using System;
using System.Collections.Generic;

namespace FairPlay.Menu
{
    /// <summary>A single row on a page, described declaratively so pages can be built from data.</summary>
    internal sealed class MenuRow
    {
        public string Label;
        public string Description = string.Empty;
        public Func<bool?> IsOn;
        public Func<string> Value;
        public Action OnPress;
        public bool Interactable = true;

        public static MenuRow Toggle(Core.Module module) => new MenuRow
        {
            Label = module.Name,
            Description = module.Description,
            IsOn = () => module.Enabled,
            Value = () => module.StatusText ?? (module.Enabled ? "ON" : "OFF"),
            OnPress = module.Press
        };

        /// <summary>A read-only row: no toggle dot, just a live value on the right.</summary>
        public static MenuRow Readout(string label, Func<string> value, string description = "") => new MenuRow
        {
            Label = label,
            Description = description,
            Value = value,
            Interactable = false
        };
    }

    internal sealed class MenuPage
    {
        public string Title;
        public readonly List<MenuRow> Rows = new List<MenuRow>();

        public MenuPage(string title) => Title = title;
    }
}
