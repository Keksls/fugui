using UnityEngine;
using Fu.Core;
using Fu.Framework;
using System.Collections.Generic;
using System;
using Fu;
using System.Linq;
using ImGuiNET;

/// <summary>
/// this sample show how to use Fugui API
/// </summary>
public class FuguiDemoScene : MonoBehaviour
{
    public Camera cam1;
    public Texture2D TmpDebugTexture;
    public bool ShowImGuiDemoWindow = false;
    public bool ShowRaycastersDebug = false;
    public Test3DRaycaster Raycaster;
    private FuCameraWindow _mainCam;

    // tree
    private class treeTestItem
    {
        public string Text;
        public byte IsOpen = 0;
        public int Level = 0;
        public treeTestItem Parent;
        public List<treeTestItem> Children;

        public treeTestItem(string text, int level, List<treeTestItem> children = null)
        {
            Level = level;
            Text = text;
            Children = children;
        }

        public void AddChild(treeTestItem child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        public static List<treeTestItem> GetRandomHierarchie()
        {
            List<treeTestItem> items = new List<treeTestItem>();

            for (int i = 0; i < 10; i++)
            {
                treeTestItem parent = new treeTestItem("Parent " + i, 0, new List<treeTestItem>());
                for (int j = 0; j < 10; j++)
                {
                    treeTestItem child = new treeTestItem("Child " + j, 1, new List<treeTestItem>());
                    for (int k = 0; k < 5; k++)
                    {
                        treeTestItem child2 = new treeTestItem("Child 2_" + k, 2);
                        child.AddChild(child2);
                    }
                    parent.AddChild(child);
                }
                items.Add(parent);
            }
            return items;
        }
    }

