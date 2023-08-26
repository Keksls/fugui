using Fu;
using Fu.Core;
using Fu.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class WidgetsWindow : MonoBehaviour
{
    private bool _enableWidgets = true;
    // texts
    string richText = "a <color=red>red</color> <b>Bold TEXT</b>";
    string smallText = "a small text";
    string pathText = "";
    string filePathText = "";
    // toggle and checkbox
    bool toggleVal = false;
    bool boolVal = false;
    // ranges
    float min = 10f;
    float max = 20;
    // drag and sliders
    int intVal = 5;
    float floatVal = 5f;
    float floatValKnob = 5f;
    Vector2 v2Val = Vector2.zero;
    Vector3 v3Val = Vector3.zero;
    Vector4 v4Val = Vector4.zero;
    // combobox
    private enum myTestEnum
    {
        EnumValueA = 0,
        EnumValueB = 1,
        EnumValueC = 2,
        EnumValueD = 3,
        EnumValueE = 4,
    }
    myTestEnum _selectedEnumValue = myTestEnum.EnumValueC;
    List<string> cbTexts = new List<string>() { "cb1", "cb2", "cb3" };
    // radio buttons
    int radioButtonGroup1Index;
    int radioButtonGroup2Index;
    // color pickers
    Vector3 colorV3 = (Vector3)(Vector4)Color.yellow;
    Vector4 colorV4 = Color.blue;
    // date time pickers
    DateTime dateTime = DateTime.Now;
    // spinners
    float spinnerSize = 20f;
    int spinnerNbDots = 6;
    float spinnerDotsSize = 2f;
    bool spinnerDoubleColor = false;
    Vector2 spinnerV2Size = new Vector2(64f, 20f);
    float spinnerFrequency = 6f;

    void Awake()
    {
        registerUIWidgetsWindow();
    }

    private void registerUIWidgetsWindow()
    {
        new FuWindowDefinition(FuWindowsNames.Widgets, (window) =>
        {
            using (var layout = new FuLayout())
            {
                layout.CenterNextItem("Check Fugui's git page.");
                layout.Text("Check Fugui's ");
                layout.SameLine();
                layout.TextURL("git page.", "https://framagit.org/Hydrocode/fugui", FuTextStyle.Info);
                layout.Toggle("##toggleDisable", ref _enableWidgets, "Widgets Disabled", "Widgets Enabled", FuToggleFlags.MaximumTextSize);

                using (new FuPanel("widgetsDemoPanel", FuStyle.Unpadded))
                {
                    layout.Collapsable("Sliders", drawSliders);
                    layout.Collapsable("Basics Widgets", () => { drawBasics(layout); });
                    layout.Collapsable("Texts", () => { drawTexts(layout); });
                    layout.Collapsable("Buttons", () => { drawButtons(layout); });
                    layout.Collapsable("Drags", drawDrags);
                    layout.Collapsable("Progress Bars", drawProgressbar);
                    layout.Collapsable("Knobs", drawKnobs);
                    layout.Collapsable("Spinners", () =>
                    {
                        drawSpinners(layout);
                    });
                    layout.Collapsable("Lists", () => { drawBoxes(layout); });
                }
            }
        }, flags: FuWindowFlags.AllowMultipleWindow);
    }

    private void drawKnobs()
    {
        using (var grid = new FuGrid("gridKnobs"))
        {
            if (!_enableWidgets)
                grid.DisableNextElements();
            grid.Knob("knob Dot", ref floatValKnob, 0f, 100f, FuKnobVariant.Dot);
            grid.Knob("knob Space", ref floatValKnob, 0f, 100f, FuKnobVariant.Space);
            grid.Knob("knob WiperOnly", ref floatValKnob, 0f, 100f, FuKnobVariant.WiperOnly);
            grid.Knob("knob Space", ref floatValKnob, 0f, 100f, FuKnobVariant.Tick);
            grid.Knob("knob Wiper", ref floatValKnob, 0f, 100f, FuKnobVariant.Wiper);
            grid.Knob("knob Stepped", ref floatValKnob, 0f, 100f, FuKnobVariant.Stepped, 10, 10f, "%1.f");
            if (!_enableWidgets)
                grid.EnableNextElements();
        }
    }

    private void drawButtons(FuLayout layout)
    {
        using (var grid = new FuGrid("buttonsGrid"))
        {
            if (!_enableWidgets)
            {
                grid.DisableNextElements();
                layout.DisableNextElements();
            }

            grid.Button("Button");
            grid.Button("Selected", FuElementSize.AutoSize, FuButtonStyle.Selected);
            grid.SameLine();
            layout.Button("Highlight", FuElementSize.AutoSize, FuButtonStyle.Highlight);

            grid.Text("Info");
            grid.Button("Info", FuButtonStyle.Info);
            grid.Text("Success");
            grid.Button("Success", FuButtonStyle.Success);
            grid.Text("Danger");
            grid.Button("Danger", FuButtonStyle.Danger);
            grid.Text("Warning");
            grid.Button("Warning", FuButtonStyle.Warning);

            grid.Separator();

            grid.ButtonsGroup<FuToggleFlags>("Buttons Group", (flag) => { Debug.Log(flag + " selected"); });
            grid.SetNextElementToolTip("About", "Accelerate", "Arch", "Arrow Down");
            grid.ButtonsGroup("Default", new List<string>() { Icons.About + "##0", Icons.Accelerate + "##0", Icons.Arch + "##0", Icons.ArrowDown + "##0" }, (index) => { }, null, 0f, FuButtonsGroupFlags.Default);
            grid.ButtonsGroup("Left", new List<string>() { Icons.About + "##1", Icons.Accelerate + "##1", Icons.Arch + "##1", Icons.ArrowDown + "##1" }, (index) => { }, null, 0f, FuButtonsGroupFlags.AlignLeft);
            grid.ButtonsGroup("Auto size", new List<string>() { Icons.About + "##2", Icons.Accelerate + "##2", Icons.Arch + "##2", Icons.ArrowDown + "##2" }, (index) => { }, null, 0f, FuButtonsGroupFlags.AutoSizeButtons);
            grid.ButtonsGroup("Left and auto size", new List<string>() { Icons.About + "##3", Icons.Accelerate + "##3", Icons.Arch + "##3", Icons.ArrowDown + "##3" }, (index) => { }, null, 0f, FuButtonsGroupFlags.AlignLeft | FuButtonsGroupFlags.AutoSizeButtons);

            if (!_enableWidgets)
            {
                grid.EnableNextElements();
                layout.EnableNextElements();
            }
        }
    }

    private void drawBasics(FuLayout layout)
    {
        layout.Tabs("basicWidgetsTabs", new string[] { "Checkbox and Toggles", "Images", "ColorPicker", "RadioButtons", "DatePicker" }, (index) =>
        {
            using (var grid = new FuGrid("basicWidgetsGrid"))
            {
                if (!_enableWidgets)
                    grid.DisableNextElements();
                switch (index)
                {
                    // checkbox and toggles
                    default:
                    case 0:
                        grid.CheckBox("checkbox", ref boolVal);
                        grid.Toggle("Toggle nude", ref toggleVal);
                        grid.Toggle("Toggle On/Off", ref toggleVal, "OFF", "ON", FuToggleFlags.AlignLeft);
                        grid.Toggle("Auto text size", ref toggleVal, "sm txt", "this is large text", FuToggleFlags.AlignLeft);
                        grid.Toggle("Max text size", ref toggleVal, "sm txt", "this is large text", FuToggleFlags.MaximumTextSize);
                        grid.DisableAnimationsFromNow();
                        grid.Toggle("No Animation", ref toggleVal, "No", "Anim");
                        grid.EnableAnimationsFromNow();
                        break;

                    // Images
                    case 1:
                        if (grid.Image("clickable", Fugui.Settings.FuguiLogo, Vector2.one * 32f))
                        {
                            Debug.Log("Image clicked");
                        }
                        grid.Image("with border", Fugui.Settings.FuguiLogo, Vector2.one * 32f, true);
                        grid.Image("32 x 32", Fugui.Settings.FuguiLogo, Vector2.one * 32f);
                        grid.Image("64 x 64 (red)", Fugui.Settings.FuguiLogo, Vector2.one * 64f, Color.red);
                        grid.Image("128 x 128", Fugui.Settings.FuguiLogo, Vector2.one * 128f);
                        grid.Separator();
                        grid.ImageButton("Image button", Fugui.Settings.FuguiLogo, Vector2.one * 32f);
                        grid.ImageButton("Image button blue", Fugui.Settings.FuguiLogo, Vector2.one * 32f, Color.blue);
                        break;

                    case 2:
                        grid.ColorPicker("picker", ref colorV3);
                        grid.ColorPicker("picker with alpha", ref colorV4);
                        grid.Separator();
                        grid.Text("direct picker");
                        grid.NextColumn();
                        Fugui.Colorpicker("testPicker", ref colorV4);
                        break;

                    case 3:
                        if (layout.RadioButton("c 1", radioButtonGroup1Index == 0))
                        {
                            radioButtonGroup1Index = 0;
                        }
                        layout.SameLine();
                        if (layout.RadioButton("c 2", radioButtonGroup1Index == 1))
                        {
                            radioButtonGroup1Index = 1;
                        }
                        layout.SameLine();
                        if (layout.RadioButton("c 3", radioButtonGroup1Index == 2))
                        {
                            radioButtonGroup1Index = 2;
                        }
                        layout.SameLine();
                        if (layout.RadioButton("c 4", radioButtonGroup1Index == 3))
                        {
                            radioButtonGroup1Index = 3;
                        }

                        if (layout.RadioButton("c 1##1", radioButtonGroup2Index == 0))
                        {
                            radioButtonGroup2Index = 0;
                        }
                        if (layout.RadioButton("c 2##1", radioButtonGroup2Index == 1))
                        {
                            radioButtonGroup2Index = 1;
                        }
                        if (layout.RadioButton("c 3##1", radioButtonGroup2Index == 2))
                        {
                            radioButtonGroup2Index = 2;
                        }
                        if (layout.RadioButton("c 4##1", radioButtonGroup2Index == 3))
                        {
                            radioButtonGroup2Index = 3;
                        }
                        break;

                    case 4:
                        grid.DateTimePickerPopup("dtPicker", ref dateTime);
                        break;
                }
                if (!_enableWidgets)
                    grid.EnableNextElements();
            }
        });
    }

    private void drawBoxes(FuLayout layout)
    {
        using (var grid = new FuGrid("listsGrid"))
        {
            if (!_enableWidgets)
            {
                grid.DisableNextElements();
                layout.DisableNextElements();
            }
            grid.Combobox("test callback combo", "click me custom", () =>
            {
                layout.CheckBox("chdk1", ref boolVal);
                layout.Drag("drdag", ref floatVal);
                layout.Button("big button");
                layout.DisableNextElement();
                layout.Button("big button", FuButtonStyle.Highlight);
                layout.Slider("sdlc1", ref intVal);
                layout.DisableNextElement();
                layout.Slider("sdlc2", ref intVal);
                layout.Slider("sdlc3", ref floatVal);
            });
            grid.Combobox("test combobox", cbTexts, (newValue) => { Debug.Log(newValue); });
            grid.ComboboxEnum<myTestEnum>("Enum combobox", (index) => { _selectedEnumValue = (myTestEnum)index; }, () => _selectedEnumValue);

            layout.Separator();

            grid.ListBox("test callback combo", () =>
            {
                layout.CheckBox("chdk1", ref boolVal);
                layout.Drag("drdag", ref floatVal);
                layout.Button("big button");
                layout.DisableNextElement();
                layout.Button("big button", FuButtonStyle.Highlight);
                layout.Slider("sdlc1", ref intVal);
                layout.DisableNextElement();
                layout.Slider("sdlc2", ref intVal);
                layout.Slider("sdlc3", ref floatVal);
            });
            grid.ListBox("test combobox", cbTexts, (newValue) => { Debug.Log(newValue); });
            if (!_enableWidgets)
            {
                grid.EnableNextElements();
                layout.EnableNextElements();
            }
        }
    }

    private void drawDrags()
    {
        using (var grid = new FuGrid("dragsGrid"))
        {
            if (!_enableWidgets)
                grid.DisableNextElements();
            grid.Drag("drag int##dint", ref intVal, "%d rad");
            grid.Drag("drag float##1", ref floatVal, "value", 0f, 360f, 0.1f, "%.1f°");
            grid.Drag("drag float##2", ref floatVal, "%", 0f, 360f, 1f, "%.0f%%");
            grid.Drag("drag v2", ref v2Val, "x", "y");
            grid.Drag("drag v3", ref v3Val, "x", "y", "z");
            grid.Drag("drag v4", ref v4Val, "r", "g", "b", "a");
            if (!_enableWidgets)
                grid.EnableNextElements();
        }
    }

    private void drawTexts(FuLayout layout)
    {
        if (!_enableWidgets)
            layout.DisableNextElements();

        layout.Text("Default text");
        layout.Text("Highlight text", FuTextStyle.Highlight);
        layout.Text("Selected text", FuTextStyle.Selected);
        layout.Text("Info text", FuTextStyle.Info);
        layout.Text("Success text", FuTextStyle.Success);
        layout.Text("Warning text", FuTextStyle.Warning);
        layout.Text("Danger text", FuTextStyle.Danger);
        layout.Separator();

        layout.TextURL("This is a hyperkling text URL", "https://framagit.org/Hydrocode/fugui");

        layout.Text("This text is wrapped because its too long.This text is wrapped because its too long.This text is wrapped because its too long.", FuTextWrapping.Wrapp);

        layout.FramedText("This is a frammed text");

        layout.SetNextElementToolTipWithLabel("This is a 128px clipped text, it's clipped");
        layout.Text("This is a 128px clipped text, it's clipped", new Vector2(128f, 0f), FuTextWrapping.Clip);

        if (layout.ClickableText("This text can be clicked"))
        {
            Debug.Log("Clicked !");
        }

        layout.SmartText("This is a smart <b>Bold <color=red>red</color></b> text.");

        using (var grid = new FuGrid("dragsGrid"))
        {
            if (!_enableWidgets)
                grid.DisableNextElements();
            grid.InputFolder("Folder Path", (path) => pathText = path, pathText);
            grid.InputFile("File Path", (path) => filePathText = path, filePathText);

            grid.TextInput("small text", "some placeholder", ref smallText);
            grid.TextInput("password", ref smallText, 0f, FuInputTextFlags.Password);

            grid.TextInput("multiline rich text", "", ref richText, 4096, 64f, FuFrameStyle.Default, 0f, FuInputTextFlags.Default);
            if (!_enableWidgets)
                grid.EnableNextElements();
        }
        layout.SmartText(richText);
        if (!_enableWidgets)
            layout.EnableNextElements();
    }

    private void drawSpinners(FuLayout layout)
    {
        using (var grid = new FuGrid("gSPN", FuGridFlag.LinesBackground))
        {
            if (!_enableWidgets)
                grid.DisableNextElements();
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
            if (!_enableWidgets)
                grid.EnableNextElements();
        }
    }

    private void drawProgressbar()
    {
        using (var grid = new FuGrid("progressBarGrid"))
        {
            if (!_enableWidgets)
                grid.DisableNextElements();
            grid.ProgressBar("pb in", floatVal / 100f, ProgressBarTextPosition.Inside);
            grid.ProgressBar("pb left", floatVal / 100f, ProgressBarTextPosition.Left);
            grid.ProgressBar("pb right", floatVal / 100f, ProgressBarTextPosition.Right);
            grid.ProgressBar("pb none", floatVal / 100f, ProgressBarTextPosition.None);
            grid.ProgressBar("pb in small", floatVal / 100f, new Vector2(-1f, 8f), ProgressBarTextPosition.Inside);
            grid.ProgressBar("pb no small", floatVal / 100f, new Vector2(-1f, 8f), ProgressBarTextPosition.None);
            grid.ProgressBar("pb idle", new Vector2(-1f, 8f));
            if (!_enableWidgets)
                grid.EnableNextElements();
        }
    }

    private void drawSliders()
    {
        using (var grid = new FuGrid("slidersGrid"))
        {
            if (!_enableWidgets)
                grid.DisableNextElements();
            grid.Slider("slider int", ref intVal);
            grid.Slider("slider", ref floatVal);
            grid.Slider("slider no drag##NoDrag", ref floatVal, 0.5f, FuSliderFlags.NoDrag);
            grid.Slider("slider left drag##LeftDrag", ref floatVal, 0.5f, FuSliderFlags.LeftDrag);
            grid.Range("range", ref min, ref max, 0f, 30f, 0.25f);
            grid.Range("range no drag", ref min, ref max, 0f, 30f, 0.1f, FuSliderFlags.NoDrag);
            if (!_enableWidgets)
                grid.EnableNextElements();
        }
    }
}