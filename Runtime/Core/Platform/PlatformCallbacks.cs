using AOT;
using ImGuiNET;
using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Defines the Get Clipboard Text Callback callback signature.
    /// </summary>
    /// <param name="user_data">The user data value.</param>
    /// <returns>The result of the operation.</returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate string GetClipboardTextCallback(void* user_data);
    /// <summary>
    /// Defines the Get Clipboard Text Safe Callback callback signature.
    /// </summary>
    /// <param name="user_data">The user data value.</param>
    /// <returns>The result of the operation.</returns>
    public delegate string GetClipboardTextSafeCallback(IntPtr user_data);

    /// <summary>
    /// Defines the Set Clipboard Text Callback callback signature.
    /// </summary>
    /// <param name="user_data">The user data value.</param>
    /// <param name="text">The text value.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void SetClipboardTextCallback(void* user_data, byte* text);
    /// <summary>
    /// Defines the Set Clipboard Text Safe Callback callback signature.
    /// </summary>
    /// <param name="user_data">The user data value.</param>
    /// <param name="text">The text value.</param>
    public delegate void SetClipboardTextSafeCallback(IntPtr user_data, string text);

    /// <summary>
    /// Defines the Ime Set Input Screen Pos Callback callback signature.
    /// </summary>
    /// <param name="x">The x value.</param>
    /// <param name="y">The y value.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ImeSetInputScreenPosCallback(int x, int y);

    /// <summary>
    /// Represents the Platform Callbacks type.
    /// </summary>
    public unsafe class PlatformCallbacks
    {
        #region State
        private static GetClipboardTextCallback _getClipboardText;
        private static SetClipboardTextCallback _setClipboardText;
        #endregion

        #region Methods
        /// <summary>
        /// Gets the clipboard text callback.
        /// </summary>
        /// <param name="user_data">The user data value.</param>
        /// <returns>The result of the operation.</returns>
        [MonoPInvokeCallback(typeof(GetClipboardTextCallback))]
        public static unsafe string GetClipboardTextCallback(void* user_data)
        {
            return GUIUtility.systemCopyBuffer;
        }

        /// <summary>
        /// Sets the clipboard text callback.
        /// </summary>
        /// <param name="user_data">The user data value.</param>
        /// <param name="text">The text value.</param>
        [MonoPInvokeCallback(typeof(SetClipboardTextCallback))]
        public static unsafe void SetClipboardTextCallback(void* user_data, byte* text)
        {
            GUIUtility.systemCopyBuffer = StringFromPtr(text);
        }

        /// <summary>
        /// Runs the ime set input screen pos callback workflow.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        [MonoPInvokeCallback(typeof(ImeSetInputScreenPosCallback))]
        public static unsafe void ImeSetInputScreenPosCallback(int x, int y)
        {
            Input.compositionCursorPos = new Vector2(x, y);
        }

        /// <summary>
        /// Sets the clipboard functions.
        /// </summary>
        /// <param name="getCb">The get Cb value.</param>
        /// <param name="setCb">The set Cb value.</param>
        public static void SetClipboardFunctions(GetClipboardTextCallback getCb, SetClipboardTextCallback setCb)
        {
            _getClipboardText = getCb;
            _setClipboardText = setCb;
        }

        /// <summary>
        /// Runs the assign workflow.
        /// </summary>
        /// <param name="io">The io value.</param>
        public void Assign(ImGuiPlatformIOPtr io)
        {
            io.Platform_SetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(_setClipboardText);
            io.Platform_GetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(_getClipboardText);
        }

        /// <summary>
        /// Runs the unset workflow.
        /// </summary>
        /// <param name="io">The io value.</param>
        public void Unset(ImGuiPlatformIOPtr io)
        {
            io.Platform_SetClipboardTextFn = IntPtr.Zero;
            io.Platform_GetClipboardTextFn = IntPtr.Zero;
        }
        #endregion

        #region State
        public static GetClipboardTextSafeCallback GetClipboardText
        {
            // TODO: convert return string to Utf8 byte*
            set => _getClipboardText = (user_data) =>
            {
                try { return value(new IntPtr(user_data)); }
                catch (Exception ex) { Debug.LogException(ex); return null; }
            };
        }

        public static SetClipboardTextSafeCallback SetClipboardText
        {
            set => _setClipboardText = (user_data, text) =>
            {
                try { value(new IntPtr(user_data), StringFromPtr(text)); }
                catch (Exception ex) { Debug.LogException(ex); }
            };
        }
        #endregion

        #region Methods
        /// <summary>
        /// Returns the string from ptr result.
        /// </summary>
        /// <param name="ptr">The ptr value.</param>
        /// <returns>The result of the operation.</returns>
        internal static string StringFromPtr(byte* ptr)
        {
            int characters = 0;
            while (ptr[characters] != 0)
            {
                characters++;
            }

            return Encoding.UTF8.GetString(ptr, characters);
        }
        #endregion
    }
}