    void Start()
    {
        IFuWindowContainer mainContainer = Fugui.MainContainer;

        // add debug window
        bool toggleVal = false;
        bool boolVal = false;
        float min = 10f;
        float max = 20;
        int intVal = 5;
        float floatVal = 5f;
        Vector2 v2Val = Vector2.zero;
        Vector3 v3Val = Vector3.zero;
        Vector4 v4Val = Vector4.zero;
        List<string> cbTexts = new List<string>() { "cb1", "cb2", "cb3" };
        List<IFuSelectable> cbButtons = new List<IFuSelectable>() {
            new FuSelectable_Button("button 1"),
            new FuSelectable_Button("button 2", FuElementSize.AutoSize),
            new FuSelectable_SameLine(),
            new FuSelectable_Button("button 3", false),
            new FuSelectable_Separator(),
            new FuSelectable_Button("button 4", new Vector2(32f, 32f)),
            new FuSelectable_SameLine(),
            new FuSelectable_Button("button 5"),
        };
        Vector3 pos = Vector3.zero;
        Vector3 rot = Vector3.zero;
        Vector3 scale = Vector3.zero;
        Vector4 color = new Vector4(.5f, 1f, .8f, .7f);
        Vector3 coloralphaless = new Vector3(.5f, 1f, .8f);
        bool physicalCamera = false;
        string title = "";
        string description = "A <color=red>red</color> <b>Bold TEXT</b>";

        // set demo Main menu
        Fugui.RegisterMainMenuItem(Icons.DragonflyLogo + " Files", null);

        FuDockingLayoutManager.OnDockLayoutReloaded += DockingLayoutManager_OnDockLayoutReloaded;
        FuDockingLayoutManager.OnDockLayoutInitialized += DockingLayoutManager_OnDockLayoutInitialized;

        Fugui.RegisterMainMenuItem("Layout", null);
        foreach (KeyValuePair<string, FuDockSpaceDefinition> layoutDefinition in FuDockingLayoutManager.Layouts)
        {
            string menuName = Fugui.AddSpacesBeforeUppercase(layoutDefinition.Key);
            if (!Fugui.IsMainMenuRegisteredItem(menuName))
            {
                Fugui.RegisterMainMenuItem(menuName, () => FuDockingLayoutManager.SetLayout(layoutDefinition.Value), "Layout");
            }
        }

        Fugui.RegisterMainMenuItem("Windows", null);
        foreach (FuWindowsNames windowName in Enum.GetValues(typeof(FuWindowsNames)))
        {
            if (windowName == FuWindowsNames.None)
            {
                continue;
            }
            Fugui.RegisterMainMenuItem(windowName.ToString(), () => Fugui.CreateWindowAsync(windowName, null), "Windows");
        }

        Fugui.RegisterMainMenuItem("3D Windows", null);
        foreach (FuWindowsNames windowName in Enum.GetValues(typeof(FuWindowsNames)))
        {
            if (windowName == FuWindowsNames.None)
            {
                continue;
            }
            Fugui.RegisterMainMenuItem("3D " + windowName.ToString(), () => Fugui.CreateWindowAsync(windowName, (window) => { Fugui.Add3DWindow(window, new Vector3(0f, -2f, 0f), Quaternion.Euler(Vector3.up * 180f)); }, false), "3D Windows");
        }

        new FuWindowDefinition(FuWindowsNames.DockSpaceManager, "DockSpace Manager", (window) => Fugui.DrawDockSpaceManager());
        new FuWindowDefinition(FuWindowsNames.WindowsDefinitionManager, "Windows Definition Manager", (window) => Fugui.DrawWindowsDefinitionManager());

        new FuWindowDefinition(FuWindowsNames.ToolBox, "Tool Box", debugWindow_UI, flags: FuWindowFlags.AllowMultipleWindow);
        void debugWindow_UI(FuWindow window)
        {
            using (new FuPanel("debugContainer"))
            {
                using (FuGrid grid = new FuGrid("debugGrid"))
                {
                    grid.Text("Local position");
                    grid.Text(window.LocalPosition.ToString());

                    grid.Text("Local Rect");
                    grid.Text(window.LocalRect.ToString());

                    grid.Separator();

                    grid.Text("World position");
                    grid.Text(window.WorldPosition.ToString());

                    grid.Text("World Rect");
                    grid.Text(window.WorldRect.ToString());

                    grid.Separator();
                    if (window.Container != null)
                    {
                        grid.Text("Container Pos");
                        grid.Text(window.Container.Position.ToString());

                        grid.Text("Container Size");
                        grid.Text(window.Container.Size.ToString());

                        grid.Separator();

                        grid.Text("FPS");
                        grid.Text(window.CurrentFPS.ToString("f2"));

                        grid.Text("State");
                        grid.Text(window.State.ToString());

                        bool chkVal = window.IsDragging;
                        grid.DisableNextElement();
                        grid.CheckBox("Dragging", ref chkVal);

                        chkVal = window.IsResizing;
                        grid.DisableNextElement();
                        grid.CheckBox("Resizing", ref chkVal);
                    }

                    grid.Separator();

                    if (grid.Button("Mouse Pos and Down Text"))
                    {
                        Debug.Log("click !");
                    }
                    grid.DisableNextElement();
                    grid.Button("Mouse Pos and Down Text", FuButtonStyle.Highlight);
                }
                window.Container.ImGuiImage(TmpDebugTexture, new Vector2(128, 128));
            }
        }

        // add Modals Window
        new FuWindowDefinition(FuWindowsNames.Modals, "Modals Demo", (window) =>
            {
                using (FuLayout layout = new FuLayout())
                {
                    if (layout.Button("Theme small"))
                    {
                        Fugui.ShowModal("Theme Manager", Fugui.DrawThemes, FuModalSize.Small);
                    }

                    if (layout.Button("Theme medium"))
                    {
                        Fugui.ShowModal("Theme Manager", Fugui.DrawThemes, FuModalSize.Medium);
                    }

                    if (layout.Button("Theme large"))
                    {
                        Fugui.ShowModal("Theme Manager", Fugui.DrawThemes, FuModalSize.Large);
                    }

                    if (layout.Button("Theme extra larger"))
                    {
                        Fugui.ShowModal("Theme Manager", Fugui.DrawThemes, FuModalSize.ExtraLarge);
                    }

                    layout.SetNextElementToolTip("Info style tooltip", "Success style tooltip", "Warning style tooltip", "Danger style tooltip");
                    layout.SetNextElementToolTipStyles(FuTextStyle.Info, FuTextStyle.Success, FuTextStyle.Warning, FuTextStyle.Danger);
                    if (layout.Button("Info modal", FuButtonStyle.Info))
                    {
                        Fugui.ShowInfo("This is an Information", () =>
                        {
                            using (FuLayout layout = new FuLayout())
                            {
                                layout.Text("This is a nomal text");
                                layout.Text("This is an info text", FuTextStyle.Info);
                            }
                        }, FuModalSize.Medium);
                    }

                    if (layout.Button("Success modal", FuButtonStyle.Success))
                    {
                        Fugui.ShowSuccess("This is a Success", () =>
                        {
                            using (FuLayout layout = new FuLayout())
                            {
                                layout.Text("This is a nomal text");
                                layout.Text("This is a success text", FuTextStyle.Success);
                            }
                        }, FuModalSize.Medium);
                    }

                    if (layout.Button("Warning modal", FuButtonStyle.Warning))
                    {
                        Fugui.ShowWarning("This is a Warning", () =>
                        {
                            using (FuLayout layout = new FuLayout())
                            {
                                layout.Text("This is a nomal text");
                                layout.Text("This is a warning text", FuTextStyle.Warning);
                            }
                        }, FuModalSize.Medium);
                    }

                    if (layout.Button("Danger modal", FuButtonStyle.Danger))
                    {
                        Fugui.ShowDanger("This is a Danger", () =>
                        {
                            using (FuLayout layout = new FuLayout())
                            {
                                layout.Text("This is a nomal text");
                                layout.Text("This is a danger text", FuTextStyle.Danger);
                            }
                        }, FuModalSize.Medium);
                    }
                }
            }, flags: FuWindowFlags.AllowMultipleWindow);

        // add tree Window
        List<treeTestItem> treeItems = treeTestItem.GetRandomHierarchie();

        FuTree<treeTestItem> tree = null;
        tree = new FuTree<treeTestItem>("testTree",
            treeItems,
            FuTextStyle.Info,
            // how to draw an item
            (item, layout) =>
            {
                layout.SetNextElementToolTipWithLabel("Warning");
                layout.SetNextElementToolTipStyles(FuTextStyle.Warning);
                ImGui.AlignTextToFramePadding();
                layout.Text(Icons.Warning + " ", FuTextStyle.Warning);
                layout.SameLine();
                ImGui.AlignTextToFramePadding();
                layout.Text(item.Text);
                layout.SameLine();
                layout.Dummy(ImGui.GetContentRegionAvail().x - 20f);
                layout.SameLine();
                FuButtonStyle.Info.Push(true);
                if (layout.UnpaddedButton(Icons.Delete, new Vector2(1f, 0f), new Vector2(0.7f, -2.2f), FuButtonStyle.Danger))
                {
                    Fugui.ShowYesNoModal("Are you sure you want to remove this tree element ? (" + item.Text + ")",
                        (yes) =>
                        {
                            if (yes)
                            {
                                if (item.Parent != null)
                                {
                                    item.Parent.Children.Remove(item);
                                }
                                else
                                {
                                    treeItems.Remove(item);
                                }
                                tree.UpdateTree(treeItems);
                            }
                        }, FuModalSize.Medium);
                }
                FuButtonStyle.Info.Pop();
            },
            // when an item just open
            (item) => { item.IsOpen = 1; },
            // when an item just close
            (item) => { item.IsOpen = 0; },
            // get the level of an item
            (item) => item.Level,
            // are two items equals ?
            (a, b) => a == b,
            // how to get direct children
            (item) => item.Children,
            // whatever an item is open
            (item) => item.IsOpen == 1,
            // items height
            14f);

        new FuWindowDefinition(FuWindowsNames.Tree, "Tree", (window) =>
        {
            using (FuPanel panel = new FuPanel("testTreePanel", false))
            {
                Fugui.Push(ImGuiStyleVar.ItemSpacing, Vector2.zero);
                tree.DrawTree();
                Fugui.PopStyle();
            }
        }, flags: FuWindowFlags.AllowMultipleWindow);

        // add Capture Window
        new FuWindowDefinition(FuWindowsNames.Captures, "Notify Demo", (window) =>
        {
            using (FuLayout layout = new FuLayout())
            {
                layout.ComboboxEnum<AnchorLocation>("Notify Anchor", (anchor) =>
                {
                    Fugui.Settings.NotificationAnchorPosition = (AnchorLocation)anchor;
                }, () => Fugui.Settings.NotificationAnchorPosition);
                layout.Separator();
                foreach (StateType type in Enum.GetValues(typeof(StateType)))
                {
                    if (layout.Button("Notify " + type, FuButtonStyle.GetStyleForState(type)))
                    {
                        Fugui.Notify(type.ToString(), "This is a test " + type + " small notification.", type);
                    }
                }
                layout.Separator();
                foreach (StateType type in Enum.GetValues(typeof(StateType)))
                {
                    if (layout.Button("Notify long " + type, FuButtonStyle.GetStyleForState(type)))
                    {
                        Fugui.Notify(type.ToString(), "This is a test " + type + " notification. it's a quite long text for a notification but I have to test that the text wrapping don't mess with my notification panel height calculation.", type);
                    }
                }
                layout.Separator();
                foreach (StateType type in Enum.GetValues(typeof(StateType)))
                {
                    if (layout.Button("Notify title " + type, FuButtonStyle.GetStyleForState(type)))
                    {
                        Fugui.Notify("this is a type " + type.ToString(), null, type);
                    }
                }
                layout.Separator();
                foreach (StateType type in Enum.GetValues(typeof(StateType)))
                {
                    if (layout.Button("Notify message " + type, FuButtonStyle.GetStyleForState(type)))
                    {
                        Fugui.Notify(null, "this is a type " + type.ToString(), type);
                    }
                }
            }
        }, flags: FuWindowFlags.AllowMultipleWindow);

        // add Metadata Window
        // create common metadata context menu items
        var metadataContextMenuItems = FuContextMenuBuilder.Start()
            .AddItem("Action 0 Lvl 0", () => { Debug.Log("Action 0 Lvl 0"); })
            .AddItem("Action 1 Lvl 0", () => { Debug.Log("Action 1 Lvl 0"); })
            .AddSeparator()
            .BeginChild("Parent 0 LVl 0")
            .AddItem("Action 0 Lvl 1", () => { Debug.Log("Action 0 Lvl 1"); })
            .AddItem("Action 1 Lvl 1", () => { Debug.Log("Action 1 Lvl 1"); })
            .EndChild()
            .Build();

        // create extra list box context menu items
        var listboxContextMenuItems = FuContextMenuBuilder.Start()
            .AddItem("Action 0 Lvl 0 : extra", () => { Debug.Log("Action 0 Lvl 0 : extra"); })
            .AddSeparator()
            .BeginChild("Parent 0 LVl 0")
            .AddItem("Action 0 Lvl 1 : extra", () => { Debug.Log("Action 0 Lvl 1 : extra"); })
            .EndChild()
            .Build();

        // create extra list box 2 context menu items
        var listbox2ContextMenuItems = FuContextMenuBuilder.Start()
            .AddItem("This is a very special listbox", "some shortcut", () => { Debug.Log("click on my very special listbox !"); })
            .Build();

        // default spinner
        float spinnerSize = 20f;
        int spinnerNbDots = 6;
        float spinnerDotsSize = 2f;
        bool spinnerDoubleColor = false;
        Vector2 spinnerV2Size = new Vector2(64f, 20f);
        float spinnerFrequency = 6f;

        new FuWindowDefinition(FuWindowsNames.Metadata, "Metadata", (window) =>
        {
            Fugui.PushContextMenuItems(metadataContextMenuItems);
            using (new FuPanel("mdcc"))
            {
                using (var layout = new FuLayout())
                {
                    layout.Text("Check Fugui ");
                    layout.SameLine();
                    layout.TextURL("git page.", "https://framagit.org/Hydrocode/fugui", FuTextStyle.Info);

                    layout.Collapsable("Knobs", () =>
                    {
                        using (var grid = new FuGrid("gridKnobs"))
                        {
                            grid.Knob("knob Dot", ref floatVal, 0f, 100f, FuKnobVariant.Dot);
                            grid.Knob("knob Space", ref floatVal, 0f, 100f, FuKnobVariant.Space);
                            grid.Knob("knob WiperOnly", ref floatVal, 0f, 100f, FuKnobVariant.WiperOnly);
                            grid.Knob("knob Space", ref floatVal, 0f, 100f, FuKnobVariant.Tick);
                            grid.Knob("knob Wiper", ref floatVal, 0f, 100f, FuKnobVariant.Wiper);
                            grid.Knob("knob Stepped", ref floatVal, 0f, 100f, FuKnobVariant.Stepped, 10, 10f, "%1.f");
                        }
                    });

                    layout.Collapsable("Widgets", () =>
                    {
                        using (var grid = new FuGrid("gridMD"))
                        {
                            grid.ProgressBar("pb in", floatVal / 100f, ProgressBarTextPosition.Inside);
                            grid.ProgressBar("pb left", floatVal / 100f, ProgressBarTextPosition.Left);
                            grid.ProgressBar("pb right", floatVal / 100f, ProgressBarTextPosition.Right);
                            grid.ProgressBar("pb none", floatVal / 100f, ProgressBarTextPosition.None);
                            grid.ProgressBar("pb in small", floatVal / 100f, new Vector2(-1f, 8f), ProgressBarTextPosition.Inside);
                            grid.ProgressBar("pb no small", floatVal / 100f, new Vector2(-1f, 8f), ProgressBarTextPosition.None);
                            grid.ProgressBar("pb idle", new Vector2(-1f, 8f));

                            grid.CheckBox("checkbox ena", ref boolVal);
                            grid.DisableNextElement();
                            grid.CheckBox("checkbox dis", ref boolVal);

                            grid.Slider("slider int ena ", ref intVal);
                            grid.DisableNextElement();
                            grid.Slider("slider int dis ", ref intVal);

                            grid.Slider("slider float ena", ref floatVal);
                            grid.DisableNextElement();
                            grid.Slider("slider float dis", ref floatVal);
                            grid.Slider("slider float ena##NoDrag", ref floatVal, 0.5f, FuSliderFlags.NoDrag);
                            grid.Slider("slider float ena##LeftDrag", ref floatVal, 0.5f, FuSliderFlags.LeftDrag);
                            grid.DisableNextElement();
                            grid.Slider("slider float dis##NoDrag", ref intVal, FuSliderFlags.NoDrag);
                            grid.DisableNextElement();
                            grid.Slider("slider float dis##LeftDrag", ref intVal, FuSliderFlags.LeftDrag);

                            grid.Toggle("Toggle nude", ref toggleVal);
                            grid.Toggle("Toggle On/Off", ref toggleVal, "OFF", "ON", FuToggleFlags.AlignLeft);
                            grid.Toggle("Auto text size", ref toggleVal, "sm txt", "this is large text", FuToggleFlags.AlignLeft);
                            grid.Toggle("Max text size", ref toggleVal, "sm txt", "this is large text", FuToggleFlags.MaximumTextSize);
                            grid.DisableAnimationsFromNow();
                            grid.Toggle("No Animation", ref toggleVal, "No", "Anim");
                            grid.EnableAnimationsFromNow();
                            grid.DisableNextElement();
                            grid.Toggle("Disabled", ref toggleVal, "OFF", "ON", FuToggleFlags.MaximumTextSize);

                            grid.ButtonsGroup<FuToggleFlags>("Buttons Group", (flag) => { Debug.Log(flag + " selected"); });
                            grid.DisableNextElement();
                            grid.ButtonsGroup<FuToggleFlags>("Btn Grp disabled", (flag) => { Debug.Log(flag + " selected"); });

                            grid.SetNextElementToolTip("About", "Accelerate", "Arch", "Arrow Down");
                            grid.ButtonsGroup("Default", new List<string>() { Icons.About, Icons.Accelerate, Icons.Arch, Icons.ArrowDown }, (index) => { }, null, FuButtonsGroupFlags.Default);

                            grid.ButtonsGroup("Left", new List<string>() { Icons.About, Icons.Accelerate, Icons.Arch, Icons.ArrowDown }, (index) => { }, null, FuButtonsGroupFlags.AlignLeft);
                            grid.ButtonsGroup("Auto size", new List<string>() { Icons.About, Icons.Accelerate, Icons.Arch, Icons.ArrowDown }, (index) => { }, null, FuButtonsGroupFlags.AutoSizeButtons);
                            grid.ButtonsGroup("Left and auto size", new List<string>() { Icons.About, Icons.Accelerate, Icons.Arch, Icons.ArrowDown }, (index) => { }, null, FuButtonsGroupFlags.AlignLeft | FuButtonsGroupFlags.AutoSizeButtons);

                            grid.Range("Range test", ref min, ref max, 0f, 30f, 0.25f);
                            grid.Range("Range no drag", ref min, ref max, 0f, 30f, 0.1f, FuSliderFlags.NoDrag);
                        }
                    });

                    layout.Collapsable("Spinners", () =>
                    {
                        using (var grid = new FuGrid("gSPN", FuGridFlag.LinesBackground))
                        {
                            grid.Loader_Spinner(spinnerSize, spinnerNbDots, spinnerDotsSize, spinnerDoubleColor);
                            grid.Text("Spinner");
                            layout.Slider("size##spinner", ref spinnerSize, 4f, 128f);
                            layout.Slider("dots##spinner", ref spinnerNbDots, 1, 64);
                            layout.Slider("dot size##spinner", ref spinnerDotsSize, 1f, 16f);
                            layout.Toggle("double colors##spinner", ref spinnerDoubleColor);

                            grid.Loader_CircleSpinner(spinnerSize, spinnerNbDots);
                            grid.Text("Circle spinner");
                            layout.Slider("size##spinner1", ref spinnerSize, 4f, 128f);
                            layout.Slider("dots##spinner2", ref spinnerNbDots, 1, 64);

                            grid.Loader_ElipseSpinner(spinnerSize, spinnerNbDots, spinnerDotsSize, spinnerDoubleColor);
                            grid.Text("Elipse Spinner");
                            layout.Slider("size##spinner2", ref spinnerSize, 4f, 128f);
                            layout.Slider("dots##spinner2", ref spinnerNbDots, 1, 64);
                            layout.Slider("dot size##spinner2", ref spinnerDotsSize, 1f, 16f);
                            layout.Toggle("double colors##spinner2", ref spinnerDoubleColor);

                            grid.Loader_Wheel(spinnerSize);
                            grid.Text("Wheel");
                            layout.Slider("size##spinner3", ref spinnerSize, 4f, 128f);

                            grid.Loader_WavyLine(spinnerV2Size, spinnerFrequency, spinnerDoubleColor);
                            grid.Text("Wavy Line");
                            layout.Drag("size##spinner4", ref spinnerV2Size, "", "", 4f, 128f);
                            layout.Slider("frequency##spinner4", ref spinnerFrequency, 0.01f, 128f);

                            grid.Loader_Squares(spinnerSize);
                            grid.Text("Squares");
                            layout.Slider("size##spinner5", ref spinnerSize, 4f, 128f);

                            grid.Loader_SquareCircleDance(spinnerSize);
                            grid.Text("SquareCircleDance");
                            layout.Slider("size##spinner6", ref spinnerSize, 4f, 128f);

                            grid.Loader_PulsingLines(spinnerV2Size);
                            grid.Text("Pulsing Lines");
                            layout.Drag("size##spinner7", ref spinnerV2Size, "", "", 4f, 128f);

                            grid.Loader_Clocker(spinnerSize);
                            grid.Text("Clocker");
                            layout.Slider("size##spinner8", ref spinnerSize, 4f, 128f);

                            grid.Loader_Pulsar(spinnerSize);
                            grid.Text("Pulsar");
                            layout.Slider("size##spinner9", ref spinnerSize, 4f, 128f);

                            grid.Loader_SpikedWheel(spinnerV2Size);
                            grid.Text("Spiked Wheel");
                            layout.Drag("size##spinner10", ref spinnerV2Size, "", "", 4f, 128f);
                        }
                    });

                    layout.Collapsable("Drag Tests", () =>
                    {
                        layout.Range("layout Range test", ref min, ref max, 0f, 30f, 0.8f);
                        layout.Range("layout Range no drag", ref min, ref max, 0f, 30f, 0.5f, FuSliderFlags.NoDrag);
                        using (var grid = new FuGrid("gridMD2"))
                        {
                            grid.Drag("drag int ena", ref intVal);
                            grid.DisableNextElement();
                            grid.Drag("drag int dis", ref intVal);

                            grid.Drag("drag float ena", ref floatVal, "value");
                            grid.DisableNextElement();
                            grid.Drag("drag float dis", ref floatVal, "value");

                            grid.Drag("drag v2 ena", ref v2Val, "x", "y");
                            grid.DisableNextElement();
                            grid.Drag("drag v2 dis", ref v2Val, "x", "y");

                            grid.Drag("drag v3 ena", ref v3Val, "x", "y", "z");
                            grid.DisableNextElement();
                            grid.Drag("drag v3 dis", ref v3Val, "x", "y", "z");

                            grid.Drag("drag v4 ena", ref v4Val, "x", "y", "z", "w");
                            grid.DisableNextElement();
                            grid.Drag("drag v4 dis", ref v4Val, "x", "y", "z", "w");

                            Fugui.PushContextMenuItems(listboxContextMenuItems);
                            grid.ListBox("test callback combo", /*"click me custom",*/ () =>
                            {
                                bool chk = true;
                                layout.CheckBox("chdk1", ref chk);
                                layout.Drag("drdag", ref floatVal);
                                layout.Button("big button");
                                layout.DisableNextElement();
                                layout.Button("big button", FuButtonStyle.Highlight);
                                layout.Slider("sdlc1", ref intVal);
                                layout.DisableNextElement();
                                layout.Slider("sdlc2", ref intVal);
                                layout.Slider("sdlc3", ref floatVal);
                            });
                            Fugui.PushContextMenuItems(listbox2ContextMenuItems);
                            grid.ListBox("test combobox", cbTexts, (newValue) => { Debug.Log(newValue); });
                            Fugui.PopContextMenuItems();
                            grid.ListBox("test button box", cbButtons, (newValue) => { Debug.Log(newValue); });
                            Fugui.PopContextMenuItems();
                        }
                    });
                }
            }
            Fugui.TryOpenContextMenuOnWindowClick();
            Fugui.PopContextMenuItems();
        }, flags: FuWindowFlags.AllowMultipleWindow);

        // add Inspector Window
        new FuWindowDefinition(FuWindowsNames.Inspector, "Inspector", (window) =>
        {
            using (new FuPanel("demoContainer", FuStyle.Unpadded))
            {
                using (var layout = new FuLayout())
                {
                    layout.Collapsable("Transform", () =>
                    {
                        using (FuGrid grid = new FuGrid("transformGrid", rowsPadding: 1f, outterPadding: 8f))
                        {
                            grid.SetMinimumLineHeight(20f);
                            pos = Raycaster.transform.position;
                            grid.SetNextElementToolTip("x parameter of the position", "y parameter of the position", "z parameter of the position");
                            if (grid.Drag(Icons.Position + " Position", ref pos, "X", "Y", "Z", -100f, 100f))
                            {
                                Raycaster.transform.position = pos;
                            }

                            grid.SetNextElementToolTip("x parameter of the rotation", "y parameter of the rotation", "z parameter of the rotation");
                            rot = Raycaster.transform.localEulerAngles;
                            if (grid.Drag(Icons.Rotate + " Rotation", ref rot, "X", "Y", "Z", -360f, 360f))
                            {
                                Raycaster.transform.localEulerAngles = rot;
                            }

                            grid.SetNextElementToolTip("x parameter of the scale", "y parameter of the scale", "z parameter of the scale");
                            scale = Raycaster.transform.localScale;
                            if (grid.Drag("Scale", ref scale, "X", "Y", "Z", 0.1f, 2f))
                            {
                                Raycaster.transform.localScale = scale;
                            }
                        }
                    }, 8f);

                    layout.Collapsable("Camera", () =>
                    {
                        using (FuGrid grid = new FuGrid("cameraGrid", outterPadding: 8f))
                        {
                            grid.SetMinimumLineHeight(22f);
                            grid.SetNextElementToolTip("Clear flag of the main camera");
                            grid.ComboboxEnum<CameraClearFlags>("Clear Flags", (CameraClearFlags) =>
                            {
                                cam1.clearFlags = (CameraClearFlags)CameraClearFlags;
                            }, () => { return cam1.clearFlags; });

                            float FOV = cam1.fieldOfView;
                            grid.SetNextElementToolTip("Field of View (FOV) of the main camera");
                            if (grid.Slider("Field of view", ref FOV))
                            {
                                cam1.fieldOfView = FOV;
                            }

                            grid.CheckBox("Physical Camera", ref physicalCamera);
                            grid.TextInput("Title", "Enter title", ref title);
                            grid.TextInput("Description", "Enter description", ref description, 4096, 64f, 0f);

                            grid.NextColumn();
                            grid.SmartText(description);

                            grid.SetNextElementToolTip("Background Color of the main camera (only if Clear Flag is on 'Solid Color')");
                            if (cam1.clearFlags != CameraClearFlags.SolidColor)
                            {
                                grid.DisableNextElement();
                            }
                            Vector4 color = cam1.backgroundColor;
                            if (grid.ColorPicker("BG Color", ref color))
                            {
                                cam1.backgroundColor = color;
                            }

                            grid.SetNextElementToolTip("It's just a test for Alphaless colorPicker");
                            grid.ColorPicker("Color alphaless", ref coloralphaless);
                        }
                    }, 8f);
                }
            }
        }, flags: FuWindowFlags.AllowMultipleWindow);

        // add Fugui Settings Window
        new FuWindowDefinition(FuWindowsNames.FuguiSettings, "Fugui Settings", (window) =>
        {
            Fugui.DrawSettings();
        }, flags: FuWindowFlags.AllowMultipleWindow);

        // add main camera window
        FuWindowDefinition camWinDef = new FuCameraWindowDefinition(FuWindowsNames.MainCameraView, cam1, "3DView", null, flags: FuWindowFlags.NoInterractions)
            .SetCustomWindowType<FuCameraWindow>();
        camWinDef.OnUIWindowCreated += CamWinDef_OnUIWindowCreated;

        #region Overlays
        // render graph panel
        FuOverlay rg = new FuOverlay("oRG", new Vector2(224f, 36f), (overlay) =>
        {
            using (FuLayout layout = new FuLayout())
            {
                layout.ComboboxEnum<AnchorLocation>("##rgCB", (val) => { });
                layout.SameLine();
                layout.Button(Icons.Ghost, FuElementSize.AutoSize);
                layout.SameLine();
                layout.Button(Icons.DataFusion, FuElementSize.AutoSize, FuButtonStyle.Highlight);
            };
        }, FuOverlayFlags.NoBackground | FuOverlayFlags.NoClose | FuOverlayFlags.NoMove);
        rg.AnchorWindowDefinition(camWinDef, AnchorLocation.TopLeft, Vector2.zero);

        // gizmos panel
        FuOverlay gz = new FuOverlay("oGP", new Vector2(46f, 36f), (overlay) =>
        {
            using (FuLayout layout = new FuLayout())
            {
                layout.Button(Icons.Gizmo, FuButtonStyle.Highlight);
            }
        }, FuOverlayFlags.NoClose | FuOverlayFlags.NoMove | FuOverlayFlags.NoBackground, FuOverlayDragPosition.Right);
        gz.AnchorWindowDefinition(camWinDef, AnchorLocation.TopRight, Vector2.zero);

        // legend
        FuOverlay bc = new FuOverlay("oLP", new Vector2(128f, 128f), (overlay) =>
        {
            using (FuLayout layout = new FuLayout())
            {
                layout.Text("Legend 1");
                layout.Text("Legend 2");
                layout.Text("Legend 3");
                layout.Text("Legend 4");
                layout.Text("Legend 5");
            }
        }, FuOverlayFlags.NoEditAnchor, FuOverlayDragPosition.Bottom);
        bc.AnchorWindowDefinition(camWinDef, AnchorLocation.BottomLeft, Vector2.zero);

        // legend
        FuOverlay tb = new FuOverlay("oTB", new Vector2(312f, 48f), (overlay) =>
        {
            using (FuLayout layout = new FuLayout())
            {
                Fugui.PushFont(18, FontType.Regular);
                layout.SetNextElementToolTip("Measures", "Notes", "Sections", "Shapes", "MLI", "Manikins", "Environments");
                layout.Button(Icons.Measure, new Vector2(32f, 32f));
                layout.SameLine();
                layout.Button(Icons.Note, new Vector2(32f, 32f));
                layout.SameLine();
                layout.Button(Icons.Section, new Vector2(32f, 32f));
                layout.SameLine();
                layout.Dummy(0, 0);
                layout.SameLine();
                layout.Button(Icons.Cube, new Vector2(32f, 32f));
                layout.SameLine();
                layout.Button(Icons.EditShape, new Vector2(32f, 32f));
                layout.SameLine();
                layout.Dummy(Vector2.zero);
                layout.SameLine();
                layout.Button(Icons.Manikin, new Vector2(32f, 32f));
                layout.SameLine();
                layout.Button(Icons.Environment, new Vector2(32f, 32f));
                layout.SameLine();
                layout.Dummy(Vector2.one);
                layout.SameLine();
                layout.Button(Icons.EditShape, new Vector2(32f, 32f), FuButtonStyle.Highlight);
                Fugui.PopFont();
            }
        }, FuOverlayFlags.Default, FuOverlayDragPosition.Top);
        tb.AnchorWindowDefinition(camWinDef, AnchorLocation.TopCenter, Vector2.zero);

        // FPS display Cam 1
        FuOverlay fps1 = new FuOverlay("oCamFPS", new Vector2(102f, 52f), (overlay) =>
        {
            drawCameraFPSOverlay(_mainCam);
        }, FuOverlayFlags.Default, FuOverlayDragPosition.Right);
        fps1.AnchorWindowDefinition(camWinDef, AnchorLocation.TopRight, new Vector2(0f, 64f));

        // cam 1 SS
        FuOverlay ss1 = new FuOverlay("oCamSS", new Vector2(224f, 36f), (overlay) =>
        {
            drawCameraOverlay(_mainCam);
        }, FuOverlayFlags.Default, FuOverlayDragPosition.Right);
        ss1.AnchorWindowDefinition(camWinDef, AnchorLocation.BottomRight, Vector2.zero);

        void drawCameraOverlay(FuCameraWindow cam)
        {
            using (var layout = new FuLayout())
            {
                if (layout.RadioButton("x0.5", cam.SuperSampling == 0.5f))
                {
                    cam.SuperSampling = 0.5f;
                }
                layout.SameLine();
                if (layout.RadioButton("x1", cam.SuperSampling == 1f))
                {
                    cam.SuperSampling = 1f;
                }
                layout.SameLine();
                if (layout.RadioButton("x1.5", cam.SuperSampling == 1.5f))
                {
                    cam.SuperSampling = 1.5f;
                }
                layout.SameLine();
                if (layout.RadioButton("x2", cam.SuperSampling == 2f))
                {
                    cam.SuperSampling = 2f;
                }
            }
        }

        // cam 1 SS
        int qualityIndex = 3;
        FuOverlay fastest = new FuOverlay("oFF", new Vector2(278f, 36f), (overlay) =>
        {
            using (FuLayout layout = new FuLayout())
            {
                Fugui.PushFont(12, FontType.Regular);
                layout.Text("Fastest");
                layout.SameLine();
                if (layout.RadioButton("##q0", qualityIndex == 0))
                {
                    qualityIndex = 0;
                }
                layout.SameLine();
                if (layout.RadioButton("##q1", qualityIndex == 1))
                {
                    qualityIndex = 1;
                }
                layout.SameLine();
                if (layout.RadioButton("##q2", qualityIndex == 2))
                {
                    qualityIndex = 2;
                }
                layout.SameLine();
                if (layout.RadioButton("##q3", qualityIndex == 3))
                {
                    qualityIndex = 3;
                }
                layout.SameLine();
                if (layout.RadioButton("##q4", qualityIndex == 4))
                {
                    qualityIndex = 4;
                }
                layout.SameLine();
                if (layout.RadioButton("##q5", qualityIndex == 5))
                {
                    qualityIndex = 5;
                }
                layout.SameLine();
                layout.Text("Fantastic");
                Fugui.PopFont();
            }
        }, FuOverlayFlags.Default, FuOverlayDragPosition.Bottom);
        fastest.AnchorWindowDefinition(camWinDef, AnchorLocation.BottomCenter, Vector2.zero);

        void drawCameraFPSOverlay(FuCameraWindow cam)
        {
            using (FuGrid grid = new FuGrid("camFPS", new FuGridDefinition(2, new int[] { 42 }, responsiveMinWidth: 0)))
            {
                grid.Text("cam FPS");
                grid.Text(((int)cam.CurrentCameraFPS).ToString());
                grid.Text("ui. FPS");
                grid.Text(((int)cam.CurrentFPS).ToString());
            }
        }
        #endregion

        // imgui demo window
        if (ShowImGuiDemoWindow)
        {
            // TODO : Use Context
            Fugui.DefaultContext.OnRender += UImGuiUtility_Layout;
        }

        // set default layout (will create UIWindows)
        if (FuDockingLayoutManager.Layouts.Count > 0)
        {
            // TODO : save layout into enum and set theme by enum (easyer by code)
            string firstKey = FuDockingLayoutManager.Layouts.Keys.ToList()[0];
            FuDockingLayoutManager.SetLayout(FuDockingLayoutManager.Layouts[firstKey]);
        }
    }

