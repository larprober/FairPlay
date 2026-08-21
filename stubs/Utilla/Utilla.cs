// Reference-only stub. Utilla owns the MODDED gamemode registration and this gate attribute.
using System;

namespace Utilla
{
    /// <summary>
    /// Applied to a MonoBehaviour, Utilla enables it on entering a modded gamemode and disables it
    /// on leaving. FairPlay uses exactly those two edges and nothing else from Utilla.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ModdedGamemodeAttribute : Attribute
    {
        public ModdedGamemodeAttribute() { }
        public ModdedGamemodeAttribute(string gamemode) { }
    }
}
