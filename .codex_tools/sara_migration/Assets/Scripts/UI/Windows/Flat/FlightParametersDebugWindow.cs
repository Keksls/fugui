using Fu;
using Fu.Framework;

using System;
using System.Collections.Generic;
using System.Globalization;
using TekelKernel3.Flight;
using TekelKernel3.Flight.EditionParameters;
using UnityEngine;

/// <summary>
/// Displays the parameters and value statistics of the loaded flight in a standalone flat Fugui window.
/// </summary>
public sealed class FlightParametersDebugWindow : FuWindowBehaviour
{
    #region Constants

    private const ushort WindowId = 60000;
    private const float DetailsReservedHeight = 150f;
    private static readonly FuWindowName FlightParametersWindowName = new FuWindowName(WindowId, "Flight parameters debug", false, 30);

    #endregion

    #region State

    private static FlightParametersDebugWindow _activeInstance;

    private readonly List<Parameter> _parameters = new List<Parameter>();
    private readonly Dictionary<Parameter, string> _parameterDetails = new Dictionary<Parameter, string>();
    private readonly List<FuTableViewColumn<Parameter>> _columns = new List<FuTableViewColumn<Parameter>>();

    private object _cachedParameterContainer;
    private string _search = string.Empty;
    private int _selectedParameterIndex = -1;
    private bool _definitionRegistered;
    private bool _started;

    #endregion

    #region Lifecycle

    /// <summary>
    /// Reserves the single runtime instance and recreates its window when the component is re-enabled.
    /// </summary>
    private void OnEnable()
    {
        // Keep a single window owner because Fugui window identifiers must be unique.
        if (_activeInstance != null && _activeInstance != this)
        {
            Debug.LogWarning("A FlightParametersDebugWindow is already active.", this);
            enabled = false;
            return;
        }

        _activeInstance = this;

        if (_started)
        {
            CreateDebugWindow();
        }
    }

    /// <summary>
    /// Creates the standalone Fugui window after Unity has initialized the scene components.
    /// </summary>
    private void Start()
    {
        // Runtime-added components reach Start after Fugui's controller has initialized its registries.
        _started = true;
        CreateDebugWindow();
        RefreshParameterData(true);
    }

    /// <summary>
    /// Detects a newly loaded flight and refreshes the cached parameter statistics.
    /// </summary>
    private void Update()
    {
        // Rebuild only when the flight container changes to avoid scanning every parameter each frame.
        object parameterContainer = Sara.Flight != null ? Sara.Flight.Container : null;
        if (ReferenceEquals(_cachedParameterContainer, parameterContainer))
        {
            return;
        }

        RefreshParameterData(true);
        Window?.ForceDraw();
    }

    /// <summary>
    /// Closes the owned window and unregisters its Fugui definition when the component is disabled or destroyed.
    /// </summary>
    private void OnDisable()
    {
        // Removing or disabling the runtime component must leave no window or definition behind.
        CloseDebugWindow();

        if (_activeInstance == this)
        {
            _activeInstance = null;
        }
    }

    #endregion

    #region Fugui Lifecycle

    /// <summary>
    /// Registers the runtime-only window definition once Fugui is available.
    /// </summary>
    public override void FuguiAwake()
    {
        // FuController may invoke this before Start for components already present in the scene.
        if (!enabled || _definitionRegistered || Fugui.UIWindowsDefinitions == null)
        {
            return;
        }

        ConfigureWindow();

        if (Fugui.UIWindowsDefinitions.ContainsKey(FlightParametersWindowName))
        {
            Debug.LogError("The Fugui window identifier reserved for FlightParametersDebugWindow is already registered.", this);
            return;
        }

        base.FuguiAwake();
        _definitionRegistered = Fugui.UIWindowsDefinitions.ContainsKey(FlightParametersWindowName);
    }

    /// <summary>
    /// Applies the runtime debug window flags after Fugui creates its instance.
    /// </summary>
    /// <param name="window">The newly created Fugui window.</param>
    public override void OnWindowCreated(FuWindow window)
    {
        // Keep the tool as a normal movable and resizable desktop debug window.
        window.IsInterractable = true;
        window.OnClosed += Window_OnClosed;
        window.ForceDraw();
    }

