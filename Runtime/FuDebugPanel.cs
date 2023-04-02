// define it to debug whatever Color or Styles are pushed (avoid stack leak metrics)
// it's ressourcefull, si comment it when debug is done. Ensure it's commented before build.
//#define FUDEBUG 
using Fu.Framework;
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
        private static Stack<pushColorData> _colorStack;
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
            _colorStack = new Stack<pushColorData>();
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
            _colorStack.Clear();
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
            while (_colorStack.Count > 0)
            {
                colors.Add(_colorStack.Pop());
            }
            int nbStylesPush = _nbPushStyle;
            int nbStylesPop = _nbPopStyle;
            int nbColorsPush = _nbPushColor;
            int nbColorsPop = _nbPopColor;

            if (ImGui.Begin("Fugui Debug Tools"))
            {
                FuLayout layout = new FuLayout();
                using (FuPanel panel = new FuPanel("fuguiDebugPanel"))
                {
                    using (FuGrid grid = new FuGrid("fuDebugToolGrid"))
                    {
                        grid.Toggle("Show StackTraces", ref _showStackTraces);
                        grid.Toggle("Force Render All", ref _forceRenderAll);
                    }

                    layout.Collapsable("Colors##fuDebugColorsCol", () =>
                    {
                        using (FuGrid grid = new FuGrid("fuDebugColorGrid"))
                        {
                            grid.Text("Push");
                            grid.NextColumn();
                            PushFont(Core.FontType.Bold);
                            layout.Text(nbColorsPush.ToString());
                            PopFont();

                            grid.Text("Pop");
                            grid.NextColumn();
                            PushFont(Core.FontType.Bold);
                            layout.Text(nbColorsPop.ToString());
                            PopFont();
                        }

                        layout.Separator();
                        layout.SetNextElementToolTipWithLabel("The following must be empty.\n" +
                            "Note that the remaning push are just the last, it does not means that this is these items that must be pop.\n" +
                            "Please investigate to find witch ones are missing.");
                        PushFont(Core.FontType.Bold);
                        layout.Text("Remaning push in stack : " + colors.Count, colors.Count > 0 ? FuTextStyle.Danger : FuTextStyle.Default);
                        PopFont();

                        foreach (var colData in colors)
                        {
                            PushFont(Core.FontType.Bold);
                            layout.Text(colData.color.ToString());
                            PopFont();
                            if (_showStackTraces)
                            {
                                layout.Text(colData.stackTrace);
                            }
                            layout.Separator();
                        }

                        PushFont(Core.FontType.Bold);
                        layout.Text("Extra Pop : " + tooMutchColorPop.Count, tooMutchColorPop.Count > 0 ? FuTextStyle.Danger : FuTextStyle.Default);
                        PopFont();
                        foreach (var colData in tooMutchColorPop)
                        {
                            PushFont(Core.FontType.Bold);
                            layout.Text(colData.color.ToString());
                            PopFont();
                            if (_showStackTraces)
                            {
                                layout.Text(colData.stackTrace);
                            }
                            layout.Separator();
                        }
                    });

                    layout.Collapsable("Styles Var##fuDebugSVCol", () =>
                    {
                        using (FuGrid grid = new FuGrid("fuDebugSVGrid"))
                        {
                            grid.Text("Push");
                            grid.NextColumn();
                            PushFont(Core.FontType.Bold);
                            layout.Text(nbStylesPush.ToString());
                            PopFont();

                            grid.Text("Pop");
                            grid.NextColumn();
                            PushFont(Core.FontType.Bold);
                            layout.Text(nbStylesPop.ToString());
                            PopFont();
                        }

                        layout.Separator();
                        layout.SetNextElementToolTipWithLabel("The following must be empty.\n" +
                            "Note that the remaning push are just the last, it does not means that this is these items that must be pop.\n" +
                            "Please investigate to find witch ones are missing.");
                        PushFont(Core.FontType.Bold);
                        layout.Text("Remaning push in stack : " + styles.Count, styles.Count > 0 ? FuTextStyle.Danger : FuTextStyle.Default);
                        PopFont();

                        foreach (var svData in styles)
                        {
                            PushFont(Core.FontType.Bold);
                            layout.Text(svData.style.ToString());
                            PopFont();
                            if (_showStackTraces)
                            {
                                layout.Text(svData.stackTrace);
                            }
                            layout.Separator();
                        }

                        PushFont(Core.FontType.Bold);
                        layout.Text("Extra Pop : " + tooMutchStylePop.Count, tooMutchStylePop.Count > 0 ? FuTextStyle.Danger : FuTextStyle.Default);
                        PopFont();
                        foreach (var svData in tooMutchStylePop)
                        {
                            PushFont(Core.FontType.Bold);
                            layout.Text(svData.style.ToString());
                            PopFont();
                            if (_showStackTraces)
                            {
                                layout.Text(svData.stackTrace);
                            }
                            layout.Separator();
                        }
                    });
                }
                layout.Dispose();
                ImGui.End();
            }

            if(_forceRenderAll)
            {
                ForceDrawAllWindows();
            }
        }

        public static void Push(ImGuiCol imCol, Vector4 color)
        {
            ImGui.PushStyleColor(imCol, color);
            _colorStack.Push(new pushColorData()
            {
                color = imCol,
                stackTrace = Environment.StackTrace
            });
            _nbPushColor++;
        }
        public static void PopColor(int nb = 1)
        {
            for (int i = 0; i < nb; i++)
            {
                if (_colorStack.Count > 0)
                {
                    ImGui.PopStyleColor();
                    _nbPopColor++;
                    _colorStack.Pop();
                }
                else
                {
                    tooMutchColorPop.Add(new pushColorData()
                    {
                        color = ImGuiCol.COUNT,
                        stackTrace = Environment.StackTrace
                    });
                }
            }
        }

        public static void Push(ImGuiStyleVar imVar, Vector2 value)
        {
            ImGui.PushStyleVar(imVar, value);
            _stylesStack.Push(new pushStyleData()
            {
                style = imVar,
                stackTrace = Environment.StackTrace
            });
            _nbPushStyle++;
        }
        public static void Push(ImGuiStyleVar imVar, float value)
        {
            ImGui.PushStyleVar(imVar, value);
            _stylesStack.Push(new pushStyleData()
            {
                style = imVar,
                stackTrace = Environment.StackTrace
            });
            _nbPushStyle++;
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

    public struct pushStyleData
    {
        public ImGuiStyleVar style;
        public string stackTrace;

        public override string ToString()
        {
            return style.ToString() + Environment.NewLine + stackTrace;
        }
    }

    public struct pushColorData
    {
        public ImGuiCol color;
        public string stackTrace;

        public override string ToString()
        {
            return color.ToString() + Environment.NewLine + stackTrace;
        }
    }
#endif
}