    private void DockingLayoutManager_OnDockLayoutInitialized()
    {
        // instantiate test 3D window
        Fugui.CreateWindowAsync(FuWindowsNames.FuguiSettings, (window) =>
        {
            Fugui.Add3DWindow(window, new Vector3(0f, -2f, 0f), Quaternion.Euler(Vector3.up * 180f));
        }, false);
    }

    private void DockingLayoutManager_OnDockLayoutReloaded()
    {
        //Unregistered menu and all children
        Fugui.UnregisterMainMenuItem("Layout");

        //Register the layout menu empty
        Fugui.RegisterMainMenuItem("Layout", null);

        foreach (KeyValuePair<string, FuDockSpaceDefinition> layoutDefinition in FuDockingLayoutManager.Layouts)
        {
            //Add new children
            string menuName = Fugui.AddSpacesBeforeUppercase(layoutDefinition.Key);
            if (!Fugui.IsMainMenuRegisteredItem(menuName))
            {
                Fugui.RegisterMainMenuItem(menuName, () => FuDockingLayoutManager.SetLayout(layoutDefinition.Value), "Layout");
            }
        }
    }

    private void CamWinDef_OnUIWindowCreated(FuWindow camWindow)
    {
        _mainCam = (FuCameraWindow)camWindow;
        _mainCam.Camera.GetComponent<MouseOrbitImproved>().Camera = _mainCam;
    }

