using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Fu.Core
{
    public static class FuWindowNameProvider
    {
        private static Dictionary<ushort, FuWindowName> _cached;

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
        /// Get all window names defined in the project.
        /// </summary>
        /// <returns> List of FuWindowName objects.</returns>
        public static Dictionary<ushort, FuWindowName> GetAllWindowNames()
        {
            if (_cached != null) return _cached;

            // Scan all assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                // Find types that inherit from FuSystemWindowsNames (excluding it)
                var types = assembly.GetTypes()
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
                        _cached = new Dictionary<ushort, FuWindowName>();
                        foreach (var windowName in listy)
                        {
                            if (!AddWindowName(windowName))
                            {
                                // Log a warning if the window name already exists
                                Console.WriteLine($"Warning: Window name {windowName.Name} with ID {windowName.ID} already exists.");
                            }
                        }
                        if (_cached != null)
                            return _cached;
                    }
                }
            }

            // Fallback
            return new Dictionary<ushort, FuWindowName>();
        }
    }
}