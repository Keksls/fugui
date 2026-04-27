using System.Runtime.InteropServices;

namespace ImGuiNET
{
    /// <summary>
    /// Defines the Im Gui Input Text Callback callback signature.
    /// </summary>
    /// <param name="data">The data value.</param>
    /// <returns>The result of the operation.</returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate int ImGuiInputTextCallback(ImGuiInputTextCallbackData* data);
}