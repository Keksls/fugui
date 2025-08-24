using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ImGuiNET
{
    /// <summary>
    /// Provides a managed callback to handle ImGui assertion failures from native code.
    /// </summary>
    public static class ImGuiAssertHandler
    {
        /// <summary>
        /// Delegate signature matching the native assertion callback.
        /// </summary>
        /// <param name="expression">The failed expression as a string.</param>
        /// <param name="file">The file where the assertion occurred.</param>
        /// <param name="line">The line number of the assertion.</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ImGuiAssertCallback(string expression, string file, int line);

        // Keep a reference alive to prevent GC from collecting the delegate.
        private static ImGuiAssertCallback _callbackDelegate;

        /// <summary>
        /// Sets up the managed assertion handler and installs it into native ImGui.
        /// </summary>
        public static void Initialize()
        {
            _callbackDelegate = HandleAssert;
            SetImGuiAssertCallback(_callbackDelegate);
        }

        /// <summary>
        /// The method that will be called when an assertion fails in native ImGui.
        /// </summary>
        private static void HandleAssert(string expr, string file, int line)
        {
            string message = $"[IM_ASSERT] Assertion failed: {expr}\nFile: {file}\nLine: {line}";
            Debug.LogError(message);
        }

        /// <summary>
        /// Imports the native function to set the ImGui assertion callback.
        /// </summary>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetImGuiAssertCallback(ImGuiAssertCallback callback);
    }

    /// <summary>
    /// Exception thrown when an ImGui assertion fails.
    /// </summary>
    public class ImGuiAssertionException : Exception
    {
        public ImGuiAssertionException(string message) : base(message) { }
    }
}