using Fu;
using Fu.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents the Widgets Window type.
/// </summary>
public class WidgetsWindow : FuWindowBehaviour
{
    #region State
    private bool _enableWidgets = true;
    // texts
    private string richText = "a <color=red>red</color> <b>Bold TEXT</b>";
    private string smallText = "a small text";
    private string pathText = "";
    private string filePathText = "";
    // toggle and checkbox
    private bool toggleVal = false;
    private bool boolVal = false;
    // ranges
    private float min = 10f;
    private float max = 20;
    // drag and sliders
    private int intVal = 5;
    private float floatVal = 5f;
    private float floatValKnob = 5f;
    private Vector2 v2Val = Vector2.zero;
    private Vector3 v3Val = Vector3.zero;
    private Vector4 v4Val = Vector4.zero;
    #endregion

    #region Nested Types
    // combobox

    /// <summary>
    /// Lists the available my Test Enum values.
    /// </summary>
    private enum myTestEnum
    {
        EnumValueA = 0,
        EnumValueB = 1,
        EnumValueC = 2,
        EnumValueD = 3,
        EnumValueE = 4,
    }

    /// <summary>
    /// Represents one row displayed by the SearchBox and TableView demo.
    /// </summary>
    private class WidgetTableDemoItem
    {
        #region State
        public string Name { get; }
        public string Category { get; }
        public int Count { get; }
        public float Weight { get; }
        public bool Enabled { get; }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a demo item row for the table view section.
        /// </summary>
        /// <param name="name">The widget display name.</param>
        /// <param name="category">The widget category.</param>
        /// <param name="count">The sample count value.</param>
        /// <param name="weight">The sample weight value.</param>
        /// <param name="enabled">Whether the row is shown as enabled.</param>
        public WidgetTableDemoItem(string name, string category, int count, float weight, bool enabled)
        {
            Name = name;
            Category = category;
            Count = count;
            Weight = weight;
            Enabled = enabled;
        }
        #endregion
    }
    #endregion

    #region State
    private myTestEnum _selectedEnumValue = myTestEnum.EnumValueC;
    private List<string> cbTexts = new List<string>() { "cb1", "cb2", "cb3" };
    // radio buttons
    private int radioButtonGroup1Index;
    private int radioButtonGroup2Index;
    // color pickers
    private Vector3 colorV3 = (Vector3)(Vector4)Color.yellow;
    private Vector4 colorV4 = Color.blue;
    // date time pickers
    private DateTime dateTime = DateTime.Now;
    // spinners
    private float spinnerSize = 32f;
    private int spinnerNbDots = 8;
    private float spinnerDotsSize = 2.4f;
    private float spinnerRingThickness = 3f;
    private int spinnerBars = 5;
    private bool spinnerDoubleColor = false;
    private Vector2 spinnerV2Size = new Vector2(96f, 24f);
    private float spinnerFrequency = 6f;
    // search and table view
    private FuSearchFilter tableDemoFilter = new FuSearchFilter();
    private int selectedTableDemoIndex = -1;
    private List<WidgetTableDemoItem> tableDemoItems = new List<WidgetTableDemoItem>()
    {
        new WidgetTableDemoItem("SearchBox", "Input", 1, 0.15f, true),
        new WidgetTableDemoItem("TableView", "Data", 5, 1.20f, true),
        new WidgetTableDemoItem("Buttons", "Action", 12, 0.40f, true),
        new WidgetTableDemoItem("Sliders", "Numeric", 8, 0.50f, true),
        new WidgetTableDemoItem("Lists", "Selection", 4, 0.75f, true),
        new WidgetTableDemoItem("Spinners", "Feedback", 14, 0.25f, false),
        new WidgetTableDemoItem("Path Fields", "Files", 2, 0.90f, true),
        new WidgetTableDemoItem("Date Picker", "Input", 1, 0.65f, false),
    };
    private List<FuTableViewColumn<WidgetTableDemoItem>> tableDemoColumns;
    // charts
    private List<FuChartSeries> chartDemoSeries;
    private FuChartOptions chartDemoOptions;
    private bool chartDemoShowLegend = true;
    private bool chartDemoShowGrid = true;
    private bool chartDemoShowTooltip = true;
    private bool chartDemoShowCrosshair = true;
    private float chartDemoHeight = 280f;
    private float chartDemoThreshold = 0.65f;
    private int chartDemoMaxPoints = 512;
    #endregion

