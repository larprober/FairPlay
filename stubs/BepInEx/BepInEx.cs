// Reference-only stubs. Signatures must match real BepInEx 5.4.x exactly; bodies never run.
using System;
using UnityEngine;

namespace BepInEx.Logging
{
    public class ManualLogSource
    {
        public void LogInfo(object data) { }
        public void LogWarning(object data) { }
        public void LogError(object data) { }
        public void LogDebug(object data) { }
        public void LogFatal(object data) { }
        public void LogMessage(object data) { }
    }
}

namespace BepInEx.Configuration
{
    public abstract class AcceptableValueBase
    {
        protected AcceptableValueBase(Type valueType) { }
    }

    public class AcceptableValueRange<T> : AcceptableValueBase where T : IComparable
    {
        public AcceptableValueRange(T minValue, T maxValue) : base(typeof(T)) { }
    }

    public class ConfigDescription
    {
        public ConfigDescription(string description, AcceptableValueBase acceptableValues = null,
                                 params object[] tags) { }
    }

    public sealed class ConfigEntry<T>
    {
        public T Value { get; set; }
    }

    public class ConfigFile
    {
        public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue,
                                      string description = null) => null;

        public ConfigEntry<T> Bind<T>(string section, string key, T defaultValue,
                                      ConfigDescription configDescription) => null;
    }
}

namespace BepInEx
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class BepInPlugin : Attribute
    {
        public BepInPlugin(string GUID, string Name, string Version) { }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class BepInDependency : Attribute
    {
        public BepInDependency(string DependencyGUID) { }
        public BepInDependency(string DependencyGUID, string MinimumDependencyVersion) { }
    }

    public abstract class BaseUnityPlugin : MonoBehaviour
    {
        protected Logging.ManualLogSource Logger { get; }
        public Configuration.ConfigFile Config { get; }
    }
}
