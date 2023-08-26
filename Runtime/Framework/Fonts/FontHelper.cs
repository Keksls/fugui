using Aspose.Font.Glyphs;
using Aspose.Font.Sources;
using Aspose.Font.Ttf;
using Fu.Core;
using Fu.Core.DearImGui;
using ImGuiNET;
using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace Fu.Framework
{
    public class FontHelper : FuWindow
    {
        private Aspose.Font.Ttf.TtfFont _ttfFont;
        private GlyphId[] _glyphs;

        public FontHelper(FuWindowDefinition windowDefinition) : base(windowDefinition)
        {
            // prevent opening if font helper icons has not been imported
            if (!CanOpenFontHelper())
            {
                OnInitialized += (window) =>
                {
                    Close(null);
                };

                Debug.LogError("Can not open FontHelper because no font have been imported and ImportFontHelperIcons is set to false in FuSettings / FontConfig (see FuController in your unity scene).");
                return;
            }

            // set UI callback
            UI = DrawFontHelper;

            //// import font data
            //string fontPath = Path.Combine(Application.streamingAssetsPath, Fugui.Settings.FontConfig.FontsFolder);
            //fontPath = Path.Combine(fontPath, Fugui.Settings.FontConfig.FontHelperIcons.IconsFontName);

            //// load font definition
            //FontDefinition fd = new FontDefinition(Aspose.Font.FontType.TTF, new FontFileDefinition("ttf", new FileSystemStreamSource(fontPath)));
            //_ttfFont = Aspose.Font.Font.Open(fd) as TtfFont;

            //// get glyphs
            //_glyphs = _ttfFont.GetAllGlyphIds();
        }

        float cell_size = -1f;
        public unsafe void DrawFontHelper(FuWindow window)
        {
            IconConfig fontConf = Fugui.Settings.FontConfig.FontHelperIcons;
            ImFontPtr fontPtr = fontConf.FontPtr;
            fontPtr = ImGui.GetIO().Fonts.Fonts[ImGui.GetIO().Fonts.Fonts.Size - 1];

            ImDrawListPtr draw_list = ImGui.GetWindowDrawList();
            uint glyph_col = ImGui.GetColorU32(ImGuiCol.Text);
            cell_size = cell_size == -1f ? fontPtr.FontSize : cell_size;
            float cell_spacing = ImGui.GetStyle().ItemSpacing.y;
            Vector2 base_pos = ImGui.GetCursorScreenPos();

            using (FuLayout layout = new FuLayout())
            {
                layout.Slider("size", ref cell_size);
            }

            if (ImGui.BeginChild(""))
            {
                for (ushort i = fontConf.StartGlyph; i < fontConf.EndGlyph; i++)
                {
                    Vector2 p1 = ImGui.GetCursorScreenPos();
                    Vector2 p2 = new Vector2(p1.x + cell_size, p1.y + cell_size);

                    ImFontGlyph* glyph = fontPtr.FindGlyphNoFallback((char)(i)).NativePtr;
                    draw_list.AddRect(p1, p2, ImGui.GetColorU32((IntPtr)glyph != IntPtr.Zero ? new Color(255, 255, 255, 100) : new Color(255, 255, 255, 50)));
                    if ((IntPtr)glyph == IntPtr.Zero)
                        continue;

                    fontPtr.RenderChar(draw_list, cell_size, p1, glyph_col, (char)i);

                    if (ImGui.IsMouseHoveringRect(p1, p2))
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text("Codepoint: U+" + ((ushort)glyph->Codepoint).ToString("X"));
                        ImGui.Separator();
                        ImGui.Text("Visible: " + (glyph->Visible > 1 ? "YES" : "NO"));
                        ImGui.Text("AdvanceX: " + glyph->AdvanceX.ToString("f2"));
                        ImGui.Text("Pos: (" + glyph->X0.ToString("f2") + ", " + glyph->Y0.ToString("f2") + ")->(" + glyph->X1.ToString("f2") + ", " + glyph->Y1.ToString("f2") + ")");
                        ImGui.Text("Pos: (" + glyph->U0.ToString("f3") + ", " + glyph->V0.ToString("f3") + ")->(" + glyph->U1.ToString("f3") + ", " + glyph->V1.ToString("f3") + ")");
                        ImGui.EndTooltip();
                    }

                    if (ImGui.GetContentRegionAvail().x > (cell_size + cell_spacing) * 2f)
                    {
                        ImGui.Dummy(new Vector2(cell_size, cell_size));
                        ImGui.SameLine();
                    }
                    else
                    {
                        ImGui.NewLine();
                    }
                }

                ImGui.EndChild();
            }
        }

        public static bool CanOpenFontHelper()
        {
            return Fugui.Settings.FontConfig.ImportFontHelperIcons;
        }
    }
}