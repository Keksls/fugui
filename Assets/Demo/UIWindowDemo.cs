using UnityEngine;
using Fugui.Core;
using Fugui.Framework;
using System.Collections.Generic;
using System;
using Fugui;
using System.Linq;

/// <summary>
/// this sample show how to use UIWindow toolkit
/// </summary>
public class UIWindowDemo : MonoBehaviour
{
    public Camera cam1;
    public Texture2D TmpDebugTexture;
    public bool ShowImGuiDemoWindow = false;
    public bool ShowRaycastersDebug = false;
    public test3DRaycaster Raycaster;
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
            new ComboboxButtonItem("button 1"),
            new ComboboxButtonItem("button 2", UIElementSize.AutoSize),
            new ComboboxSameLineItem(),
            new ComboboxButtonItem("button 3", false),
            new ComboboxSeparatorItem(),
            new ComboboxButtonItem("button 4", new Vector2(32f, 32f)),
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
        string description = "A <color=red>red</color> <b>Bold TEXT</b>";

        // set demo Main menu
        MainMenu.RegisterItem(Icons.DragonflyLogo + " Files", null);

        DockingLayoutManager.OnDockLayoutReloaded += DockingLayoutManager_OnDockLayoutReloaded;

        MainMenu.RegisterItem("Layout", null);
        foreach (KeyValuePair<string, UIDockSpaceDefinition> layoutDefinition in DockingLayoutManager.Layouts)
        {
            string menuName = FuGui.AddSpacesBeforeUppercase(layoutDefinition.Key);
            if (!MainMenu.IsRegisteredItem(menuName))
            {
                MainMenu.RegisterItem(menuName, () => DockingLayoutManager.SetLayout(layoutDefinition.Value), "Layout");
            }
        }

        MainMenu.RegisterItem("Windows", null);
        foreach (FuguiWindows windowName in Enum.GetValues(typeof(FuguiWindows)))
        {
            if (windowName == FuguiWindows.None)
            {
                continue;
            }
            MainMenu.RegisterItem(windowName.ToString(), () => FuGui.CreateWindowAsync(windowName, null), "Windows");
        }

        MainMenu.RegisterItem("3D Windows", null);
        foreach (FuguiWindows windowName in Enum.GetValues(typeof(FuguiWindows)))
        {
            if (windowName == FuguiWindows.None)
            {
                continue;
            }
            MainMenu.RegisterItem("3D " + windowName.ToString(), () => FuGui.CreateWindowAsync(windowName, (window) => { FuGui.Add3DWindow(window); }, false), "3D Windows");
        }

        new UIWindowDefinition(FuguiWindows.DockSpaceManager, "DockSpace Manager", FuGui.DockSpaceManager);
        new UIWindowDefinition(FuguiWindows.WindowsDefinitionManager, "Windows Definition Manager", FuGui.WindowsDefinitionManager);

