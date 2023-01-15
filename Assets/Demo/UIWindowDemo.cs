using UnityEngine;
using Fugui.Core;
using ImGuiNET;
using Fugui.Framework;
using System.Collections.Generic;
using System;

/// <summary>
/// this sample show how to use UIWindow toolkit
/// </summary>
public class UIWindowDemo : MonoBehaviour
{
    public Camera cam1;
    public Texture2D TmpDebugTexture;
    public GameObject LastPointObject;
    public bool ShowImGuiDemoWindow = false;
    private UICameraWindow _mainCam;

    void Start()
    {
        IUIWindowContainer mainContainer = FuGui.MainContainer;

        // add debug window
        bool toggleVal = false;
        bool boolVal = false;
        int intVal = 5;
        float floatVal = 5f;
        Vector2 v2Val = Vector2.zero;
        Vector3 v3Val = Vector3.zero;
        Vector4 v4Val = Vector4.zero;
        List<string> cbTexts = new List<string>() { "cb1", "cb2", "cb3" };
        List<IComboboxItem> cbButtons = new List<IComboboxItem>() {
            new ComboboxButtonItem("button 1", UIButtonStyle.FullSize),
            new ComboboxButtonItem("button 2"),
            new ComboboxSameLineItem(),
            new ComboboxButtonItem("button 3", false),
            new ComboboxSeparatorItem(),
            new ComboboxButtonItem("button 4"),
            new ComboboxSameLineItem(),
            new ComboboxButtonItem("button 5"),
        };
        Vector3 pos = Vector3.zero;
        Vector3 rot = Vector3.zero;
        Vector3 scale = Vector3.zero;
        Vector4 color = new Vector4(.5f, 1f, .8f, .7f);
        Vector3 coloralphaless = new Vector3(.5f, 1f, .8f);
        bool physicalCamera = false;
        string title = "";
        string description = "A regular size <b> Bold TEXT</b>, and a lineBreak<br/>this score-splitted-text can be breal-lined on score.<br/>" +
            "<size=10>a small text (10px)</size> then a <size=18>realy BIG <b>Bold</b> text (18px)</size> then back to normal.<br/>" +
            "<color=#FF00FFFF>Want some <color=#00FF00FF><b>COLOR</b>?</color> : here <b> it is</b></color>!";

        // set demo Main menu
        MainMenu.RegisterItem(Icons.DragonflyLogo + " Files", null);
        MainMenu.RegisterItem("Layout", null);
        MainMenu.RegisterItem("Default", () => DockingLayoutManager.SetLayout(UIDockingLayout.Default), "Layout", "Alt + Left Arrow");
        MainMenu.RegisterItem("Console on Bottom", () => DockingLayoutManager.SetLayout(UIDockingLayout.Console), "Layout", "Alt + Right Arrow");
        MainMenu.RegisterItem("Windows", null);
        foreach (UIWindowName windowName in Enum.GetValues(typeof(UIWindowName)))
        {
            if (windowName == UIWindowName.None)
            {
                continue;
            }
            MainMenu.RegisterItem(windowName.ToString(), () => FuGui.CreateWindowAsync(windowName, null), "Windows");
        }

        new UIWindowDefinition(UIWindowName.ToolBox, "Tool Box", debugWindow_UI);
        void debugWindow_UI(UIWindow window)
        {
            using (new UIContainer("debugContainer"))
            {
                using (UIGrid grid = new UIGrid("debugGrid"))
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
                        grid.Text(window.WindowPerformanceState.ToString());

                        bool chkVal = window.IsDragging;
                        grid.DisableNextElement();
                        grid.CheckBox("Dragging", ref chkVal);

                        chkVal = window.IsResizing;
                        grid.DisableNextElement();
                        grid.CheckBox("Resizing", ref chkVal);
                    }

                    grid.Separator();

                    if (grid.Button("Mouse Pos and Down Text", UIButtonStyle.FullSize))
                    {
                        Debug.Log("click !");
                    }
                    grid.DisableNextElement();
                    grid.Button("Mouse Pos and Down Text", UIButtonStyle.FullSize, UIButtonStyle.Blue);
                }
                window.Container.ImGuiImage(TmpDebugTexture, new Vector2(240f, 100f));
            }
        }

        // add Tree Window
        new UIWindowDefinition(UIWindowName.Tree, "Tree", (window) =>
            {
                for (int i = 0; i < 10; i++)
                {
                    ImGui.Text("Tree Item " + i);
                }
            }, isInterractible: false);

        // add Capture Window
        new UIWindowDefinition(UIWindowName.Captures, "Captures", (window) =>
        {
            for (int i = 0; i < 10; i++)
            {
                ImGui.Button("Capture " + i, new Vector2(128f, 128f));
            }
        }, isInterractible: false);

        // add Metadata Window
        new UIWindowDefinition(UIWindowName.Metadata, "Metadata", (window) =>
        {
            using (new UIContainer("mdcc"))
            {
                using (var layout = new UILayout())
                {
                    using (var grid = new UIGrid("gridMD"))
                    {
                        grid.CheckBox("checkbox ena", ref boolVal);
                        grid.DisableNextElement();
                        grid.CheckBox("checkbox dis", ref boolVal);

                        grid.Slider("slider int ena ", ref intVal);
                        grid.DisableNextElement();
                        grid.Slider("slider int dis ", ref intVal);

                        grid.Slider("slider float ena", ref floatVal);
                        grid.DisableNextElement();
                        grid.Slider("slider float dis", ref floatVal);

                        grid.Toggle("Toggle nude", ref toggleVal);
                        grid.Toggle("Toggle On/Off", ref toggleVal, "OFF", "ON", ToggleFlags.AlignLeft);
                        grid.Toggle("Auto text size", ref toggleVal, "sm txt", "this is large text", ToggleFlags.AlignLeft);
                        grid.Toggle("Max text size", ref toggleVal, "sm txt", "this is large text", ToggleFlags.MaximumTextSize);
                        grid.DisableAnimationsFromNow();
                        grid.Toggle("No Animation", ref toggleVal, "No", "Anim");
                        grid.EnableAnimationsFromNow();
                        grid.DisableNextElement();
                        grid.Toggle("Disabled", ref toggleVal, "OFF", "ON", ToggleFlags.MaximumTextSize);

                        grid.ButtonsGroup<ToggleFlags>("Buttons Group", (flag) => { Debug.Log(flag + " selected"); });
                        grid.DisableNextElement();
                        grid.ButtonsGroup<ToggleFlags>("Btn Grp disabled", (flag) => { Debug.Log(flag + " selected"); });

                        grid.SetNextElementToolTip("About", "Accelerate", "Arch", "Arrow Down");
                        grid.ButtonsGroup("Default", new List<string>() { Icons.About, Icons.Accelerate, Icons.Arch, Icons.ArrowDown }, (index) => { }, 0, ButtonsGroupFlags.Default);

                        grid.ButtonsGroup("Left", new List<string>() { Icons.About, Icons.Accelerate, Icons.Arch, Icons.ArrowDown }, (index) => { }, 0, ButtonsGroupFlags.AlignLeft);
                        grid.ButtonsGroup("Auto size", new List<string>() { Icons.About, Icons.Accelerate, Icons.Arch, Icons.ArrowDown }, (index) => { }, 0, ButtonsGroupFlags.AutoSizeButtons);
                        grid.ButtonsGroup("Left and auto size", new List<string>() { Icons.About, Icons.Accelerate, Icons.Arch, Icons.ArrowDown }, (index) => { }, 0, ButtonsGroupFlags.AlignLeft | ButtonsGroupFlags.AutoSizeButtons);
                    }

                    layout.Collapsable("Drag Tests", () =>
                    {
                        using (var grid = new UIGrid("gridMD2"))
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

                            grid.Combobox("test callback combo", "click me custom", () =>
                            {
                                bool chk = true;
                                layout.CheckBox("chdk1", ref chk);
                                layout.Drag("drdag", ref floatVal);
                                layout.Button("big button");
                                layout.DisableNextElement();
                                layout.Button("big button", UIButtonStyle.Blue);
                                layout.Slider("sdlc1", ref intVal);
                                layout.DisableNextElement();
                                layout.Slider("sdlc2", ref intVal);
                                layout.Slider("sdlc3", ref floatVal);
                            });

                            grid.Combobox<string>("test combobox", cbTexts, (newValue) => { Debug.Log(newValue); });
                            grid.Combobox("test button box", cbButtons, (newValue) => { Debug.Log(newValue); });
                        }
                    });
                }
            }
        });

        // add Tree Window
        new UIWindowDefinition(UIWindowName.Tree, "Tree", (window) =>
        {
            for (int i = 0; i < 10; i++)
            {
                ImGui.Text("Tree Item " + i);
            }
        }, isInterractible: false);

        // add Inspector Window
        new UIWindowDefinition(UIWindowName.Inspector, "Inspector", (window) =>
        {
            using (new UIContainer("demoContainer", UILayoutStyle.Unpadded))
            {
                using (var layout = new UILayout())
                {
                    layout.Collapsable("Transform", () =>
                    {
                        using (UIGrid grid = new UIGrid("transformGrid", rowsPadding: 1f, outterPadding: 8f))
                        {
                            grid.SetMinimumLineHeight(20f);
                            pos = cam1.transform.position;
                            grid.SetNextElementToolTip("x parameter of the position", "y parameter of the position", "z parameter of the position");
                            if (grid.Drag(Icons.Position + " Position", ref pos, "X", "Y", "Z", -100f, 100f))
                            {
                                cam1.transform.position = pos;
                            }

                            grid.SetNextElementToolTip("x parameter of the rotation", "y parameter of the rotation", "z parameter of the rotation");
                            rot = cam1.transform.localEulerAngles;
                            if (grid.Drag(Icons.Rotate + " Rotation", ref rot, "X", "Y", "Z", -360f, 360f))
                            {
                                cam1.transform.localEulerAngles = rot;
                            }

                            grid.SetNextElementToolTip("x parameter of the scale", "y parameter of the scale", "z parameter of the scale");
                            scale = cam1.transform.localScale;
                            if (grid.Drag("Scale", ref scale, "X", "Y", "Z", 0.1f, 2f))
                            {
                                cam1.transform.localScale = scale;
                            }
                        }
                    }, 8f);

                    layout.Collapsable("Camera", () =>
                    {
                        using (UIGrid grid = new UIGrid("cameraGrid", outterPadding: 8f))
                        {
                            grid.SetMinimumLineHeight(22f);
                            grid.SetNextElementToolTip("Clear flag of the main camera");
                            grid.ComboboxEnum<CameraClearFlags>("Clear Flags", (CameraClearFlags) =>
                            {
                                cam1.clearFlags = CameraClearFlags;
                            });

                            float FOV = cam1.fieldOfView;
                            grid.SetNextElementToolTip("Field of View (FOV) of the main camera");
                            if (grid.Slider("Field of view", ref FOV))
                            {
                                cam1.fieldOfView = FOV;
                            }

                            grid.CheckBox("Physical Camera", ref physicalCamera);
                            grid.TextInput("Title", "Enter title", ref title);
                            grid.TextInput("Description", "Enter description", ref description, 4096, 64f);

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
        });

        // add Theme Window
        new UIWindowDefinition(UIWindowName.Theme, "Theme Configurator", (window) =>
            {
                ThemeManager.DrawThemeManagerUI();
            });

        // add main camera window
        UIWindowDefinition camWinDef = new UICameraWindowDefinition(UIWindowName.MainCameraView, cam1, "3DView", null, isInterractible: false)
            .SetCustomWindowType<UICameraWindow>();
        camWinDef.OnUIWindowCreated += CamWinDef_OnUIWindowCreated;

        #region Overlays
        // render graph panel
        UIOverlay rg = new UIOverlay("RenderGraphPanel", new Vector2(224f, 36f), (overlay) =>
        {
            using (UILayout layout = new UILayout())
            {
                layout.ComboboxEnum<AnchorLocation>("##rgCbTest", (val) => { }, UIComboboxStyle.Default);
                layout.SameLine();
                layout.Button(Icons.Ghost, UIButtonStyle.AutoSize);
                layout.SameLine();
                layout.Button(Icons.DataFusion, UIButtonStyle.AutoSize, UIButtonStyle.Blue);
            };
        }, OverlayFlags.NoBackground | OverlayFlags.NoClose | OverlayFlags.NoMove);
        rg.AnchorWindowDefinition(camWinDef, AnchorLocation.TopLeft, Vector2.zero);

        // gizmos panel
        UIOverlay gz = new UIOverlay("GizmoPanel", new Vector2(136f, 36f), (overlay) =>
        {
            ImGui.Button("Gizmos Settings");
            ImGui.SameLine();
            ImGui.Button("1");
        }, OverlayFlags.NoClose | OverlayFlags.NoMove, OverlayDragPosition.Right);
        gz.AnchorWindowDefinition(camWinDef, AnchorLocation.TopRight, Vector2.zero);

        // legend
        UIOverlay bc = new UIOverlay("LegendPanel", new Vector2(128f, 128f), (overlay) =>
        {
            ImGui.Text("Legend 1");
            ImGui.Text("Legend 2");
            ImGui.Text("Legend 3");
            ImGui.Text("Legend 4");
            ImGui.Text("Legend 5");
        }, OverlayFlags.NoEditAnchor, OverlayDragPosition.Bottom);
        bc.AnchorWindowDefinition(camWinDef, AnchorLocation.BottomLeft, Vector2.zero);

        // legend
        UIOverlay tb = new UIOverlay("ToolBarOverlay", new Vector2(312f, 48f), (overlay) =>
        {
            using (UILayout layout = new UILayout())
            {
                FuGui.PushFont(18, Fugui.Framework.FontType.Regular);
                layout.SetNextElementToolTip("Measures", "Notes", "Sections", "Shapes", "MLI", "Manikins", "Environments");
                layout.Button(Icons.Measure, new Vector2(32f, 32f));
                layout.SameLine();
                layout.Button(Icons.Note, new Vector2(32f, 32f));
                layout.SameLine();
                layout.Button(Icons.Section, new Vector2(32f, 32f));
                layout.SameLine();
                ImGui.Dummy(Vector2.zero);
                layout.SameLine();
                layout.Button(Icons.Cube, new Vector2(32f, 32f));
                layout.SameLine();
                layout.Button(Icons.EditShape, new Vector2(32f, 32f));
                layout.SameLine();
                ImGui.Dummy(Vector2.zero);
                layout.SameLine();
                layout.Button(Icons.Manikin, new Vector2(32f, 32f));
                layout.SameLine();
                layout.Button(Icons.Environment, new Vector2(32f, 32f));
                layout.SameLine();
                ImGui.Dummy(Vector2.one);
                layout.SameLine();
                layout.Button(Icons.EditShape, new Vector2(32f, 32f), UIButtonStyle.Blue);
                FuGui.PopFont();
            }
        }, OverlayFlags.Default, OverlayDragPosition.Top);
        tb.AnchorWindowDefinition(camWinDef, AnchorLocation.TopCenter, Vector2.zero);

        // FPS display Cam 1
        UIOverlay fps1 = new UIOverlay("FPSOverlayCam1", new Vector2(102f, 52f), (overlay) =>
        {
            drawCameraFPSOverlay(_mainCam);
        }, OverlayFlags.Default, OverlayDragPosition.Right);
        fps1.AnchorWindowDefinition(camWinDef, AnchorLocation.TopRight, new Vector2(0f, 64f));

        // cam 1 SS
        UIOverlay ss1 = new UIOverlay("Cam1SS", new Vector2(224f, 36f), (overlay) =>
        {
            drawCameraOverlay(_mainCam);
        }, OverlayFlags.Default, OverlayDragPosition.Right);
        ss1.AnchorWindowDefinition(camWinDef, AnchorLocation.BottomRight, Vector2.zero);

        void drawCameraOverlay(UICameraWindow cam)
        {
            using (var layout = new UILayout())
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
        UIOverlay fastest = new UIOverlay("FastestF", new Vector2(224f, 36f), (overlay) =>
        {
            using (UILayout layout = new UILayout())
            {
                FuGui.PushFont(12, Fugui.Framework.FontType.Regular);
                layout.Text("Fastest");
                layout.SameLine();
                ImGui.RadioButton("", false);
                ImGui.SameLine();
                ImGui.RadioButton("", false);
                ImGui.SameLine();
                ImGui.RadioButton("", false);
                ImGui.SameLine();
                ImGui.RadioButton("", false);
                ImGui.SameLine();
                ImGui.RadioButton("", false);
                ImGui.SameLine();
                ImGui.RadioButton("", true);
                ImGui.SameLine();
                layout.SameLine();
                layout.Text("Fantastic");
                FuGui.PopFont();
            }
        }, OverlayFlags.Default, OverlayDragPosition.Bottom);
        fastest.AnchorWindowDefinition(camWinDef, AnchorLocation.BottomCenter, Vector2.zero);

        void drawCameraFPSOverlay(UICameraWindow cam)
        {
            ImGui.Text("cam FPS : " + (int)cam.CurrentCameraFPS);
            ImGui.Text("ui. FPS : " + (int)cam.CurrentFPS);
        }
        #endregion

        #region Toolbar
        UIWindowDefinition toolbarDef = new UIWindowDefinition(UIWindowName.ToolBar, "toolbarWindow").SetCustomWindowType<UIToolBarWindow>();
        toolbarDef.OnUIWindowCreated += ToolbarDef_OnUIWindowCreated;
        //new UIWindow(toolbarDef).TryAddToContainer(FuGui.MainContainer);
        #endregion

        // imgui demo window
        if (ShowImGuiDemoWindow)
        {
            // TODO : Use Context
            FuGui.DefaultContext.OnRender += UImGuiUtility_Layout;
        }

        // set default layout (will create UIWindows)
        DockingLayoutManager.SetLayout(UIDockingLayout.Default);
    }

    private void ToolbarDef_OnUIWindowCreated(UIWindow window)
    {
        UIToolBarWindow toolbar = (UIToolBarWindow)window;
        toolbar.AddShortcut(new Shortcut("1", "shortcut 1", null)
            .AddAction("a1", "Action 1", null)
            .AddAction("a2", "Action 2", null));

        toolbar.AddShortcut(new Shortcut("2", "shortcut 2", null)
            .AddAction("a1", "Action 1", null)
            .AddAction("a2", "Action 2", null));

        toolbar.AddShortcut(new Shortcut("3", "shortcut 3", null)
            .AddAction("a1", "Action 1", null)
            .AddAction("a2", "Action 2", null)
            .AddAction("a3", "Action 3", null));

        toolbar.AddShortcut(new Shortcut("4", "shortcut 4", null)
            .AddAction("a1", "Action 1", null)
            .AddAction("a2", "Action 2", null)
            .AddAction("a3", "Action 3", null));

        toolbar.AddShortcut(new Shortcut("5", "shortcut 5", null)
            .AddAction("a1", "Action 1", null)
            .AddAction("a2", "Action 2", null)
            .AddAction("a3", "Action 3", null));
    }

    private void CamWinDef_OnUIWindowCreated(UIWindow camWindow)
    {
        _mainCam = (UICameraWindow)camWindow;
        _mainCam.Camera.GetComponent<MouseOrbitImproved>().Camera = _mainCam;
    }

    private void UImGuiUtility_Layout()
    {
        ImGui.ShowDemoWindow();
    }

    private void Update()
    {
        if (_mainCam == null || !_mainCam.IsInitialized)
        {
            return;
        }

        if (_mainCam.Mouse.IsDown(0) && !_mainCam.Mouse.IsHoverOverlay)
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
        LastPointObject.transform.position = hit.point;

        Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
        if (rb == null)
            return;
        rb.AddExplosionForce(hitForce, hit.point, 5f, 0f, ForceMode.Impulse);
    }
}