    /// <summary>
    /// Clears the cached window instance when the user closes it from its title bar.
    /// </summary>
    /// <param name="window">The Fugui window that has finished closing.</param>
    private void Window_OnClosed(FuWindow window)
    {
        // Avoid trying to close an already removed Fugui instance when the component is later destroyed.
        window.OnClosed -= Window_OnClosed;
        if (_fuWindow == window)
        {
            _fuWindow = null;
        }
    }

    /// <summary>
    /// Draws the search field, parameter table, and complete details for the selected row.
    /// </summary>
    /// <param name="window">The Fugui window currently being drawn.</param>
    /// <param name="layout">The layout used to draw Fugui widgets.</param>
    public override void OnUI(FuWindow window, FuLayout layout)
    {
        // The component is intentionally limited to the flat UI runtime.
        if (Sara.IsVR)
        {
            layout.Text("This debug window is available only in flat mode.", FuTextStyle.Warning);
            return;
        }

        DrawHeader(layout);

        if (Sara.Flight == null || Sara.Flight.Container == null)
        {
            layout.Text("No flight is currently loaded.", FuTextStyle.Warning);
            return;
        }

        layout.SearchBox("flightParameterSearch", ref _search, "Search a parameter...");
        layout.Spacing();

        FuTableViewFlags tableFlags = FuTableViewFlags.Default
            | FuTableViewFlags.ScrollY
            | FuTableViewFlags.UseClipper;

        layout.TableView(
            "flightParametersTable",
            _parameters,
            _columns,
            ref _selectedParameterIndex,
            _search,
            BuildSearchText,
            -DetailsReservedHeight,
            tableFlags);

        DrawSelectedParameterDetails(layout);
    }

    #endregion

    #region Window Management

    /// <summary>
    /// Configures the identity, initial placement, and behavior of the standalone debug window.
    /// </summary>
    private void ConfigureWindow()
    {
        // Use a high project-local identifier so this temporary debug tool does not collide with generated windows.
        SetWindowName(FlightParametersWindowName);
        _windowFlags = FuWindowFlags.NoExternalization | FuWindowFlags.NoDocking | FuWindowFlags.NoDockingOverMe;
        _position = new Vector2Int(40, 40);
        _size = new Vector2Int(1100, 650);
        _forceCreateAloneOnAwake = false;
    }

    /// <summary>
    /// Registers and creates the debug window when the component is active in flat mode.
    /// </summary>
    private void CreateDebugWindow()
    {
        // Delay creation until Fugui has initialized its window registries.
        if (!enabled || Sara.IsVR || Fugui.UIWindowsDefinitions == null)
        {
            return;
        }

        FuguiAwake();
        EnsureColumns();

        if (_definitionRegistered && Window == null)
        {
            Fugui.CreateWindow(FlightParametersWindowName, true);
        }
    }

    /// <summary>
    /// Closes the live window and removes its temporary definition from Fugui.
    /// </summary>
    private void CloseDebugWindow()
    {
        // Close the instance before unregistering the definition used to create it.
        _fuWindow?.Close();
        _fuWindow = null;

        if (_definitionRegistered && Fugui.UIWindowsDefinitions != null)
        {
            Fugui.UnregisterWindowDefinition(FlightParametersWindowName);
        }

        _definitionRegistered = false;
    }

    #endregion

    #region Parameter Data

