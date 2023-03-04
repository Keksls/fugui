using Fu;
using Fu.Core;
using Fu.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// this sample show how to use Fugui API
/// </summary>
public class FuguiDemoScene : MonoBehaviour
{
    public static FuguiDemoScene Instance;
    public Camera cam1;
    public Texture2D TmpDebugTexture;
    public bool ShowImGuiDemoWindow = false;
    public bool ShowRaycastersDebug = false;
    private FuCameraWindow _mainCam;

    private void Awake()
    {
        // register Fugui Settings Window
        new FuWindowDefinition(FuSystemWindowsNames.FuguiSettings, "Fugui Settings", (window) =>
        {
            Fugui.DrawSettings();
        }, flags: FuWindowFlags.AllowMultipleWindow);
    }

    private void Start()
    {
        FuDockingLayoutManager.OnDockLayoutReloaded += DockingLayoutManager_OnDockLayoutReloaded;
        FuDockingLayoutManager.OnDockLayoutInitialized += DockingLayoutManager_OnDockLayoutInitialized;
        FuDockingLayoutManager.Initialize(FuWindowsNames.GetAllWindowsNames());
        registerMainMenuItems();
    }

    private void registerMainMenuItems()
    {
        // set demo Main menu
        Fugui.RegisterMainMenuItem(Icons.DragonflyLogo + " Files", null);

        Fugui.RegisterMainMenuItem("Layout", null);
        foreach (KeyValuePair<string, FuDockingLayoutDefinition> layoutDefinition in FuDockingLayoutManager.Layouts)
        {
            string menuName = Fugui.AddSpacesBeforeUppercase(layoutDefinition.Key);
            if (!Fugui.IsMainMenuRegisteredItem(menuName))
            {
                Fugui.RegisterMainMenuItem(menuName, () => FuDockingLayoutManager.SetLayout(layoutDefinition.Key), "Layout");
            }
        }

        Fugui.RegisterMainMenuItem("Windows", null);
        foreach (FuWindowName windowName in FuWindowsNames.GetAllWindowsNames())
        {
            if (windowName.Equals(FuSystemWindowsNames.None))
            {
                continue;
            }
            Fugui.RegisterMainMenuItem(windowName.ToString(), () => Fugui.CreateWindowAsync(windowName, null), "Windows");
        }

        Fugui.RegisterMainMenuItem("3D Windows", null);
        foreach (FuWindowName windowName in FuWindowsNames.GetAllWindowsNames())
        {
            if (windowName.Equals(FuWindowsNames.None))
            {
                continue;
            }
            Fugui.RegisterMainMenuItem("3D " + windowName.ToString(), () => Fugui.CreateWindowAsync(windowName, (window) => { Fugui.Add3DWindow(window, new Vector3(0f, -2f, 0f), Quaternion.Euler(Vector3.up * 180f)); }, false), "3D Windows");
        }
    }

    void _Start()
    {
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

        // add Modals Window
        new FuWindowDefinition(FuWindowsNames.Popups, "Popups Demo", (window) =>
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

        // add Capture Window
        new FuWindowDefinition(FuWindowsNames.Captures, "Notify Demo", (window) =>
        {
            using (FuLayout layout = new FuLayout())
            {
                layout.ComboboxEnum<FuOverlayAnchorLocation>("Notify Anchor", (anchor) =>
                {
                    Fugui.Settings.NotificationAnchorPosition = (FuOverlayAnchorLocation)anchor;
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
            FuDockingLayoutManager.SetLayout(firstKey);
        }
    }

    private void DockingLayoutManager_OnDockLayoutInitialized()
    {
        // instantiate test 3D window
        Fugui.CreateWindowAsync(FuSystemWindowsNames.FuguiSettings, (window) =>
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

        foreach (KeyValuePair<string, FuDockingLayoutDefinition> layoutDefinition in FuDockingLayoutManager.Layouts)
        {
            //Add new children
            string menuName = Fugui.AddSpacesBeforeUppercase(layoutDefinition.Key);
            if (!Fugui.IsMainMenuRegisteredItem(menuName))
            {
                Fugui.RegisterMainMenuItem(menuName, () => FuDockingLayoutManager.SetLayout(layoutDefinition.Value, layoutDefinition.Key), "Layout");
            }
        }
    }

    private void UImGuiUtility_Layout()
    {
        ImGuiNET.ImGui.ShowDemoWindow();
    }

}