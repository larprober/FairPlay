using System;
using System.Linq;
using System.Reflection;

namespace FairPlay.Utils
{
    /// <summary>
    /// Small reflection helpers used by <see cref="GameRefs"/>.
    ///
    /// Everything here treats "member not found" as a normal outcome rather than an error: Gorilla
    /// Tag renames and reshuffles its classes between updates, and a mod that hard-crashes on a
    /// renamed field is a mod that breaks every patch day.
    /// </summary>
    internal static class Reflect
    {
        private const BindingFlags Any =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        /// <summary>Finds a type by full name across every loaded assembly, trying each candidate in order.</summary>
        public static Type FindType(params string[] fullNames)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (string name in fullNames)
            {
                foreach (var assembly in assemblies)
                {
                    Type type;
                    try { type = assembly.GetType(name, false); }
                    catch { continue; }
                    if (type != null) return type;
                }
            }

            return null;
        }

        /// <summary>Reads a static property or field, whichever exists.</summary>
        public static object GetStatic(Type type, params string[] memberNames)
        {
            if (type == null) return null;

            foreach (string name in memberNames)
            {
                var property = type.GetProperty(name, Any);
                if (property != null && property.CanRead)
                {
                    try { return property.GetValue(null); } catch { }
                }

                var field = type.GetField(name, Any);
                if (field != null)
                {
                    try { return field.GetValue(null); } catch { }
                }
            }

            return null;
        }

        /// <summary>Reads an instance property or field, whichever exists.</summary>
        public static object Get(object target, params string[] memberNames)
        {
            if (target == null) return null;
            var type = target.GetType();

            foreach (string name in memberNames)
            {
                var property = type.GetProperty(name, Any);
                if (property != null && property.CanRead)
                {
                    try { return property.GetValue(target); } catch { }
                }

                var field = type.GetField(name, Any);
                if (field != null)
                {
                    try { return field.GetValue(target); } catch { }
                }
            }

            return null;
        }

        public static T Get<T>(object target, params string[] memberNames)
        {
            object value = Get(target, memberNames);
            return value is T typed ? typed : default;
        }

        /// <summary>Writes an instance property or field. Returns false if nothing matched.</summary>
        public static bool Set(object target, object value, params string[] memberNames)
        {
            if (target == null) return false;
            var type = target.GetType();

            foreach (string name in memberNames)
            {
                var property = type.GetProperty(name, Any);
                if (property != null && property.CanWrite)
                {
                    try { property.SetValue(target, value); return true; } catch { }
                }

                var field = type.GetField(name, Any);
                if (field != null && !field.IsInitOnly)
                {
                    try { field.SetValue(target, value); return true; } catch { }
                }
            }

            return false;
        }

        /// <summary>Calls an instance method by name, matching on argument count.</summary>
        public static bool Invoke(object target, string methodName, params object[] args)
        {
            if (target == null) return false;

            var method = target.GetType()
                .GetMethods(Any)
                .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == args.Length);

            if (method == null) return false;

            try { method.Invoke(target, args); return true; }
            catch { return false; }
        }
    }
}
