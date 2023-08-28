using Aspose.Font.Sources;
using Aspose.Font.Ttf;
using Aspose.Font.TtfHelpers;
using Fu.Core;
using Fu.Core.DearImGui;
using ImGuiNET;
using SharpFont;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Fu.Framework
{
    public class FontHelper : FuWindow
    {
        Face _font;
        FuGlyph[] _glyphs;
        FuGlyph _editingGlyph = null;
        List<FuGlyph> _selectedGlyphs = new List<FuGlyph>();
        private string _newFontName = "Fugui Icons";
        private string _newFontPath = Application.streamingAssetsPath + @"/Fugui/Fonts";
        private const ushort _duotoneIconGlyphsStart = 60644;
        private const ushort _regularIconGlyphsStart = 57445;
        private float _cellSize = -1f;
        private int _duotoneBaseOffset = 0;

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

            // import font data
            string fontPath = Path.Combine(Application.streamingAssetsPath, Fugui.Settings.FontConfig.FontsFolder);
            fontPath = Path.Combine(fontPath, Fugui.Settings.FontConfig.FontHelperIcons.IconsFontName);

            // load glyphs and bind FuGlyph from font glyphs data
            _font = new Face(new Library(), fontPath);
            uint charIndex = _font.GetFirstChar(out uint glyphIndex);
            _glyphs = new FuGlyph[_font.GlyphCount];
            int i = 0;
            _glyphs[i] = new FuGlyph()
            {
                BaseChar = (char)charIndex,
                BaseGlyphIndex = glyphIndex,
                Name = "Icon " + charIndex.ToString("X2")
            };
            for (; i < _font.GlyphCount; i++)
            {
                charIndex = _font.GetNextChar(charIndex, out glyphIndex);
                _glyphs[i] = new FuGlyph()
                {
                    BaseChar = (char)charIndex,
                    BaseGlyphIndex = glyphIndex,
                    Name = "Icon " + charIndex.ToString("X2")
                };
            }
        }

        public unsafe void DrawFontHelper(FuWindow window)
        {
            IconConfig fontConf = Fugui.Settings.FontConfig.FontHelperIcons;
            ImFontPtr fontPtr = fontConf.FontPtr;
            fontPtr = ImGui.GetIO().Fonts.Fonts[ImGui.GetIO().Fonts.Fonts.Size - 1];

            ImDrawListPtr draw_list = ImGui.GetWindowDrawList();
            uint glyph_col = ImGui.GetColorU32(ImGuiCol.Text);
            _cellSize = _cellSize == -1f ? fontPtr.FontSize : _cellSize;
            float cell_spacing = ImGui.GetStyle().ItemSpacing.y;
            Vector2 base_pos = ImGui.GetCursorScreenPos();

            using (FuLayout layout = new FuLayout())
            {
                if (ImGui.BeginChild("fntHlprGlphCntnr", new Vector2(ImGui.GetContentRegionAvail().x - 320f, -1f)))
                {
                    for (int i = 0; i < _glyphs.Length; i++)
                    {
                        // get current FuGlyph data
                        FuGlyph fuGlyph = _glyphs[i];

                        Vector2 p1 = ImGui.GetCursorScreenPos();
                        Vector2 p2 = new Vector2(p1.x + _cellSize, p1.y + _cellSize);
                        bool hovered = layout.IsItemHovered(p1, new Vector2(_cellSize, _cellSize));

                        ImFontGlyph* glyph = fontPtr.FindGlyphNoFallback(fuGlyph.BaseChar).NativePtr;

                        Color color = FuThemeManager.GetColor(FuColors.FrameBg);
                        if (_selectedGlyphs.Contains(fuGlyph))
                            color = FuThemeManager.GetColor(FuColors.Highlight);

                        draw_list.AddRectFilled(p1, p2, ImGui.GetColorU32(color));

                        if (hovered)
                        {
                            draw_list.AddRect(p1, p2, ImGui.GetColorU32(FuThemeManager.GetColor(FuColors.FrameHoverFeedback)));
                            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                        }

                        if ((IntPtr)glyph == IntPtr.Zero)
                            continue;

                        fontPtr.RenderChar(draw_list, _cellSize, p1, glyph_col, fuGlyph.BaseChar);

                        if (hovered)
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text("Codepoint: U+" + Convert.ToUInt16(fuGlyph.BaseChar).ToString("X2"));
                            ImGui.Text(_font.GetGlyphName(fuGlyph.BaseGlyphIndex, 1024));
                            ImGui.Separator();
                            //ImGui.Text("AdvanceX: " + ((float)_font.GetAdvance(_charIndices[i], LoadFlags.Default)).ToString("f2"));
                            ImGui.Text("Pos: (" + glyph->X0.ToString("f2") + ", " + glyph->Y0.ToString("f2") + ")->(" + glyph->X1.ToString("f2") + ", " + glyph->Y1.ToString("f2") + ")");
                            ImGui.Text("Pos: (" + glyph->U0.ToString("f3") + ", " + glyph->V0.ToString("f3") + ")->(" + glyph->U1.ToString("f3") + ", " + glyph->V1.ToString("f3") + ")");
                            ImGui.EndTooltip();

                            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                            {
                                if (_selectedGlyphs.Contains(fuGlyph))
                                    _selectedGlyphs.Remove(fuGlyph);
                                else
                                    _selectedGlyphs.Add(fuGlyph);
                            }
                        }

                        if (ImGui.GetContentRegionAvail().x > (_cellSize + cell_spacing) * 2f)
                        {
                            ImGui.Dummy(new Vector2(_cellSize, _cellSize));
                            ImGui.SameLine();
                        }
                        else
                        {
                            ImGui.NewLine();
                        }
                    }
                    ImGui.EndChild();
                }

                ImGui.SameLine();

                if(ImGui.BeginChild("fntHlprNwFntCntnr"))
                {
                    using (FuGrid grid = new FuGrid("fntHlprNwFntGrd"))
                    {
                        int cs = (int)_cellSize;
                        if (grid.Slider("size", ref cs, 10, 96))
                        {
                            _cellSize = cs;
                        }
                        grid.TextInput("Font Name", ref _newFontName);
                        grid.InputFolder("Export Path", (path) =>
                        {
                            _newFontPath = path;
                        }, _newFontPath);
                        grid.Slider("Duotone offset", ref _duotoneBaseOffset, 0, 9999);
                    }

                    if (layout.Button("Export Font", FuButtonStyle.Highlight))
                    {
                        ExportFont();
                    }

                    ImGui.Separator();

                    foreach (FuGlyph fuGlyph in _selectedGlyphs)
                    {
                        char g = fuGlyph.BaseChar;
                        Vector2 p1 = ImGui.GetCursorScreenPos();
                        Vector2 size = new Vector2(_cellSize, _cellSize);
                        Vector2 p2 = p1 + size;
                        bool hovered = layout.IsItemHovered(p1, size);

                        ImFontGlyph* glyph = fontPtr.FindGlyphNoFallback(g).NativePtr;

                        Color color = FuThemeManager.GetColor(FuColors.FrameBg);
                        if (_selectedGlyphs.Contains(fuGlyph))
                            color = FuThemeManager.GetColor(FuColors.Highlight);

                        draw_list.AddRectFilled(p1, p2, ImGui.GetColorU32(color));

                        if (hovered)
                        {
                            draw_list.AddRect(p1, p2, ImGui.GetColorU32(FuThemeManager.GetColor(FuColors.FrameHoverFeedback)));
                            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                            if (Mouse.IsDown(FuMouseButton.Right))
                            {
                                _selectedGlyphs.Remove(fuGlyph);
                            }
                            else if (Mouse.IsDown(FuMouseButton.Left))
                            {
                                _editingGlyph = fuGlyph;
                                Fugui.ShowModal("Edit glyph", () =>
                                {
                                    using (FuLayout layout = new FuLayout())
                                    {
                                        string gName = fuGlyph.Name;
                                        if (layout.TextInput("GlyphName", ref gName))
                                        {
                                            fuGlyph.Name = gName;
                                        }
                                    }
                                }, FuModalSize.Medium, new FuModalButton("OK", null, FuButtonStyle.Default));
                            }
                        }

                        if ((IntPtr)glyph == IntPtr.Zero)
                            continue;

                        fontPtr.RenderChar(draw_list, _cellSize, p1, glyph_col, g);

                        if (ImGui.GetContentRegionAvail().x > (_cellSize + cell_spacing) * 2f)
                        {
                            ImGui.Dummy(new Vector2(_cellSize, _cellSize));
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
        }

        private void ExportFont()
        {
            // prevent to export less than 4 glyphs
            if (_selectedGlyphs.Count < 4)
            {
                Fugui.ShowModal("Fail to export font", () =>
                {
                    using (FuLayout layout = new FuLayout())
                    {
                        layout.Text("Fugui had hard time creating font.\n" +
                            "It's quite a hacky trick to create a font from scratch.\n" +
                            "To be sure the generated font is not broken, we need to know a quite long list of glyphs to place theme at right fugui glyph range\n" +
                            "Please select at least 4 glyphs to export a font.");
                    }
                }, FuModalSize.Medium, new FuModalButton("Ok", () => { }, FuButtonStyle.Default));
                return;
            }

            // sort selected Glyphs by base char
            _selectedGlyphs = _selectedGlyphs.OrderBy(g => g.BaseChar).ToList();

            // get base font path
            string fontPath = Path.Combine(Application.streamingAssetsPath, Fugui.Settings.FontConfig.FontsFolder);
            fontPath = Path.Combine(fontPath, Fugui.Settings.FontConfig.FontHelperIcons.IconsFontName);

            // load base font
            FontDefinition fd = new FontDefinition(Aspose.Font.FontType.TTF, new FontFileDefinition("ttf", new FileSystemStreamSource(fontPath)));
            TtfFont baseFont = Aspose.Font.Font.Open(fd) as TtfFont;

            // To create a font we use functionality of the IFontCharactersMerger interface.
            IFontCharactersMerger merger = HelpersFactory.GetFontCharactersMerger(baseFont, baseFont);

            // create merged font
            TtfFont destFont = merger.MergeFonts(_selectedGlyphs.Select(g => (uint)g.BaseChar).ToArray(), new uint[0], "Montserrat");

            // Save resultant font
            string filePath = _newFontPath + @"\" + _newFontName + ".ttf";
            destFont.Save(filePath);

            // Reload font from file
            fd = new FontDefinition(Aspose.Font.FontType.TTF, new FontFileDefinition("ttf", new FileSystemStreamSource(filePath)));
            destFont = Aspose.Font.Font.Open(fd) as TtfFont;

            // get cmap table offset and size
            int size = _selectedGlyphs.Count * sizeof(char);

            // read file as byte array
            byte[] fileBytes = File.ReadAllBytes(filePath);

            // get base cmap table as byte array
            byte[] baseCMap = new byte[size];
            Buffer.BlockCopy(_selectedGlyphs.Select(c => (ushort)c.BaseChar).ToArray(), 0, baseCMap, 0, baseCMap.Length);

            // revert endian bytes
            baseCMap = ReverseEndian(baseCMap);

            // sort FuGlyphs
            SortGlyphs();

            // create converted CMap table
            byte[] sortedCMap = new byte[size];
            Buffer.BlockCopy(_selectedGlyphs.Select(c => (ushort)c.NewChar).ToArray(), 0, sortedCMap, 0, sortedCMap.Length);

            // revert endian bytes
            sortedCMap = ReverseEndian(sortedCMap);

            // replace base cmap table with sorted one
            fileBytes = Replace(fileBytes, baseCMap, sortedCMap);

            // save file
            File.WriteAllBytes(filePath, fileBytes);
        }

        /// <summary>
        /// Switch a byte array's endian encoding
        /// </summary>
        /// <param name="input">array to convert</param>
        /// <returns>converted array</returns>
        public byte[] ReverseEndian(byte[] input)
        {
            byte[] endianedResult = new byte[input.Length];
            for (int i = 0; i < input.Length; i += 2)
            {
                endianedResult[i] = input[i + 1];
                endianedResult[i + 1] = input[i];
            }
            return endianedResult;
        }

        /// <summary>
        /// Replace a byte array by another one inside a byte array
        /// </summary>
        /// <param name="input">full byte array</param>
        /// <param name="pattern">patern to replace</param>
        /// <param name="replacement">replacement array</param>
        /// <returns>replaced array</returns>
        public byte[] Replace(byte[] input, byte[] pattern, byte[] replacement)
        {
            if (pattern.Length == 0)
            {
                return input;
            }

            List<byte> result = new List<byte>();

            int i;

            for (i = 0; i <= input.Length - pattern.Length; i++)
            {
                bool foundMatch = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (input[i + j] != pattern[j])
                    {
                        foundMatch = false;
                        break;
                    }
                }

                if (foundMatch)
                {
                    result.AddRange(replacement);
                    i += pattern.Length - 1;
                }
                else
                {
                    result.Add(input[i]);
                }
            }

            for (; i < input.Length; i++)
            {
                result.Add(input[i]);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Whatever we can open this window
        /// </summary>
        /// <returns>true if fuController settings has FontConfig/IconFontHelper setted</returns>
        public static bool CanOpenFontHelper()
        {
            return Fugui.Settings.FontConfig.ImportFontHelperIcons;
        }

        /// <summary>
        /// Sort glyphs to stack theme at minimum location whatever it's duotone or regular
        /// </summary>
        public void SortGlyphs()
        {
            char regularChar = (char)_regularIconGlyphsStart;
            char duotoneChar = (char)_duotoneIconGlyphsStart;
            foreach (var g in _selectedGlyphs)
            {
                if (g.DuotoneBaseOffset > 0)
                {
                    g.NewChar = duotoneChar++;
                    g.NewDuotoneChar = duotoneChar++;
                }
                else
                {
                    g.NewChar = regularChar++;
                }
            }
        }

        /// <summary>
        /// Class that represent a FuGlyph
        /// </summary>
        public class FuGlyph
        {
            /// <summary>
            /// Char from the base font
            /// </summary>
            public char BaseChar;
            /// <summary>
            /// Duotone secondary Char from the base font
            /// </summary>
            public char BaseDuotoneChar;
            /// <summary>
            /// Char to the generated Fugui font
            /// </summary>
            public char NewChar;
            /// <summary>
            /// Duotone secondary Char to the generated Fugui font
            /// </summary>
            public char NewDuotoneChar;
            /// <summary>
            /// Whatever the glyph is a duotone glyph
            /// </summary>
            public int DuotoneBaseOffset;
            /// <summary>
            /// Name of the glyph
            /// </summary>
            public string Name;
            /// <summary>
            /// Index of the glyph into base font
            /// </summary>
            public uint BaseGlyphIndex;

            public override int GetHashCode()
            {
                return BaseChar;
            }
            public override bool Equals(object obj)
            {
                return Equals(obj as FuGlyph);
            }

            public bool Equals(FuGlyph obj)
            {
                return obj != null && obj.BaseChar == this.BaseChar;
            }
        }
    }
}