    #region Methods
    /// <summary>
    /// Handles the UI event.
    /// </summary>
    /// <param name="window">The window value.</param>
    /// <param name="layout">The layout value.</param>
    public override void OnUI(FuWindow window, FuLayout layout)
    {
        layout.CenterNextItemH("Check Fugui's git page.");
        layout.Text("Check Fugui's ");
        layout.SameLine();
        layout.TextURL("git page.", "https://framagit.org/Hydrocode/fugui");
        layout.Toggle("##toggleDisable", ref _enableWidgets, "Widgets Disabled", "Widgets Enabled", FuToggleFlags.MaximumTextSize);

        using (new FuPanel("widgetsDemoPanel", FuStyle.Unpadded))
        {
            layout.Collapsable("Sliders", drawSliders);
            layout.Collapsable("Basics Widgets", () => { drawBasics(layout); }, 0);
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
            layout.Collapsable("Search and Table View", () => { drawSearchAndTableView(layout); });
            layout.Collapsable("Charts", () => { drawCharts(layout); });
        }
    }

    /// <summary>
    /// Runs the draw knobs workflow.
    /// </summary>
    private void drawKnobs()
    {
        using (var grid = new FuGrid("gridKnobs"))
        {
            if (!_enableWidgets)
                grid.DisableNextElements();
            grid.Knob("knob Dot", ref floatValKnob, 0f, 100f, FuKnobVariant.Dot);
            grid.Knob("knob Space", ref floatValKnob, 0f, 100f, FuKnobVariant.Space);
            grid.Knob("knob WiperOnly", ref floatValKnob, 0f, 100f, FuKnobVariant.WiperOnly);
            grid.Knob("knob Tick", ref floatValKnob, 0f, 100f, FuKnobVariant.Tick);
            grid.Knob("knob Wiper", ref floatValKnob, 0f, 100f, FuKnobVariant.Wiper);
            grid.Knob("knob Stepped", ref floatValKnob, 0f, 100f, FuKnobVariant.Stepped, 10, 10f, "%1.f");
            if (!_enableWidgets)
                grid.EnableNextElements();
        }
    }

    /// <summary>
    /// Runs the draw buttons workflow.
    /// </summary>
    /// <param name="layout">The layout value.</param>
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
            grid.ButtonsGroup("Default", new List<string>() { Icons.InfoCircle + "##0", Icons.Light + "##0", Icons.Arch + "##0", Icons.Alignment + "##0" }, (index) => { }, null, 0f, FuButtonsGroupFlags.Default);
            grid.ButtonsGroup("Left", new List<string>() { Icons.InfoCircle + "##1", Icons.Light + "##1", Icons.Arch + "##1", Icons.Alignment + "##1" }, (index) => { }, null, 0f, FuButtonsGroupFlags.AlignLeft);
            grid.ButtonsGroup("Auto size", new List<string>() { Icons.InfoCircle + "##2", Icons.Light + "##2", Icons.Arch + "##2", Icons.Alignment + "##2" }, (index) => { }, null, 0f, FuButtonsGroupFlags.AutoSizeButtons);
            grid.ButtonsGroup("Left and auto size", new List<string>() { Icons.InfoCircle + "##3", Icons.Light + "##3", Icons.Arch + "##3", Icons.Alignment + "##3" }, (index) => { }, null, 0f, FuButtonsGroupFlags.AlignLeft | FuButtonsGroupFlags.AutoSizeButtons);

