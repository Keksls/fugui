using ImGuiNET;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Fu
{
    public static partial class Fugui
    {
        // Standard Drag and Drop payload types. You can define you own payload types using short strings. Types starting with '_' are defined by Dear ImGui.
        public static readonly string IMGUI_PAYLOAD_TYPE_COLOR_3F = "_COL3F"; // float[3]: Standard type for colors, without alpha. User code may use this type.
        public static readonly string IMGUI_PAYLOAD_TYPE_COLOR_4F = "_COL4F"; // float[4]: Standard type for colors. User code may use this type.
        private static Vector4 _colorpickerBackupColor;
        private static Vector4[] _colorpickerPalette;
        private static string _colorpickerCurrentID = string.Empty;

        /// <summary>
        /// Initialize the colorpicker palette
        /// </summary>
        private static void InitPalette()
        {
            if (_colorpickerPalette == null)
            {
                _colorpickerPalette = new Vector4[32];
                for (int n = 0; n < _colorpickerPalette.Length; n++)
                {
                    ImGui.ColorConvertHSVtoRGB(n / 31.0f, 0.8f, 0.8f, out _colorpickerPalette[n].x, out _colorpickerPalette[n].y, out _colorpickerPalette[n].z);
                    _colorpickerPalette[n].w = 1.0f; // Alpha
                }
            }
        }

        /// <summary>
        /// Draw a fully custom colorpicker component
        /// </summary>
        /// <param name="id">id of the colorpicker</param>
        /// <param name="color">color to edit</param>
        /// <param name="hdr">whatever the color is HDR</param>
        /// <param name="drag_and_drop">enable drag and drop on color button and fields</param>
        /// <param name="alpha_half_preview">color preview display alpha</param>
        /// <param name="alpha_preview">color preview alpha is display as half button color</param>
        /// <param name="options_menu">enable opetion menu flag</param>
        /// <returns>true if color value is updated this frame</returns>
        public static bool Colorpicker(string id, ref Vector4 color, bool hdr = false, bool drag_and_drop = true, bool alpha_half_preview = true, bool alpha_preview = true, bool options_menu = false)
        {
            if(_colorpickerCurrentID != id)
            {
                _colorpickerCurrentID = id;
                _colorpickerBackupColor = color;
            }

            ImGuiColorEditFlags misc_flags = ImGuiColorEditFlags.DefaultOptions | ImGuiColorEditFlags.DisplayHex | (alpha_preview ? ImGuiColorEditFlags.AlphaBar : ImGuiColorEditFlags.NoAlpha) | (hdr ? ImGuiColorEditFlags.HDR : 0) | (drag_and_drop ? 0 : ImGuiColorEditFlags.NoDragDrop) | (alpha_half_preview ? ImGuiColorEditFlags.AlphaPreviewHalf : (alpha_preview ? ImGuiColorEditFlags.AlphaPreview : 0)) | (options_menu ? 0 : ImGuiColorEditFlags.NoOptions);
            bool edited = ImGui.ColorPicker4(id, ref color, misc_flags | ImGuiColorEditFlags.NoSidePreview | ImGuiColorEditFlags.NoSmallPreview);
            ImGui.SameLine();
            ImGui.BeginGroup(); // Lock X position
            
            // current color
            ImGui.Spacing();
            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.Text("Current");
            ImGui.ColorButton("##current" + id, color, ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.AlphaPreviewHalf, new Vector2(60, 40) * CurrentContext.Scale);
            ImGui.EndGroup();
            
            // preview color
            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();
            ImGui.BeginGroup();
            ImGui.Text("Previous");
            if (ImGui.ColorButton("##previous" + id, _colorpickerBackupColor, ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.AlphaPreviewHalf, new Vector2(60, 40) * CurrentContext.Scale))
            {
                color = _colorpickerBackupColor;
            }
            ImGui.EndGroup();

            // palette
            ImGui.Separator();
            ImGui.Text("Palette");
            InitPalette();
            ImGui.Spacing();
            ImGui.BeginGroup();
            ImGuiColorEditFlags palette_button_flags = ImGuiColorEditFlags.NoAlpha | ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.NoTooltip;
            for (int n = 0; n < _colorpickerPalette.Length; n++)
            {
                ImGui.PushID(n);
                if ((n % 8) != 0)
                    ImGui.SameLine(0.0f, ImGui.GetStyle().ItemSpacing.y);
                if (ImGui.ColorButton("##cpBtnPlt" + n, _colorpickerPalette[n], palette_button_flags, new Vector2(20, 20) * CurrentContext.Scale))
                {
                    color = new Vector4(_colorpickerPalette[n].x, _colorpickerPalette[n].y, _colorpickerPalette[n].z, color.w); // Preserve alpha!
                    edited = true;
                }
                // Allow user to drop colors into each palette entry. Note that ColorButton() is already a
                // drag source by default, unless specifying the ImGuiColorEditFlags_NoDragDrop flag.
                if (ImGui.BeginDragDropTarget())
                {
                    unsafe
                    {
                        ImGuiPayloadPtr payload = null;
                        if ((payload = ImGui.AcceptDragDropPayload(IMGUI_PAYLOAD_TYPE_COLOR_3F)).NativePtr != null)
                        {
                            _colorpickerPalette[n] = Marshal.PtrToStructure<Vector3>(payload.Data);
                        }
                        if ((payload = ImGui.AcceptDragDropPayload(IMGUI_PAYLOAD_TYPE_COLOR_4F)).NativePtr != null)
                        {
                            _colorpickerPalette[n] = Marshal.PtrToStructure<Vector4>(payload.Data);
                        }
                    }
                }
                ImGui.EndDragDropTarget();
                ImGui.PopID();
            }
            ImGui.EndGroup();
            ImGui.EndGroup();
            return edited;
        }
    }
}