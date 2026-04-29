using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Fu
{
    /// <summary>
    /// Represents the Fu Window Name Provider type.
    /// </summary>
    public static class FuWindowNameProvider
    {
        #region State
        private static Dictionary<ushort, FuWindowName> _cached;
        #endregion

        #region Methods
        /// <summary>
        /// Add a new window name to the cache or update an existing one.
        /// </summary>
        /// <param name="windowName"> The FuWindowName to add or update.</param>
        internal static bool AddWindowName(FuWindowName windowName)
        {
            if (_cached == null)
                _cached = new Dictionary<ushort, FuWindowName>();
            if (_cached.ContainsKey(windowName.ID))
                return false; // Already exists
            _cached[windowName.ID] = windowName;
            return true; // Successfully added
        }

        /// <summary>
        /// Clears the cached window names so the next lookup scans loaded assemblies again.
        /// </summary>
        public static void ClearCache()
        {
            _cached = null;
        }

        /// <summary>
        /// Get all window names defined in the project.
        /// </summary>
        /// <returns> List of FuWindowName objects.</returns>
        public static Dictionary<ushort, FuWindowName> GetAllWindowNames()
        {
            if (_cached != null) return _cached;

            _cached = new Dictionary<ushort, FuWindowName>();

            AddWindowName(FuSystemWindowsNames.None);
            AddWindowName(FuSystemWindowsNames.FuguiSettings);

            // Scan all assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                Type[] assemblyTypes;

                try
                {
                    assemblyTypes = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    assemblyTypes = ex.Types.Where(type => type != null).ToArray();
                }

                // Find types that inherit from FuSystemWindowsNames (excluding it)
                var types = assemblyTypes
                    .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(FuSystemWindowsNames)));

                foreach (var type in types)
                {
                    // Look for a static method "GetAllWindowsNames" that returns List<FuWindowName>
                    var method = type.GetMethod("GetAllWindowsNames", BindingFlags.Public | BindingFlags.Static);
                    if (method != null && typeof(List<FuWindowName>).IsAssignableFrom(method.ReturnType))
                    {
                        var listy = method.Invoke(null, null) as List<FuWindowName>;
                        if (listy == null)
                            continue;
                        // Add each FuWindowName to the cache
                        foreach (var windowName in listy)
                        {
                            AddWindowName(windowName);
                        }
                    }
                }
            }

            return _cached;
        }
        #endregion
    }
}