    private void UImGuiUtility_Layout()
    {
        ImGuiNET.ImGui.ShowDemoWindow();
    }

    private void Update()
    {
        if (_mainCam == null || !_mainCam.IsInitialized)
        {
            return;
        }

        // debug keyboard state
        if (_mainCam.Keyboard.GetKeyDown(FuKeysCode.A))
        {
            Debug.Log("A Down in camera view");
        }
        if (_mainCam.Keyboard.GetKeyPressed(FuKeysCode.A))
        {
            Debug.Log("A Pressed in camera view");
        }
        if (_mainCam.Keyboard.GetKeyUp(FuKeysCode.A))
        {
            Debug.Log("A Up in camera view");
        }

        if (_mainCam.Mouse.IsDown(0) && !_mainCam.Mouse.IsHoverOverlay && !_mainCam.Mouse.IsHoverPopup)
        {
            RaycastHit hit;
            Ray ray = _mainCam.GetCameraRay();
            if (Physics.Raycast(ray, out hit))
            {
                clickOnSphere(hit, ray);
            }
        }
    }

    float hitForce = 50f;
    private void clickOnSphere(RaycastHit hit, Ray ray)
    {
        Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
        if (rb == null)
            return;
        rb.AddExplosionForce(hitForce, hit.point, 5f, 0f, ForceMode.Impulse);
    }

    private void OnDrawGizmos()
    {
        if (!ShowRaycastersDebug)
        {
            return;
        }

        foreach (FuRaycaster ray in FuRaycasting.GetAllRaycasters())
        {
            Ray r = ray.GetRay();
            Gizmos.DrawRay(r);
        }
    }
}