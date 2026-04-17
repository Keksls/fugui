using System;
using System.Runtime.InteropServices;
using AOT;
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
        /// <param name="expression">Pointer to the failed expression string.</param>
        /// <param name="file">Pointer to the file string.</param>
        /// <param name="line">Line number where the assertion occurred.</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ImGuiAssertCallback(IntPtr expression, IntPtr file, int line);

        /// <summary>
        /// Keeps a reference alive to prevent GC from collecting the delegate.
        /// </summary>
        private static readonly ImGuiAssertCallback _callbackDelegate = HandleAssert;

        /// <summary>
        /// Sets up the managed assertion handler and installs it into native ImGui.
        /// </summary>
        public static void Initialize()
        {
            SetImGuiAssertCallback(_callbackDelegate);
        }

        /// <summary>
        /// Called from native code when an ImGui assertion fails.
        /// </summary>
        [MonoPInvokeCallback(typeof(ImGuiAssertCallback))]
        private static void HandleAssert(IntPtr expr, IntPtr file, int line)
        {
            string expression = Marshal.PtrToStringAnsi(expr) ?? "<null>";
            string filePath = Marshal.PtrToStringAnsi(file) ?? "<null>";

            string message = $"[IM_ASSERT] Assertion failed: {expression}\nFile: {filePath}\nLine: {line}";
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
        public ImGuiAssertionException(string message) : base(message)
        {
        }
    }
}