        new UIWindowDefinition(FuguiWindows.ToolBox, "Tool Box", debugWindow_UI, flags: UIWindowFlags.AllowMultipleWindow);
        void debugWindow_UI(UIWindow window)
        {
            using (new UIPanel("debugContainer"))
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

                    if (grid.Button("Mouse Pos and Down Text"))
                    {
                        Debug.Log("click !");
                    }
                    grid.DisableNextElement();
                    grid.Button("Mouse Pos and Down Text", UIButtonStyle.Highlight);
                }
                window.Container.ImGuiImage(TmpDebugTexture, new Vector2(128, 128));
            }
        }

        // add Tree Window
        new UIWindowDefinition(FuguiWindows.Tree, "Modals Demo", (window) =>
            {
                using (UILayout layout = new UILayout())
                {
                    if (layout.Button("Theme small"))
                    {
                        FuGui.ShowModal("Theme Manager", FuGui.DrawThemes, UIModalSize.Small);
                    }

                    if (layout.Button("Theme medium"))
                    {
                        FuGui.ShowModal("Theme Manager", FuGui.DrawThemes, UIModalSize.Medium);
                    }

                    if (layout.Button("Theme large"))
                    {
                        FuGui.ShowModal("Theme Manager", FuGui.DrawThemes, UIModalSize.Large);
                    }

                    if (layout.Button("Theme extra larger"))
                    {
                        FuGui.ShowModal("Theme Manager", FuGui.DrawThemes, UIModalSize.ExtraLarge);
                    }

                    if (layout.Button("Info modal", UIButtonStyle.Info))
                    {
                        FuGui.ShowInfo("This is an Information", () =>
                        {
                            using (UILayout layout = new UILayout())
                            {
                                layout.Text("This is a nomal text");
                                layout.Text("This is an info text", UITextStyle.Info);
                            }
                        }, UIModalSize.Medium);
                    }

                    if (layout.Button("Success modal", UIButtonStyle.Success))
                    {
                        FuGui.ShowSuccess("This is a Success", () =>
                        {
                            using (UILayout layout = new UILayout())
                            {
                                layout.Text("This is a nomal text");
                                layout.Text("This is a success text", UITextStyle.Success);
                            }
                        }, UIModalSize.Medium);
                    }

                    if (layout.Button("Warning modal", UIButtonStyle.Warning))
                    {
                        FuGui.ShowWarning("This is a Warning", () =>
                        {
                            using (UILayout layout = new UILayout())
                            {
                                layout.Text("This is a nomal text");
                                layout.Text("This is a warning text", UITextStyle.Warning);
                            }
                        }, UIModalSize.Medium);
                    }

                    if (layout.Button("Danger modal", UIButtonStyle.Danger))
                    {
                        FuGui.ShowDanger("This is a Danger", () =>
                        {
                            using (UILayout layout = new UILayout())
                            {
                                layout.Text("This is a nomal text");
                                layout.Text("This is a danger text", UITextStyle.Danger);
                            }
                        }, UIModalSize.Medium);
                    }
                }
            });

        // add Capture Window
        new UIWindowDefinition(FuguiWindows.Captures, "Notify Demo", (window) =>
        {
            using (UILayout layout = new UILayout())
            {
                layout.ComboboxEnum<AnchorLocation>("Notify Anchor", (anchor) =>
                {
                    FuGui.Settings.NotificationAnchorPosition = anchor;
                }, () => FuGui.Settings.NotificationAnchorPosition);
                layout.Separator();
                foreach (StateType type in Enum.GetValues(typeof(StateType)))
                {
                    if (layout.Button("Notify " + type, UIButtonStyle.GetStyleForState(type)))
                    {
                        FuGui.Notify(type.ToString(), "This is a test " + type + " small notification.", type);
                    }
                }
                layout.Separator();
                foreach (StateType type in Enum.GetValues(typeof(StateType)))
                {
                    if (layout.Button("Notify long " + type, UIButtonStyle.GetStyleForState(type)))
                    {
                        FuGui.Notify(type.ToString(), "This is a test " + type + " notification. it's a quite long text for a notification but I have to test that the text wrapping don't mess with my notification panel height calculation.", type);
                    }
                }
                layout.Separator();
                foreach (StateType type in Enum.GetValues(typeof(StateType)))
                {
                    if (layout.Button("Notify title " + type, UIButtonStyle.GetStyleForState(type)))
                    {
                        FuGui.Notify("this is a type " + type.ToString(), null, type);
                    }
                }
                layout.Separator();
                foreach (StateType type in Enum.GetValues(typeof(StateType)))
                {
                    if (layout.Button("Notify message " + type, UIButtonStyle.GetStyleForState(type)))
                    {
                        FuGui.Notify(null, "this is a type " + type.ToString(), type);
                    }
                }
            }
        });

        // add Metadata Window
        new UIWindowDefinition(FuguiWindows.Metadata, "Metadata", (window) =>
        {
            using (new UIPanel("mdcc"))
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
                        grid.Slider("slider float ena##NoDrag", ref floatVal, SliderFlags.NoDrag);
                        grid.Slider("slider float ena##LeftDrag", ref floatVal, SliderFlags.LeftDrag);
                        grid.DisableNextElement();
                        grid.Slider("slider float dis##NoDrag", ref intVal, SliderFlags.NoDrag);
                        grid.DisableNextElement();
                        grid.Slider("slider float dis##LeftDrag", ref intVal, SliderFlags.LeftDrag);

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
                                layout.Button("big button", UIButtonStyle.Highlight);
                                layout.Slider("sdlc1", ref intVal);
                                layout.DisableNextElement();
                                layout.Slider("sdlc2", ref intVal);
                                layout.Slider("sdlc3", ref floatVal);
                            });

                            grid.Combobox("test combobox", cbTexts, (newValue) => { Debug.Log(newValue); });
                            grid.Combobox("test button box", cbButtons, (newValue) => { Debug.Log(newValue); });
                        }
                    });
                }
            }
        });

        // add Inspector Window
        new UIWindowDefinition(FuguiWindows.Inspector, "Inspector", (window) =>
        {
            using (new UIPanel("demoContainer", UIStyle.Unpadded))
            {
                using (var layout = new UILayout())
                {
                    layout.Collapsable("Transform", () =>
                    {
                        using (UIGrid grid = new UIGrid("transformGrid", rowsPadding: 1f, outterPadding: 8f))
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
                        using (UIGrid grid = new UIGrid("cameraGrid", outterPadding: 8f))
                        {
                            grid.SetMinimumLineHeight(22f);
                            grid.SetNextElementToolTip("Clear flag of the main camera");
                            grid.ComboboxEnum<CameraClearFlags>("Clear Flags", (CameraClearFlags) =>
                            {
                                cam1.clearFlags = CameraClearFlags;
                            }, () => { return cam1.clearFlags; });

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

        // add Fugui Settings Window
        new UIWindowDefinition(FuguiWindows.FuguiSettings, "Fugui Settings", (window) =>
        {
            FuGui.DrawSettings();
        });

        // add main camera window
        UIWindowDefinition camWinDef = new UICameraWindowDefinition(FuguiWindows.MainCameraView, cam1, "3DView", null, flags: UIWindowFlags.NoInterractions)
            .SetCustomWindowType<UICameraWindow>();
        camWinDef.OnUIWindowCreated += CamWinDef_OnUIWindowCreated;

        #region Overlays
        // render graph panel
        UIOverlay rg = new UIOverlay("RenderGraphPanel", new Vector2(224f, 36f), (overlay) =>
        {
            using (UILayout layout = new UILayout())
            {
                layout.ComboboxEnum<AnchorLocation>("##rgCbTest", (val) => { });
                layout.SameLine();
                layout.Button(Icons.Ghost, UIElementSize.AutoSize);
                layout.SameLine();
                layout.Button(Icons.DataFusion, UIElementSize.AutoSize, UIButtonStyle.Highlight);
            };
        }, OverlayFlags.NoBackground | OverlayFlags.NoClose | OverlayFlags.NoMove);
        rg.AnchorWindowDefinition(camWinDef, AnchorLocation.TopLeft, Vector2.zero);

        // gizmos panel
        UIOverlay gz = new UIOverlay("GizmoPanel", new Vector2(46f, 36f), (overlay) =>
        {
            using (UILayout layout = new UILayout())
            {
                layout.Button(Icons.Gizmo, UIButtonStyle.Highlight);
            }
        }, OverlayFlags.NoClose | OverlayFlags.NoMove | OverlayFlags.NoBackground, OverlayDragPosition.Right);
        gz.AnchorWindowDefinition(camWinDef, AnchorLocation.TopRight, Vector2.zero);

        // legend
        UIOverlay bc = new UIOverlay("LegendPanel", new Vector2(128f, 128f), (overlay) =>
        {
            using (UILayout layout = new UILayout())
            {
                layout.Text("Legend 1");
                layout.Text("Legend 2");
                layout.Text("Legend 3");
                layout.Text("Legend 4");
                layout.Text("Legend 5");
            }
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
                layout.Button(Icons.EditShape, new Vector2(32f, 32f), UIButtonStyle.Highlight);
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
        int qualityIndex = 3;
        UIOverlay fastest = new UIOverlay("FastestF", new Vector2(278f, 36f), (overlay) =>
        {
            using (UILayout layout = new UILayout())
            {
                FuGui.PushFont(12, FontType.Regular);
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
                FuGui.PopFont();
            }
        }, OverlayFlags.Default, OverlayDragPosition.Bottom);
        fastest.AnchorWindowDefinition(camWinDef, AnchorLocation.BottomCenter, Vector2.zero);

        void drawCameraFPSOverlay(UICameraWindow cam)
        {
            using (UIGrid grid = new UIGrid("camFPS", new UIGridDefinition(2, new int[] { 42 }, responsiveMinWidth: 0)))
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
            FuGui.DefaultContext.OnRender += UImGuiUtility_Layout;
        }

        // set default layout (will create UIWindows)
        if (DockingLayoutManager.Layouts.Count > 0)
        {
            string firstKey = DockingLayoutManager.Layouts.Keys.ToList()[0];
            DockingLayoutManager.SetLayout(DockingLayoutManager.Layouts[firstKey]);
        }

    }

    private void DockingLayoutManager_OnDockLayoutReloaded()
    {
        //Unregistered menu and all children
        MainMenu.UnregisterItem("Layout");

        //Register the layout menu empty
        MainMenu.RegisterItem("Layout", null);

        foreach (KeyValuePair<string, UIDockSpaceDefinition> layoutDefinition in DockingLayoutManager.Layouts)
        {
            //Add new children
            string menuName = FuGui.AddSpacesBeforeUppercase(layoutDefinition.Key);
            if (!MainMenu.IsRegisteredItem(menuName))
            {
                MainMenu.RegisterItem(menuName, () => DockingLayoutManager.SetLayout(layoutDefinition.Value), "Layout");
            }
        }
    }

    private void CamWinDef_OnUIWindowCreated(UIWindow camWindow)
    {
        _mainCam = (UICameraWindow)camWindow;
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

        foreach (FuguiRaycaster ray in InputManager.GetAllRaycasters())
        {
            Ray r = ray.GetRay();
            Gizmos.DrawRay(r);
        }
    }
}