            if (!_enableWidgets)
            {
                grid.EnableNextElements();
                layout.EnableNextElements();
            }
        }
    }

    /// <summary>
    /// Runs the draw basics workflow.
    /// </summary>
    /// <param name="layout">The layout value.</param>
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
                        grid.ImageButton("Image button", Fugui.Settings.FuguiLogo, Vector2.one * 32f, Vector2.zero);
                        grid.ImageButton("Image button blue", Fugui.Settings.FuguiLogo, Vector2.one * 32f, Color.blue, Vector2.zero, false);
                        break;

                    case 2:
                        grid.ColorPicker("picker", ref colorV3);
                        grid.ColorPicker("picker with alpha", ref colorV4);
                        break;

                    case 3:
                        if (grid.RadioButton("c 1", radioButtonGroup1Index == 0))
                        {
                            radioButtonGroup1Index = 0;
                        }
                        grid.SameLine();
                        if (grid.RadioButton("c 2", radioButtonGroup1Index == 1))
                        {
                            radioButtonGroup1Index = 1;
                        }
                        grid.SameLine();
                        if (grid.RadioButton("c 3", radioButtonGroup1Index == 2))
                        {
                            radioButtonGroup1Index = 2;
                        }
                        grid.SameLine();
                        if (grid.RadioButton("c 4", radioButtonGroup1Index == 3))
                        {
                            radioButtonGroup1Index = 3;
                        }

                        if (grid.RadioButton("c 1##1", radioButtonGroup2Index == 0))
                        {
                            radioButtonGroup2Index = 0;
                        }
                        if (grid.RadioButton("c 2##1", radioButtonGroup2Index == 1))
                        {
                            radioButtonGroup2Index = 1;
                        }
                        if (grid.RadioButton("c 3##1", radioButtonGroup2Index == 2))
                        {
                            radioButtonGroup2Index = 2;
                        }
                        if (grid.RadioButton("c 4##1", radioButtonGroup2Index == 3))
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

    /// <summary>
    /// Runs the draw boxes workflow.
    /// </summary>
    /// <param name="layout">The layout value.</param>
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

    /// <summary>
    /// Runs the draw search and table view workflow.
    /// </summary>
    /// <param name="layout">The layout value.</param>
    private void drawSearchAndTableView(FuLayout layout)
    {
        if (!_enableWidgets)
        {
            layout.DisableNextElements();
        }

        layout.SearchBox("tableDemoSearch", tableDemoFilter, "Search widget, category or state...");
        layout.Spacing();

        bool selectionChanged = layout.TableView(
            "widgetTableDemo",
            tableDemoItems,
            getTableDemoColumns(),
            ref selectedTableDemoIndex,
            tableDemoFilter.Query,
            getTableDemoSearchText,
            220f,
            FuTableViewFlags.Default | FuTableViewFlags.ScrollY | FuTableViewFlags.UseClipper | FuTableViewFlags.NoSavedSettings);

        if (selectionChanged && selectedTableDemoIndex >= 0 && selectedTableDemoIndex < tableDemoItems.Count)
        {
            Debug.Log("Selected table demo item: " + tableDemoItems[selectedTableDemoIndex].Name);
        }

        layout.Spacing();
        drawSelectedTableDemoItem(layout);

        if (!_enableWidgets)
        {
            layout.EnableNextElements();
        }
    }

    /// <summary>
    /// Gets the cached columns used by the table view demo.
    /// </summary>
    /// <returns>The table view demo columns.</returns>
    private List<FuTableViewColumn<WidgetTableDemoItem>> getTableDemoColumns()
    {
        if (tableDemoColumns == null)
        {
            tableDemoColumns = new List<FuTableViewColumn<WidgetTableDemoItem>>()
            {
                new FuTableViewColumn<WidgetTableDemoItem>("Widget", item => item.Name, 0f, sortComparison: (left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase)),
                new FuTableViewColumn<WidgetTableDemoItem>("Category", item => item.Category, 96f, sortComparison: (left, right) => string.Compare(left.Category, right.Category, StringComparison.OrdinalIgnoreCase)),
                new FuTableViewColumn<WidgetTableDemoItem>("Count", item => item.Count.ToString(), 64f, sortComparison: (left, right) => left.Count.CompareTo(right.Count)),
                new FuTableViewColumn<WidgetTableDemoItem>("Weight", item => item.Weight.ToString("0.00"), 72f, sortComparison: (left, right) => left.Weight.CompareTo(right.Weight)),
                FuTableViewColumn<WidgetTableDemoItem>.Custom("State", drawTableDemoStateCell, 86f, sortComparison: (left, right) => left.Enabled.CompareTo(right.Enabled), searchGetter: getTableDemoStateSearchText),
            };
        }

        return tableDemoColumns;
    }

    /// <summary>
    /// Gets the full searchable text for one demo table item.
    /// </summary>
    /// <param name="item">The table item value.</param>
    /// <returns>All searchable terms for the row.</returns>
    private string getTableDemoSearchText(WidgetTableDemoItem item)
    {
        return item.Name + " " + item.Category + " " + item.Count + " " + item.Weight.ToString("0.00") + " " + getTableDemoStateSearchText(item);
    }

    /// <summary>
    /// Gets the searchable state text for one demo table item.
    /// </summary>
    /// <param name="item">The table item value.</param>
    /// <returns>The searchable state terms.</returns>
    private string getTableDemoStateSearchText(WidgetTableDemoItem item)
    {
        return item.Enabled ? "enabled active on" : "disabled inactive off";
    }

    /// <summary>
    /// Draws the custom state cell for the table view demo.
    /// </summary>
    /// <param name="item">The table item value.</param>
    /// <param name="layout">The layout value.</param>
    private void drawTableDemoStateCell(WidgetTableDemoItem item, FuLayout layout)
    {
        layout.Text(item.Enabled ? "Enabled" : "Disabled", item.Enabled ? FuTextStyle.Success : FuTextStyle.Deactivated);
    }

    /// <summary>
    /// Draws the currently selected table item summary.
    /// </summary>
    /// <param name="layout">The layout value.</param>
    private void drawSelectedTableDemoItem(FuLayout layout)
    {
        if (selectedTableDemoIndex < 0 || selectedTableDemoIndex >= tableDemoItems.Count)
        {
            layout.Text("No table row selected.", FuTextStyle.Deactivated);
            return;
        }

        WidgetTableDemoItem selectedItem = tableDemoItems[selectedTableDemoIndex];
        using (var grid = new FuGrid("selectedTableDemoGrid", FuGridDefinition.DefaultFixed, FuGridFlag.LinesBackground))
        {
            // TableView selection stores the original source index, even after filtering and sorting.
            grid.Text("Selected index");
            grid.Text(selectedTableDemoIndex.ToString());
            grid.Text("Widget");
            grid.Text(selectedItem.Name);
            grid.Text("Category");
            grid.Text(selectedItem.Category);
            grid.Text("State");
            grid.Text(selectedItem.Enabled ? "Enabled" : "Disabled", selectedItem.Enabled ? FuTextStyle.Success : FuTextStyle.Deactivated);
        }
    }

    /// <summary>
    /// Runs the draw charts workflow.
    /// </summary>
    /// <param name="layout">The layout value.</param>
    private void drawCharts(FuLayout layout)
    {
        ensureChartDemoData();

        if (!_enableWidgets)
        {
            layout.DisableNextElements();
        }

        using (var grid = new FuGrid("chartsControlGrid", FuGridDefinition.DefaultFixed, FuGridFlag.LinesBackground))
        {
            if (!_enableWidgets)
            {
                grid.DisableNextElements();
            }

            grid.Toggle("Legend", ref chartDemoShowLegend);
            grid.Toggle("Grid", ref chartDemoShowGrid);
            grid.Toggle("Tooltip", ref chartDemoShowTooltip);
            grid.Toggle("Crosshair", ref chartDemoShowCrosshair);
            grid.Slider("Height", ref chartDemoHeight, 180f, 420f);
            grid.Slider("Max points", ref chartDemoMaxPoints, 64, 4096);
            grid.Slider("Threshold", ref chartDemoThreshold, -1f, 1f);

            if (!_enableWidgets)
            {
                grid.EnableNextElements();
            }
        }

        applyChartDemoOptions();

        FuChartHoverState hover;
        layout.Chart("chartDemo", chartDemoSeries, chartDemoOptions, out hover);
        layout.Spacing();
        drawChartDemoHover(layout, hover);

        if (!_enableWidgets)
        {
            layout.EnableNextElements();
        }
    }

    /// <summary>
    /// Creates the reusable data and options for the chart demo.
    /// </summary>
    private void ensureChartDemoData()
    {
        if (chartDemoSeries != null)
        {
            return;
        }

        List<Vector2> linePoints = new List<Vector2>(180);
        List<Vector2> areaPoints = new List<Vector2>(180);
        List<Vector2> barPoints = new List<Vector2>(24);
        List<Vector2> scatterPoints = new List<Vector2>(36);

        for (int i = 0; i < 180; i++)
        {
            float x = i;
            float wave = Mathf.Sin(i * 0.08f) * 0.65f + Mathf.Cos(i * 0.025f) * 0.35f;
            linePoints.Add(new Vector2(x, wave));
            areaPoints.Add(new Vector2(x, Mathf.Sin(i * 0.045f) * 0.42f + 0.35f));
        }

        for (int i = 0; i < 24; i++)
        {
            barPoints.Add(new Vector2(i * 7.5f, Mathf.Abs(Mathf.Sin(i * 0.55f)) * 0.9f + 0.12f));
        }

        for (int i = 0; i < 36; i++)
        {
            float x = 4f + i * 4.8f;
            scatterPoints.Add(new Vector2(x, Mathf.Sin(i * 0.73f) * 0.9f));
        }

        chartDemoSeries = new List<FuChartSeries>()
        {
            FuChartSeries.Custom("Custom band", drawChartDemoCustomBand),
            new FuChartSeries("Signal", linePoints, FuChartSeriesType.Line)
            {
                LineThickness = 2.25f,
                ShowPoints = false,
            },
            new FuChartSeries("Envelope", areaPoints, FuChartSeriesType.Area)
            {
                LineThickness = 1.5f,
                FillAlpha = 0.20f,
                Baseline = 0f,
            },
            new FuChartSeries("Volume", barPoints, FuChartSeriesType.Bar)
            {
                FillAlpha = 0.72f,
                Baseline = 0f,
                BarRounding = 3f,
            },
            new FuChartSeries("Events", scatterPoints, FuChartSeriesType.Scatter)
            {
                PointRadius = 3.25f,
            },
        };

        chartDemoOptions = new FuChartOptions()
        {
            Size = new FuElementSize(-1f, chartDemoHeight),
            MaxRenderedPointsPerSeries = chartDemoMaxPoints,
            AfterPlotDraw = drawChartDemoThresholdLine,
        };
        chartDemoOptions.XAxis.Label = "Frame";
        chartDemoOptions.XAxis.SetRange(0f, 179f);
        chartDemoOptions.YAxis.Label = "Value";
        chartDemoOptions.YAxis.SetAutoRange(true, 0.08f);
        chartDemoOptions.YAxis.ValueFormat = "0.00";
    }

    /// <summary>
    /// Applies the live demo controls to the chart options.
    /// </summary>
    private void applyChartDemoOptions()
    {
        FuChartFlags flags = FuChartFlags.Default;
        if (!chartDemoShowLegend)
        {
            flags &= ~FuChartFlags.Legend;
        }
        if (!chartDemoShowGrid)
        {
            flags &= ~FuChartFlags.Grid;
        }
        if (!chartDemoShowTooltip)
        {
            flags &= ~FuChartFlags.Tooltip;
        }
        if (!chartDemoShowCrosshair)
        {
            flags &= ~FuChartFlags.Crosshair;
        }

        chartDemoOptions.Flags = flags;
        chartDemoOptions.Size = new FuElementSize(-1f, chartDemoHeight);
        chartDemoOptions.MaxRenderedPointsPerSeries = chartDemoMaxPoints;
    }

    /// <summary>
    /// Draws the custom shaded band series used by the chart demo.
    /// </summary>
    /// <param name="context">The chart draw context.</param>
    /// <param name="series">The custom chart series.</param>
    private void drawChartDemoCustomBand(FuChartDrawContext context, FuChartSeries series)
    {
        Vector2 min = context.ToScreen(new Vector2(62f, context.Min.y));
        Vector2 max = context.ToScreen(new Vector2(92f, context.Max.y));
        // Custom series draw directly in the plot drawlist and still benefits from chart clipping.
        context.DrawList.AddRectFilled(new Vector2(min.x, max.y), new Vector2(max.x, min.y), Fugui.Themes.GetColorU32(FuColors.TextInfo, 0.10f));
        context.DrawList.AddRect(new Vector2(min.x, max.y), new Vector2(max.x, min.y), Fugui.Themes.GetColorU32(FuColors.TextInfo, 0.35f));
    }

    /// <summary>
    /// Draws a custom threshold overlay after all chart series have rendered.
    /// </summary>
    /// <param name="context">The chart draw context.</param>
    private void drawChartDemoThresholdLine(FuChartDrawContext context)
    {
        Vector2 left = context.ToScreen(new Vector2(context.Min.x, chartDemoThreshold));
        Vector2 right = context.ToScreen(new Vector2(context.Max.x, chartDemoThreshold));
        uint color = Fugui.Themes.GetColorU32(FuColors.TextWarning, 0.85f);
        context.DrawList.AddLine(left, right, color, 1.5f * context.Scale);
        context.DrawList.AddText(right + new Vector2(-72f * context.Scale, -18f * context.Scale), color, "Threshold");
    }

    /// <summary>
    /// Draws the hover readout returned by the chart widget.
    /// </summary>
    /// <param name="layout">The layout value.</param>
    /// <param name="hover">The current chart hover state.</param>
    private void drawChartDemoHover(FuLayout layout, FuChartHoverState hover)
    {
        if (!hover.HasPoint)
        {
            layout.Text("No chart point hovered.", FuTextStyle.Deactivated);
            return;
        }

        using (var grid = new FuGrid("chartHoverGrid", FuGridDefinition.DefaultFixed, FuGridFlag.LinesBackground))
        {
            grid.Text("Series");
            grid.Text(hover.SeriesLabel);
            grid.Text("Point");
            grid.Text(hover.PointIndex.ToString());
            grid.Text("Value");
            grid.Text(hover.Value.x.ToString("0.##") + " / " + hover.Value.y.ToString("0.00"));
        }
    }

    /// <summary>
    /// Runs the draw drags workflow.
    /// </summary>
    private void drawDrags()
    {
        using (var grid = new FuGrid("dragsGrid"))
        {
            if (!_enableWidgets)
                grid.DisableNextElements();
            grid.Drag("drag int##dint", ref intVal, "%d rad");
            grid.Drag("drag float##1", ref floatVal, "value", 0f, 360f, 0.1f, "%.1f�");
            grid.Drag("drag float##2", ref floatVal, "%", 0f, 360f, 1f, "%.0f%%");
            grid.Drag("drag v2", ref v2Val, "x", "y");
            grid.Drag("drag v3", ref v3Val, "x", "y", "z");
            grid.Drag("drag v4", ref v4Val, "r", "g", "b", "a");
            if (!_enableWidgets)
                grid.EnableNextElements();
        }
    }

    /// <summary>
    /// Runs the draw texts workflow.
    /// </summary>
    /// <param name="layout">The layout value.</param>
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

        layout.Text("This text is wrapped because its too long.This text is wrapped because its too long.This text is wrapped because its too long.", FuTextWrapping.Wrap);

        layout.FramedText("This is a frammed text");

        layout.SetNextElementToolTipWithLabel("This is a 128px clipped text, it's clipped");
        layout.Text("This is a 128px clipped text, it's clipped", new Vector2(128f, 0f), FuTextWrapping.Clip);

        layout.SetNextElementToolTip("This is a clickable text, click it !");
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

    /// <summary>
    /// Runs the draw spinners workflow.
    /// </summary>
    /// <param name="layout">The layout value.</param>
    private void drawSpinners(FuLayout layout)
    {
        using (var grid = new FuGrid("gSPN", FuGridFlag.LinesBackground))
        {
            if (!_enableWidgets)
                grid.DisableNextElements();
            grid.Loader_Arc(spinnerSize, spinnerRingThickness);
            grid.Text("Arc");
            layout.Slider("size##spinnerArc", ref spinnerSize, 8f, 128f);
            layout.Slider("thickness##spinnerArc", ref spinnerRingThickness, 1f, 12f);

            grid.Loader_DualRing(spinnerSize, spinnerRingThickness);
            grid.Text("Dual Ring");
            layout.Slider("size##spinnerDualRing", ref spinnerSize, 8f, 128f);
            layout.Slider("thickness##spinnerDualRing", ref spinnerRingThickness, 1f, 12f);

            grid.Loader_Spinner(spinnerSize, spinnerNbDots, spinnerDotsSize, spinnerDoubleColor);
            grid.Text("Dot Trail");
            layout.Slider("size##spinnerDots", ref spinnerSize, 8f, 128f);
            layout.Slider("dots##spinnerDots", ref spinnerNbDots, 3, 32);
            layout.Slider("dot size##spinnerDots", ref spinnerDotsSize, 1f, 16f);
            layout.Toggle("double colors##spinnerDots", ref spinnerDoubleColor);

            grid.Loader_CircleSpinner(spinnerSize, spinnerNbDots);
            grid.Text("Circle spinner");
            layout.Slider("size##spinnerCircle", ref spinnerSize, 8f, 128f);
            layout.Slider("dots##spinnerCircle", ref spinnerNbDots, 3, 32);

            grid.Loader_EllipseSpinner(spinnerSize, spinnerNbDots, spinnerDotsSize, spinnerDoubleColor);
            grid.Text("Ellipse Spinner");
            layout.Slider("size##spinnerEllipse", ref spinnerSize, 8f, 128f);
            layout.Slider("dots##spinnerEllipse", ref spinnerNbDots, 4, 32);
            layout.Slider("dot size##spinnerEllipse", ref spinnerDotsSize, 1f, 16f);
            layout.Toggle("double colors##spinnerEllipse", ref spinnerDoubleColor);

            grid.Loader_Orbit(spinnerSize, Mathf.Clamp(spinnerNbDots / 2, 1, 6));
            grid.Text("Orbit");
            layout.Slider("size##spinnerOrbit", ref spinnerSize, 8f, 128f);
            layout.Slider("dots##spinnerOrbit", ref spinnerNbDots, 2, 12);

            grid.Loader_BreathingDots(spinnerV2Size, Mathf.Clamp(spinnerNbDots, 2, 8));
            grid.Text("Breathing Dots");
            layout.Drag("size##spinnerBreathingDots", ref spinnerV2Size, "", "", 8f, 160f);
            layout.Slider("dots##spinnerBreathingDots", ref spinnerNbDots, 2, 8);

            grid.Loader_Bars(spinnerV2Size, spinnerBars);
            grid.Text("Bars");
            layout.Drag("size##spinnerBars", ref spinnerV2Size, "", "", 8f, 160f);
            layout.Slider("bars##spinnerBars", ref spinnerBars, 3, 9);

            grid.Loader_Shimmer(spinnerV2Size);
            grid.Text("Shimmer");
            layout.Drag("size##spinnerShimmer", ref spinnerV2Size, "", "", 8f, 180f);

            grid.Loader_Wheel(spinnerSize);
            grid.Text("Wheel");
            layout.Slider("size##spinnerWheel", ref spinnerSize, 8f, 128f);

            grid.Loader_WavyLine(spinnerV2Size, spinnerFrequency, spinnerDoubleColor);
            grid.Text("Wavy Line");
            layout.Drag("size##spinnerWave", ref spinnerV2Size, "", "", 8f, 180f);
            layout.Slider("frequency##spinnerWave", ref spinnerFrequency, 0.5f, 24f);

            grid.Loader_Squares(spinnerSize);
            grid.Text("Squares");
            layout.Slider("size##spinnerSquares", ref spinnerSize, 8f, 128f);

            grid.Loader_SquareCircleDance(spinnerSize);
            grid.Text("Oval Orbit");
            layout.Slider("size##spinnerOvalOrbit", ref spinnerSize, 8f, 128f);

            grid.Loader_PulsingLines(spinnerV2Size);
            grid.Text("Pulsing Lines");
            layout.Drag("size##spinnerPulsingLines", ref spinnerV2Size, "", "", 8f, 160f);

            grid.Loader_Clocker(spinnerSize);
            grid.Text("Clock");
            layout.Slider("size##spinnerClock", ref spinnerSize, 8f, 128f);

            grid.Loader_Pulsar(spinnerSize);
            grid.Text("Pulsar");
            layout.Slider("size##spinnerPulsar", ref spinnerSize, 8f, 128f);

            grid.Loader_SpikedWheel(spinnerV2Size);
            grid.Text("Segment Wheel");
            layout.Drag("size##spinnerSegmentWheel", ref spinnerV2Size, "", "", 8f, 160f);
            if (!_enableWidgets)
                grid.EnableNextElements();
        }
    }

    /// <summary>
    /// Runs the draw progressbar workflow.
    /// </summary>
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

    /// <summary>
    /// Runs the draw sliders workflow.
    /// </summary>
    private void drawSliders()
    {
        using (var grid = new FuGrid("slidersGrid"))
        {
            if (!_enableWidgets)
                grid.DisableNextElements();
            grid.Slider("slider int", ref intVal);
            grid.Slider("slider", ref floatVal);
            grid.Slider("no drag##NoDrag", ref floatVal, 0.5f, FuSliderFlags.NoDrag);
            grid.Slider("left drag##LeftDrag", ref floatVal, 0.5f, FuSliderFlags.LeftDrag);
            grid.Slider("no knobs##NoKnobs", ref floatVal, 0.5f, FuSliderFlags.NoKnobs);
            grid.Slider("bar click##UpdateOnBarClick", ref floatVal, 0.5f, FuSliderFlags.UpdateOnBarClick);
            grid.Slider("bar click no drag##UpdateOnBarClickNoDrag", ref floatVal, 0f, 100f, new FuElementSize(-64f, 16f), 0.5f, FuSliderFlags.UpdateOnBarClick | FuSliderFlags.NoDrag);
            grid.Slider("no drag no knob##NoDragNoKnobs", ref floatVal, 0f, 100f, new FuElementSize(-1f, 16f), 0.5f, FuSliderFlags.NoDrag | FuSliderFlags.NoKnobs);

            grid.Range("range", ref min, ref max, 0f, 30f, 0.25f);
            grid.Range("range no drag", ref min, ref max, 0f, 30f, 0.1f, FuSliderFlags.NoDrag);
            if (!_enableWidgets)
                grid.EnableNextElements();
        }
    }
    #endregion
}
