
using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
#if FUDEBUG
    public static partial class Fugui
    {
        private static int _nbPushStyle = 0;
        private static int _nbPushColor = 0;
        private static int _nbPopStyle = 0;
        private static int _nbPopColor = 0;
        private static Stack<pushStyleData> _stylesStack;
        private static Stack<pushColorData> _debugColorStack;
        private static List<pushStyleData> tooMutchStylePop = new List<pushStyleData>();
        private static List<pushColorData> tooMutchColorPop = new List<pushColorData>();
        private static bool _showStackTraces;
        private static bool _forceRenderAll;

        private static void initDebugTool()
        {
            _nbPushStyle = 0;
            _nbPushColor = 0;
            _nbPopStyle = 0;
            _nbPopColor = 0;
            _stylesStack = new Stack<pushStyleData>();
            _debugColorStack = new Stack<pushColorData>();
            DefaultContext.OnLastRender += DefaultContext_OnLastRender;
        }

        private static void DefaultContext_OnLastRender()
        {
            drawDebugUI();
        }

        private static void newFrame()
        {
            _nbPushStyle = 0;
            _nbPushColor = 0;
            _nbPopStyle = 0;
            _nbPopColor = 0;
            _stylesStack.Clear();
            _debugColorStack.Clear();
            tooMutchStylePop.Clear();
            tooMutchColorPop.Clear();
        }

        private static void drawDebugUI()
        {
            List<pushStyleData> styles = new List<pushStyleData>();
            while (_stylesStack.Count > 0)
            {
                styles.Add(_stylesStack.Pop());
            }
            List<pushColorData> colors = new List<pushColorData>();
            while (_debugColorStack.Count > 0)
            {
                colors.Add(_debugColorStack.Pop());
            }
            int nbStylesPush = _nbPushStyle;
            int nbStylesPop = _nbPopStyle;
            int nbColorsPush = _nbPushColor;
            int nbColorsPop = _nbPopColor;

            if (ImGui.Begin("Fugui Debug Tools"))
            {
                // The debug panel uses the same session layout as every other root surface.
                Layout.ResetSurfaceState();
                using (FuPanel panel = new FuPanel("fuguiDebugPanel"))
                {
                    using (FuGrid grid = new FuGrid("fuDebugToolGrid"))
                    {
                        grid.Toggle("Show StackTraces", ref _showStackTraces);
                        grid.Toggle("Force Render All", ref _forceRenderAll);
                    }

                    Layout.Collapsable("Colors##fuDebugColorsCol", () =>
                    {
                        using (FuGrid grid = new FuGrid("fuDebugColorGrid"))
                        {
                            grid.Text("Push");
                            grid.NextColumn();
                            PushFont(Core.FontType.Bold);
                            Layout.Text(nbColorsPush.ToString());
                            PopFont();

                            grid.Text("Pop");
                            grid.NextColumn();
                            PushFont(Core.FontType.Bold);
                            Layout.Text(nbColorsPop.ToString());
                            PopFont();
                        }

                        Layout.Separator();
                        Layout.SetNextElementToolTipWithLabel("The following must be empty.\n" +
                            "Note that the remaning push are just the last, it does not means that this is these items that must be pop.\n" +
                            "Please investigate to find witch ones are missing.");
                        PushFont(Core.FontType.Bold);
                        Layout.Text("Remaning push in stack : " + colors.Count, colors.Count > 0 ? FuTextStyle.Danger : FuTextStyle.Default);
                        PopFont();

                        foreach (var colData in colors)
                        {
                            PushFont(Core.FontType.Bold);
                            Layout.Text(colData.color.ToString());
                            PopFont();
                            if (_showStackTraces)
                            {
                                Layout.Text(colData.stackTrace);
                            }
                            Layout.Separator();
                        }

                        PushFont(Core.FontType.Bold);
                        Layout.Text("Extra Pop : " + tooMutchColorPop.Count, tooMutchColorPop.Count > 0 ? FuTextStyle.Danger : FuTextStyle.Default);
                        PopFont();
                        foreach (var colData in tooMutchColorPop)
                        {
                            PushFont(Core.FontType.Bold);
                            Layout.Text(colData.color.ToString());
                            PopFont();
                            if (_showStackTraces)
                            {
                                Layout.Text(colData.stackTrace);
                            }
                            Layout.Separator();
                        }
                    });

                    Layout.Collapsable("Styles Var##fuDebugSVCol", () =>
                    {
                        using (FuGrid grid = new FuGrid("fuDebugSVGrid"))
                        {
                            grid.Text("Push");
                            grid.NextColumn();
                            PushFont(Core.FontType.Bold);
                            Layout.Text(nbStylesPush.ToString());
                            PopFont();

                            grid.Text("Pop");
                            grid.NextColumn();
                            PushFont(Core.FontType.Bold);
                            Layout.Text(nbStylesPop.ToString());
                            PopFont();
                        }

                        Layout.Separator();
                        Layout.SetNextElementToolTipWithLabel("The following must be empty.\n" +
                            "Note that the remaning push are just the last, it does not means that this is these items that must be pop.\n" +
                            "Please investigate to find witch ones are missing.");
                        PushFont(Core.FontType.Bold);
                        Layout.Text("Remaning push in stack : " + styles.Count, styles.Count > 0 ? FuTextStyle.Danger : FuTextStyle.Default);
                        PopFont();

                        foreach (var svData in styles)
                        {
                            PushFont(Core.FontType.Bold);
                            Layout.Text(svData.style.ToString());
                            PopFont();
                            if (_showStackTraces)
                            {
                                Layout.Text(svData.stackTrace);
                            }
                            Layout.Separator();
                        }

                        PushFont(Core.FontType.Bold);
                        Layout.Text("Extra Pop : " + tooMutchStylePop.Count, tooMutchStylePop.Count > 0 ? FuTextStyle.Danger : FuTextStyle.Default);
                        PopFont();
                        foreach (var svData in tooMutchStylePop)
                        {
                            PushFont(Core.FontType.Bold);
                            Layout.Text(svData.style.ToString());
                            PopFont();
                            if (_showStackTraces)
                            {
                                Layout.Text(svData.stackTrace);
                            }
                            Layout.Separator();
                        }
                    });
                }
                ImGui.End();
            }

            if(_forceRenderAll)
            {
                ForceDrawAllWindows();
            }
        }

        public static void Push(FuColors styleColor, Vector4 color)
        {
            Push((int)styleColor, color);
        }

        public static void Push(int colorIndex, Vector4 color)
        {
            if (!PushColorValue(colorIndex, color))
            {
                return;
            }

            _debugColorStack.Push(new pushColorData()
            {
                color = colorIndex,
                stackTrace = Environment.StackTrace
            });
            _nbPushColor++;
        }

        public static void Push(Enum color, Vector4 value)
        {
            Push(ResolveColorIndex(color), value);
        }

        internal static void Push(ImGuiCol imCol, Vector4 color)
        {
            if (!PushColorValue((int)imCol, color))
            {
                return;
            }

            _debugColorStack.Push(new pushColorData()
            {
                color = (int)imCol,
                stackTrace = Environment.StackTrace
            });
            _nbPushColor++;
        }
        public static void PopColor(int nb = 1)
        {
            for (int i = 0; i < nb; i++)
            {
                if (PopColorValue(out _))
                {
                    _nbPopColor++;
                    if (_debugColorStack.Count > 0)
                    {
                        _debugColorStack.Pop();
                    }
                    else
                    {
                        tooMutchColorPop.Add(new pushColorData()
                        {
                            color = (int)ImGuiCol.COUNT,
                            stackTrace = Environment.StackTrace
                        });
                    }
                }
                else
                {
                    tooMutchColorPop.Add(new pushColorData()
                    {
                        color = (int)ImGuiCol.COUNT,
                        stackTrace = Environment.StackTrace
                    });
                }
            }
        }

        public static void Push(FuStyleVar styleVar, Vector2 value)
        {
            Push((ImGuiStyleVar)styleVar, value);
        }

        internal static void Push(ImGuiStyleVar imVar, Vector2 value)
        {
            ImGui.PushStyleVar(imVar, value);
            _stylesStack.Push(new pushStyleData()
            {
                style = imVar,
                stackTrace = Environment.StackTrace
            });
            _nbPushStyle++;
            NbPushStyle++;
        }
        public static void Push(FuStyleVar styleVar, float value)
        {
            Push((ImGuiStyleVar)styleVar, value);
        }

        internal static void Push(ImGuiStyleVar imVar, float value)
        {
            ImGui.PushStyleVar(imVar, value);
            _stylesStack.Push(new pushStyleData()
            {
                style = imVar,
                stackTrace = Environment.StackTrace
            });
            _nbPushStyle++;
            NbPushStyle++;
        }
        public static void PopStyle(int nb = 1)
        {
            for (int i = 0; i < nb; i++)
            {
                if (_stylesStack.Count > 0)
                {
                    ImGui.PopStyleVar();
                    _nbPopStyle++;
                    _stylesStack.Pop();
                    NbPushStyle--;
                }
                else
                {
                    tooMutchStylePop.Add(new pushStyleData()
                    {
                        style = ImGuiStyleVar.COUNT,
                        stackTrace = Environment.StackTrace
                    });
                }
            }
        }
    }

    internal struct pushStyleData
    {
        internal ImGuiStyleVar style;
        internal string stackTrace;

        public override string ToString()
        {
            return style.ToString() + Environment.NewLine + stackTrace;
        }
    }

    internal struct pushColorData
    {
        internal int color;
        internal string stackTrace;

        public override string ToString()
        {
            return color.ToString() + Environment.NewLine + stackTrace;
        }
    }
#endif
}