    /// <summary>
    /// Rebuilds the sorted parameter list and computes value statistics from the loaded flight samples.
    /// </summary>
    /// <param name="force">Whether to rebuild even when the flight container reference did not change.</param>
    private void RefreshParameterData(bool force)
    {
        // Cache results because enumerating all samples can be expensive on large flights.
        object parameterContainer = Sara.Flight != null ? Sara.Flight.Container : null;
        if (!force && ReferenceEquals(_cachedParameterContainer, parameterContainer))
        {
            return;
        }

        _cachedParameterContainer = parameterContainer;
        _parameters.Clear();
        _parameterDetails.Clear();
        _selectedParameterIndex = -1;

        if (Sara.Flight == null || Sara.Flight.Container == null)
        {
            return;
        }

        HashSet<string> parameterNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (Parameter parameter in Sara.Flight.Container.Parameters)
        {
            AddParameterData(parameter, parameterNames);
        }

        foreach (var compressedParameter in Sara.Flight.Container.CompressedParameters)
        {
            string parameterName = compressedParameter.Key.Name;
            if (string.IsNullOrEmpty(parameterName) || parameterNames.Contains(parameterName))
            {
                continue;
            }

            // Resolve the compressed entry through the container indexer before scanning its samples.
            AddParameterData(Sara.Flight.Container[parameterName], parameterNames);
        }

        _parameters.Sort((left, right) => string.Compare(
            GetParameterName(left),
            GetParameterName(right),
            StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Adds one resolved parameter and its computed statistics when its name is not already cached.
    /// </summary>
    /// <param name="parameter">The resolved flight parameter to cache.</param>
    /// <param name="parameterNames">The set used to deduplicate standard and compressed parameters.</param>
    private void AddParameterData(Parameter parameter, HashSet<string> parameterNames)
    {
        // Skip invalid or duplicate entries while keeping one row per authoritative header name.
        if (parameter == null)
        {
            return;
        }

        string parameterName = GetParameterName(parameter);
        if (!parameterNames.Add(parameterName))
        {
            return;
        }

        _parameters.Add(parameter);
        _parameterDetails[parameter] = BuildParameterDetails(parameter);
    }

    /// <summary>
    /// Computes the complete display data for a numeric, textual, or unsupported parameter type.
    /// </summary>
    /// <param name="parameter">The flight parameter to inspect.</param>
    /// <returns>The formatted value statistics.</returns>
    private static string BuildParameterDetails(Parameter parameter)
    {
        // Dispatch to the concrete Tekel parameter type so every sample is interpreted correctly.
        if (parameter is ParameterText textParameter)
        {
            return BuildTextParameterDetails(textParameter);
        }

        if (parameter is ParameterNumeric numericParameter)
        {
            return BuildNumericParameterDetails(numericParameter);
        }

        return "Unsupported parameter type: " + parameter.GetType().Name;
    }

    /// <summary>
    /// Collects and formats every distinct string value stored in a text parameter.
    /// </summary>
    /// <param name="parameter">The text parameter to inspect.</param>
    /// <returns>The sorted distinct values and their count.</returns>
    private static string BuildTextParameterDetails(ParameterText parameter)
    {
        // A sorted set removes duplicate timeline samples while keeping the debug output deterministic.
        SortedSet<string> values = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var sample in parameter)
        {
            values.Add(string.IsNullOrEmpty(sample.StringValue) ? "<empty>" : sample.StringValue);
        }

        if (values.Count == 0)
        {
            return "Values (0): none";
        }

        return "Values (" + values.Count + "): " + string.Join(", ", values);
    }

    /// <summary>
    /// Computes and formats the minimum and maximum finite values stored in a numeric parameter.
    /// </summary>
    /// <param name="parameter">The numeric parameter to inspect.</param>
    /// <returns>The minimum and maximum values found in the flight samples.</returns>
    private static string BuildNumericParameterDetails(ParameterNumeric parameter)
    {
        // Ignore NaN and infinity so corrupt samples do not make the reported range unusable.
        double minimum = double.PositiveInfinity;
        double maximum = double.NegativeInfinity;

        foreach (var sample in parameter)
        {
            double value = sample.Value;
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                continue;
            }

            minimum = Math.Min(minimum, value);
            maximum = Math.Max(maximum, value);
        }

        if (double.IsPositiveInfinity(minimum))
        {
            return "Min: none | Max: none";
        }

        return "Min: " + minimum.ToString("G17", CultureInfo.InvariantCulture)
            + " | Max: " + maximum.ToString("G17", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns the cached detail text associated with a parameter.
    /// </summary>
    /// <param name="parameter">The parameter whose details are requested.</param>
    /// <returns>The cached parameter details.</returns>
    private string GetParameterDetails(Parameter parameter)
    {
        // Return a stable fallback if a parameter was added after the cache was built.
        return parameter != null && _parameterDetails.TryGetValue(parameter, out string details)
            ? details
            : "No data";
    }

    /// <summary>
    /// Returns the display name stored in a parameter header.
    /// </summary>
    /// <param name="parameter">The parameter whose name is requested.</param>
    /// <returns>The parameter name.</returns>
    private static string GetParameterName(Parameter parameter)
    {
        // Headers are the authoritative source for flight parameter names.
        return parameter != null
            ? parameter.Header.Name ?? string.Empty
            : string.Empty;
    }

    /// <summary>
    /// Returns the description stored in a parameter header.
    /// </summary>
    /// <param name="parameter">The parameter whose description is requested.</param>
    /// <returns>The parameter description.</returns>
    private static string GetParameterDescription(Parameter parameter)
    {
        // Preserve empty descriptions instead of synthesizing misleading debug metadata.
        return parameter != null
            ? parameter.Header.Description ?? string.Empty
            : string.Empty;
    }

    /// <summary>
    /// Maps the concrete flight parameter class to a concise debug type label.
    /// </summary>
    /// <param name="parameter">The parameter whose type is requested.</param>
    /// <returns>String, Numerical, or the concrete fallback type name.</returns>
    private static string GetParameterTypeLabel(Parameter parameter)
    {
        // Expose the two domain types requested by the debug interface.
        if (parameter is ParameterText)
        {
            return "String";
        }

        if (parameter is ParameterNumeric)
        {
            return "Numerical";
        }

        return parameter != null ? parameter.GetType().Name : "Unknown";
    }

    /// <summary>
    /// Builds the combined searchable text used by the Fugui table filter.
    /// </summary>
    /// <param name="parameter">The parameter represented by the row.</param>
    /// <returns>All searchable parameter fields in one string.</returns>
    private string BuildSearchText(Parameter parameter)
    {
        // Include metadata and computed values so searches can find names, types, ranges, or text values.
        return GetParameterName(parameter) + " "
            + GetParameterDescription(parameter) + " "
            + GetParameterTypeLabel(parameter) + " "
            + GetParameterDetails(parameter);
    }

    #endregion

    #region Drawing

    /// <summary>
    /// Creates the reusable Fugui column definitions used by the parameter table.
    /// </summary>
    private void EnsureColumns()
    {
        // Column callbacks read cached data and therefore allocate no per-row statistics during rendering.
        if (_columns.Count > 0)
        {
            return;
        }

        _columns.Add(new FuTableViewColumn<Parameter>("Name", GetParameterName, 240f));
        _columns.Add(new FuTableViewColumn<Parameter>("Description", GetParameterDescription, 300f));
        _columns.Add(new FuTableViewColumn<Parameter>("Type", GetParameterTypeLabel, 100f));
        _columns.Add(new FuTableViewColumn<Parameter>("Data", GetParameterDetails));
    }

    /// <summary>
    /// Draws the window title, parameter count, and manual refresh action.
    /// </summary>
    /// <param name="layout">The layout used to draw the header.</param>
    private void DrawHeader(FuLayout layout)
    {
        // Keep refresh available for debug scenarios where a container is mutated in place.
        Fugui.PushFont(FontType.Bold);
        layout.Text("Flight parameters");
        Fugui.PopFont();

        layout.Text(_parameters.Count + " parameters");
        layout.SameLine();
        if (layout.Button("Refresh##flightParameters"))
        {
            RefreshParameterData(true);
        }

        layout.Separator();
    }

    /// <summary>
    /// Draws all cached metadata and values for the selected parameter below the table.
    /// </summary>
    /// <param name="layout">The layout used to draw the selected parameter details.</param>
    private void DrawSelectedParameterDetails(FuLayout layout)
    {
        // The scrollable detail area exposes every value even when the table cell is visually clipped.
        layout.Separator();

        if (Fugui.BeginChild(
            "flightParameterDetails",
            Vector2.zero,
            FuChildFlags.Borders,
            FuWindowStyleFlags.HorizontalScrollbar))
        {
            if (_selectedParameterIndex < 0 || _selectedParameterIndex >= _parameters.Count)
            {
                layout.Text("Select a parameter to view all its data.");
            }
            else
            {
                Parameter parameter = _parameters[_selectedParameterIndex];
                Fugui.PushFont(FontType.Bold);
                layout.Text(GetParameterName(parameter) + " (" + GetParameterTypeLabel(parameter) + ")");
                Fugui.PopFont();

                string description = GetParameterDescription(parameter);
                if (!string.IsNullOrWhiteSpace(description))
                {
                    layout.Text(description, FuTextWrapping.Wrap);
                }

                layout.Text(GetParameterDetails(parameter), FuTextWrapping.Wrap);
            }
        }

        Fugui.EndChild();
    }

    #endregion
}
