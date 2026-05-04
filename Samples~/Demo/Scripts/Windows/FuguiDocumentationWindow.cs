using Fu;
using Fu.Framework;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Runtime documentation window for the Fugui demo scene.
/// </summary>
public class FuguiDocumentationWindow : FuWindowBehaviour
{
    #region State
    private static readonly string[] Groups = new string[]
    {
        "Start",
        "Windows",
        "UI",
        "Systems",
        "Reference"
    };

    private readonly List<DocSection> _sections = new List<DocSection>();
    private string _searchText = string.Empty;
    private string _lastSearchText = string.Empty;
    private string _selectedSectionId = "overview";
    private int _selectedGroupIndex;
    private int _lastVisibleCount;
    private bool _pendingScrollToSelected;
    #endregion

    #region Nested Types
    /// <summary>
    /// One documentation page displayed by the runtime window.
    /// </summary>
    private sealed class DocSection
    {
        #region State
        public readonly string Id;
        public readonly string Group;
        public readonly string Title;
        public readonly string Summary;
        public readonly string Body;
        public readonly string[] Bullets;
        public readonly string CodeTitle;
        public readonly string Code;
        public readonly string[] Tags;
        #endregion

        #region Constructors
        public DocSection(string id, string group, string title, string summary, string body, string[] bullets = null, string codeTitle = null, string code = null, string[] tags = null)
        {
            Id = id;
            Group = group;
            Title = title;
            Summary = summary;
            Body = body;
            Bullets = bullets ?? Array.Empty<string>();
            CodeTitle = codeTitle;
            Code = code;
            Tags = tags ?? Array.Empty<string>();
        }
        #endregion

        #region Methods
        public string CollapseLabel()
        {
            return Title + "##doc-section-" + Id;
        }

        public bool Matches(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            return Contains(Group, query)
                || Contains(Title, query)
                || Contains(Summary, query)
                || Contains(Body, query)
                || Contains(CodeTitle, query)
                || Contains(Code, query)
                || Bullets.Any(item => Contains(item, query))
                || Tags.Any(item => Contains(item, query));
        }

        private static bool Contains(string value, string query)
        {
            return !string.IsNullOrEmpty(value)
                && value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        #endregion
    }
    #endregion

    #region Methods
    private void Awake()
    {
        ensureWindowDefaults();
        ensureDocumentation();
    }

    /// <summary>
    /// Register the documentation window definition with robust defaults.
    /// </summary>
    public override void FuguiAwake()
    {
        ensureWindowDefaults();
        ensureDocumentation();
        base.FuguiAwake();
    }

    /// <summary>
    /// Adds a modern header and footer to the documentation window.
    /// </summary>
    /// <param name="windowDefinition">The window definition.</param>
    public override void OnWindowDefinitionCreated(FuWindowDefinition windowDefinition)
    {
        base.OnWindowDefinitionCreated(windowDefinition);
        windowDefinition.SetHeaderUI(drawHeader, 34f);
        windowDefinition.SetFooterUI(drawFooter, 22f);
    }

    /// <summary>
    /// Draws the documentation UI.
    /// </summary>
    /// <param name="window">The Fugui window.</param>
    /// <param name="layout">The active layout.</param>
    public override void OnUI(FuWindow window, FuLayout layout)
    {
        ensureDocumentation();

        using (new FuPanel("fugui-documentation-root", FuStyle.Unpadded))
        {
            drawHero(layout);
            drawSearchAndTabs(layout);
            drawDocumentationBody(layout);
        }
    }

    private void ensureWindowDefaults()
    {
        if (_windowName.ID == 0)
        {
            _windowName = FuWindowsNames.FuguiDocumentation;
        }

        if (_size == Vector2Int.zero)
        {
            _size = new Vector2Int(860, 660);
        }
    }

    private void drawHeader(FuWindow window, Vector2 size)
    {
        FuLayout layout = window.Layout;
        float scale = Fugui.CurrentContext.Scale;
        Vector2 pos = ImGui.GetCursorScreenPos();
        Vector2 padding = new Vector2(12f, 0f) * scale;
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        Vector4 bg = Fugui.Themes.GetColor(FuColors.TitleBgActive, 0.92f);
        Vector4 accent = Fugui.Themes.GetColor(FuColors.Highlight, 0.85f);
        drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(bg), 0f);
        drawList.AddRectFilled(pos, new Vector2(pos.x + 3f * scale, pos.y + size.y), ImGui.GetColorU32(accent), 0f);

        Fugui.PushFont(FontType.Bold);
        layout.EnboxedText("Fugui Documentation", pos + padding, size - padding * 2f, Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f), FuTextWrapping.Clip);
        Fugui.PopFont();

        string count = _sections.Count + " topics";
        Vector2 countSize = ImGui.CalcTextSize(count) + new Vector2(16f, 4f) * scale;
        Rect badge = new Rect(new Vector2(pos.x + size.x - countSize.x - 10f * scale, pos.y + (size.y - countSize.y) * 0.5f), countSize);
        drawList.AddRectFilled(badge.min, badge.max, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Highlight, 0.16f)), badge.height * 0.5f, ImDrawFlags.RoundCornersAll);
        Fugui.Push(ImGuiCol.Text, Fugui.Themes.GetColor(FuColors.HighlightText, 0.92f));
        layout.EnboxedText(count, badge.position, badge.size, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), FuTextWrapping.Clip);
        Fugui.PopColor();
    }

    private void drawFooter(FuWindow window, Vector2 size)
    {
        FuLayout layout = window.Layout;
        string text = string.IsNullOrWhiteSpace(_searchText)
            ? "Runtime guide, API map, integration checklist and troubleshooting"
            : _lastVisibleCount + " matching sections for \"" + _searchText + "\"";

        Fugui.PushFont(FontType.Italic);
        layout.CenterNextItemH(text);
        layout.CenterNextItemV(text, size.y);
        layout.Text(text, FuTextStyle.Deactivated, FuTextWrapping.Clip);
        Fugui.PopFont();
    }

    private void drawHero(FuLayout layout)
    {
        layout.FeaturePanel(
            "fugui-docs-hero",
            "Build runtime tools with Fugui",
            "A complete in-scene guide to setup, windows, docking, widgets, styling, overlays, 3D panels, mobile input and production checks.",
            new string[] { "Immediate mode", "Dockable", "Unity runtime" },
            new FuColors[] { FuColors.Highlight, FuColors.BackgroundSuccess, FuColors.BackgroundInfo });
        layout.Dummy(0f, 6f);
    }

    private void drawSearchAndTabs(FuLayout layout)
    {
        bool searchChanged = layout.SearchBox("fugui-docs-search", ref _searchText, "Search setup, windows, widgets, overlays, mobile...");
        if (searchChanged || _lastSearchText != _searchText)
        {
            _lastSearchText = _searchText;
            syncSelectionWithSearch();
        }

        layout.Dummy(0f, 6f);

        if (isSearchActive())
        {
            drawSearchModeBar(layout);
            layout.Dummy(0f, 8f);
            return;
        }

        int previous = _selectedGroupIndex;
        if (layout.Tabs("fugui-docs-groups", Groups, ref _selectedGroupIndex, FuTabsFlags.Stretch | FuTabsFlags.EqualWidth | FuTabsFlags.Compact)
            && previous != _selectedGroupIndex)
        {
            DocSection first = getGroupSections(Groups[_selectedGroupIndex]).FirstOrDefault();
            if (first != null)
            {
                selectSection(first, true);
                openOnlySection(layout, first);
            }
        }

        layout.Dummy(0f, 8f);
    }

    private void drawSearchModeBar(FuLayout layout)
    {
        float scale = Fugui.CurrentContext.Scale;
        Rect rect = layout.Surface("fugui-docs-search-mode", new FuElementSize(-1f, 30f), FuColors.Highlight, FuSurfaceFlags.Border, 0.34f, 0.24f, 0.72f, 5f);
        Vector2 afterSurfacePos = ImGui.GetCursorScreenPos();
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        int count = getNavigationSections().Count();

        string label = count + " search results";
        Fugui.Push(ImGuiCol.Text, Fugui.Themes.GetColor(FuColors.TextDisabled, 0.90f));
        layout.EnboxedText(label, rect.position + new Vector2(10f, 0f) * scale, new Vector2(rect.width - 96f * scale, rect.height), Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f), FuTextWrapping.Clip);
        Fugui.PopColor();

        Rect clearRect = new Rect(new Vector2(rect.xMax - 78f * scale, rect.yMin + 4f * scale), new Vector2(68f * scale, rect.height - 8f * scale));
        ImGui.SetCursorScreenPos(clearRect.position);
        bool clicked = ImGui.InvisibleButton("##fugui-docs-clear-search", clearRect.size);
        bool hovered = ImGui.IsItemHovered();
        Vector4 clearBg = Fugui.Themes.GetColor(hovered ? FuColors.ButtonHovered : FuColors.Button, hovered ? 0.72f : 0.42f);
        drawList.AddRectFilled(clearRect.min, clearRect.max, ImGui.GetColorU32(clearBg), clearRect.height * 0.5f, ImDrawFlags.RoundCornersAll);
        layout.EnboxedText("Clear", clearRect.position, clearRect.size, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), FuTextWrapping.Clip);
        if (hovered)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
        if (clicked)
        {
            _searchText = string.Empty;
            _lastSearchText = string.Empty;
            ensureSelectedSectionForCurrentGroup();
        }

        ImGui.SetCursorScreenPos(afterSurfacePos);
    }

    private void drawDocumentationBody(FuLayout layout)
    {
        if (!isSearchActive())
        {
            ensureSelectedSectionForCurrentGroup();
        }

        Vector2 available = layout.GetAvailable();
        float scale = Fugui.CurrentContext.Scale;
        bool wide = available.x > 760f * scale;

        if (wide)
        {
            float navWidth = Mathf.Clamp(224f * scale, 190f * scale, Mathf.Min(300f * scale, available.x * 0.33f));
            ImGui.BeginChild("fugui-docs-navigation", new Vector2(navWidth, 0f), ImGuiChildFlags.None, ImGuiWindowFlags.None);
            drawSidebar(layout);
            ImGui.EndChild();

            ImGui.SameLine();
            ImGui.BeginChild("fugui-docs-content", Vector2.zero, ImGuiChildFlags.None, ImGuiWindowFlags.None);
            drawSections(layout);
            ImGui.EndChild();
        }
        else
        {
            layout.Collapsable("Contents", () => drawSidebar(layout), FuButtonStyle.Collapsable, 0f, false);
            layout.Dummy(0f, 6f);
            drawSections(layout);
        }
    }

    private void drawSidebar(FuLayout layout)
    {
        List<DocSection> sections = getNavigationSections().ToList();
        _lastVisibleCount = sections.Count;

        drawSidebarCard(layout, sections.Count);
        layout.Dummy(0f, 6f);

        if (sections.Count == 0)
        {
            layout.Text("No matching documentation section.", FuTextStyle.Deactivated, FuTextWrapping.Wrap);
            return;
        }

        string previousGroup = null;
        foreach (DocSection section in sections)
        {
            if (isSearchActive() && previousGroup != section.Group)
            {
                if (previousGroup != null)
                {
                    layout.Dummy(0f, 4f);
                }
                drawNavGroupLabel(layout, section.Group);
                previousGroup = section.Group;
            }

            if (layout.NavigationItem("doc-nav-" + section.Id, section.Title, section.Id == _selectedSectionId, section.Summary))
            {
                selectSection(section, true);
                openOnlySection(layout, section);
            }
            layout.Dummy(0f, 2f);
        }

        layout.Dummy(0f, 8f);
        layout.Text("Tip: use the Window Names tab in Tools > Fugui > Editor to keep IDs unique.", FuTextStyle.Deactivated, FuTextWrapping.Wrap);
        layout.Dummy(0f, 4f);
    }

    private void drawSections(FuLayout layout)
    {
        List<DocSection> sections = getContentSections().ToList();
        _lastVisibleCount = getNavigationSections().Count();

        if (sections.Count == 0)
        {
            drawEmptyState(layout);
            return;
        }

        foreach (DocSection section in sections)
        {
            bool selected = section.Id == _selectedSectionId;
            if (selected && _pendingScrollToSelected)
            {
                ImGui.SetScrollHereY(0.08f);
                _pendingScrollToSelected = false;
            }

            layout.Collapsable(section.CollapseLabel(), () =>
            {
                drawSectionBody(layout, section);
            }, selected ? FuButtonStyle.Highlight : FuButtonStyle.Collapsable, 10f, selected);

            layout.Dummy(0f, 8f);
        }
    }

    private void drawSectionBody(FuLayout layout, DocSection section)
    {
        layout.Callout("doc-callout-" + section.Id, section.Summary, FuColors.Highlight);

        if (!string.IsNullOrWhiteSpace(section.Body))
        {
            layout.Text(section.Body, FuTextStyle.Default, FuTextWrapping.Wrap);
            layout.Dummy(0f, 6f);
        }

        foreach (string bullet in section.Bullets)
        {
            drawBullet(layout, bullet);
        }

        if (!string.IsNullOrWhiteSpace(section.Code))
        {
            layout.Dummy(0f, 6f);
            drawCodeBlock(layout, section.CodeTitle, section.Code);
        }

        if (section.Tags.Length > 0)
        {
            layout.Dummy(0f, 6f);
            layout.PillRow("doc-tags-" + section.Id, section.Tags);
        }
    }

    private void drawSidebarCard(FuLayout layout, int visibleSections)
    {
        float scale = Fugui.CurrentContext.Scale;
        Rect rect = layout.Surface("fugui-docs-sidebar-card", new FuElementSize(-1f, 54f), FuColors.Highlight, FuSurfaceFlags.Border, 0.40f, 0.20f, 0.72f, 5f);

        Fugui.PushFont(14, FontType.Bold);
        layout.EnboxedText(isSearchActive() ? "Search Results" : Groups[_selectedGroupIndex], rect.position + new Vector2(10f, 5f) * scale, new Vector2(rect.width - 20f * scale, 19f * scale), Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f), FuTextWrapping.Clip);
        Fugui.PopFont();

        string subtitle = isSearchActive()
            ? visibleSections + " sections match"
            : getGroupSections(Groups[_selectedGroupIndex]).Count() + " sections in this area";
        Fugui.Push(ImGuiCol.Text, Fugui.Themes.GetColor(FuColors.TextDisabled, 0.76f));
        layout.EnboxedText(subtitle, rect.position + new Vector2(10f, 27f) * scale, new Vector2(rect.width - 20f * scale, 18f * scale), Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f), FuTextWrapping.Clip);
        Fugui.PopColor();
    }

    private void drawNavGroupLabel(FuLayout layout, string group)
    {
        Fugui.PushFont(12, FontType.Bold);
        Fugui.Push(ImGuiCol.Text, Fugui.Themes.GetColor(FuColors.TextDisabled, 0.78f));
        layout.Text(group.ToUpperInvariant(), FuTextWrapping.Clip);
        Fugui.PopColor();
        Fugui.PopFont();
    }

    private void drawBullet(FuLayout layout, string text)
    {
        float scale = Fugui.CurrentContext.Scale;
        float width = Mathf.Max(1f, layout.GetAvailableWidth());
        Vector2 pos = ImGui.GetCursorScreenPos();
        Vector2 textSize = Fugui.CalcTextSize(text, FuTextWrapping.Wrap, new Vector2(width - 24f * scale, 300f * scale));
        float height = Mathf.Max(ImGui.GetTextLineHeight(), textSize.y) + 4f * scale;
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();
        Vector2 dot = pos + new Vector2(7f, 9f) * scale;
        drawList.AddCircleFilled(dot, 3f * scale, ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.Highlight, 0.84f)), 16);
        layout.EnboxedText(text, pos + new Vector2(20f, 0f) * scale, new Vector2(width - 20f * scale, height), Vector2.zero, Vector2.zero, new Vector2(0f, 0f), FuTextWrapping.Wrap);
        ImGui.Dummy(new Vector2(width, height));
    }

    private void drawCodeBlock(FuLayout layout, string title, string code)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            Fugui.PushFont(12, FontType.Bold);
            layout.Text(title, FuTextStyle.Deactivated, FuTextWrapping.Clip);
            Fugui.PopFont();
            layout.Dummy(0f, 3f);
        }

        string[] lines = code.Replace("\r\n", "\n").Split('\n');
        float scale = Fugui.CurrentContext.Scale;
        float width = Mathf.Max(1f, layout.GetAvailableWidth());
        Vector2 padding = new Vector2(10f, 8f) * scale;
        Fugui.PushFont(13, FontType.Regular);
        float lineHeight = ImGui.GetTextLineHeightWithSpacing();
        float height = padding.y * 2f + Mathf.Max(1, lines.Length) * lineHeight;
        Vector2 pos = ImGui.GetCursorScreenPos();
        Rect rect = new Rect(pos, new Vector2(width, height));
        ImDrawListPtr drawList = ImGui.GetWindowDrawList();

        Vector4 bg = Fugui.Themes.GetColor(FuColors.PopupBg, 0.82f);
        Vector4 border = Fugui.Themes.GetColor(FuColors.Border, 0.48f);
        drawList.AddRectFilled(rect.min, rect.max, ImGui.GetColorU32(bg), 6f * scale, ImDrawFlags.RoundCornersAll);
        drawList.AddRect(rect.min, rect.max, ImGui.GetColorU32(border), 6f * scale, ImDrawFlags.RoundCornersAll, Mathf.Max(1f, scale));

        drawList.PushClipRect(rect.min + padding * 0.5f, rect.max - padding * 0.5f, true);
        Vector2 linePos = pos + padding;
        uint textColor = ImGui.GetColorU32(Fugui.Themes.GetColor(FuColors.TextDisabled, 0.96f));
        for (int i = 0; i < lines.Length; i++)
        {
            drawList.AddText(linePos + new Vector2(0f, i * lineHeight), textColor, lines[i]);
        }
        drawList.PopClipRect();

        Fugui.PopFont();
        ImGui.Dummy(new Vector2(width, height + 4f * scale));
    }

    private void drawEmptyState(FuLayout layout)
    {
        layout.Callout("doc-empty-state", "No section matches the current search. Try setup, window, layout, theme, 3D, mobile, widget or modal.", FuColors.BackgroundWarning);
    }

    private bool isSearchActive()
    {
        return !string.IsNullOrWhiteSpace(_searchText);
    }

    private IEnumerable<DocSection> getNavigationSections()
    {
        if (isSearchActive())
        {
            return _sections.Where(section => section.Matches(_searchText));
        }

        return getGroupSections(Groups[_selectedGroupIndex]);
    }

    private IEnumerable<DocSection> getContentSections()
    {
        if (isSearchActive())
        {
            DocSection selected = _sections.FirstOrDefault(section => section.Id == _selectedSectionId && section.Matches(_searchText));
            if (selected != null)
            {
                return new DocSection[] { selected };
            }

            DocSection first = getNavigationSections().FirstOrDefault();
            return first != null ? new DocSection[] { first } : Array.Empty<DocSection>();
        }

        return getGroupSections(Groups[_selectedGroupIndex]);
    }

    private void syncSelectionWithSearch()
    {
        if (!isSearchActive())
        {
            ensureSelectedSectionForCurrentGroup();
            return;
        }

        DocSection current = _sections.FirstOrDefault(section => section.Id == _selectedSectionId);
        if (current != null && current.Matches(_searchText))
        {
            return;
        }

        DocSection first = getNavigationSections().FirstOrDefault();
        if (first != null)
        {
            selectSection(first, false);
        }
    }

    private void selectSection(DocSection section, bool requestScroll)
    {
        if (section == null)
        {
            return;
        }

        _selectedSectionId = section.Id;
        int groupIndex = Array.IndexOf(Groups, section.Group);
        if (groupIndex >= 0)
        {
            _selectedGroupIndex = groupIndex;
        }
        _pendingScrollToSelected = requestScroll;
    }

    private void openOnlySection(FuLayout layout, DocSection openedSection)
    {
        if (layout == null || openedSection == null)
        {
            return;
        }

        foreach (DocSection section in _sections)
        {
            if (section.Id == openedSection.Id)
            {
                continue;
            }

            layout.CloseCollapsable(section.CollapseLabel());
        }

        layout.OpenCollapsable(openedSection.CollapseLabel());
    }

    private IEnumerable<DocSection> getGroupSections(string group)
    {
        return _sections.Where(section => section.Group == group);
    }

    private void ensureSelectedSectionForCurrentGroup()
    {
        string group = Groups[_selectedGroupIndex];
        if (_sections.Any(section => section.Group == group && section.Id == _selectedSectionId))
        {
            return;
        }

        DocSection first = getGroupSections(group).FirstOrDefault();
        if (first != null)
        {
            _selectedSectionId = first.Id;
        }
    }

    private void ensureDocumentation()
    {
        if (_sections.Count > 0)
        {
            return;
        }

        _sections.Add(new DocSection(
            "overview",
            "Start",
            "What Fugui Is",
            "Fugui is an immediate-mode UI framework for Unity 6, built on Dear ImGui and focused on runtime tools.",
            "Use Fugui when you need dockable runtime windows, debug panels, inspectors, camera views, 3D UI panels, overlays, native popups or dense production tools inside a Unity application.",
            new string[]
            {
                "Core API lives in Fugui, FuWindow, FuWindowDefinition, FuLayout, FuGrid and FuPanel.",
                "Rendering is driven by FuController and FuguiRenderFeature, with Unity, 3D and optional external contexts.",
                "The demo scene shows normal windows, a camera window, settings on a 3D panel, widgets, tree data, popups and a nodal editor."
            },
            tags: new string[] { "runtime UI", "Dear ImGui", "Unity 6" }));

        _sections.Add(new DocSection(
            "package-structure",
            "Start",
            "Package Structure",
            "The package is organized around Runtime, StreamingAssets and Samples.",
            "Keep these folders together when moving the package. Runtime contains assemblies, framework code, renderer resources and settings. StreamingAssets contains fonts, layouts and themes loaded at runtime. Samples contains the demo scenes and example windows.",
            new string[]
            {
                "Runtime/Core: contexts, rendering, windows, containers, input, platform glue and file browser.",
                "Runtime/Framework: layouts, widgets, behaviours, styles, themes, menus, panels and the nodal system.",
                "Runtime/Resources: FuguiController prefab, shaders, images, cursors and FontConfig.",
                "StreamingAssets/Fugui: Fonts, Themes, Layouts and their index JSON files.",
                "Samples/Demo and Samples/MobileDemo: practical integration examples."
            },
            tags: new string[] { "folders", "resources", "samples" }));

        _sections.Add(new DocSection(
            "installation",
            "Start",
            "Installation",
            "Install Fugui as a Unity package or keep the folder under Assets with its metadata intact.",
            "The package declares the Unity post-processing dependency and embeds its native and managed runtime dependencies. Unity must import the .meta files so scripts, prefabs, shaders and resources keep stable GUIDs.",
            new string[]
            {
                "Package Manager path: Window > Package Manager > Add package from git URL.",
                "Local path: copy the Fugui folder into Packages or Assets.",
                "Do not split Runtime, StreamingAssets, Samples and Logo unless your project remaps paths intentionally.",
                "URP is the intended rendering path for the default FuguiRenderFeature."
            },
            tags: new string[] { "UPM", "dependencies", "URP" }));

        _sections.Add(new DocSection(
            "setup",
            "Start",
            "Unity Setup",
            "A scene needs a FuController, the URP render feature and runtime assets available in Resources and StreamingAssets.",
            "The setup wizard under Tools > Fugui > Editor can diagnose common project configuration issues. It can add the controller prefab, repair renderer feature configuration and verify fonts, themes and layouts.",
            new string[]
            {
                "Add Runtime/Resources/FuguiController.prefab to the scene or create a GameObject with FuController.",
                "Assign FuSettings and the UI camera on the controller.",
                "Add FuguiRenderFeature to the active URP renderer and assign the Fugui URP mesh shader.",
                "Keep StreamingAssets/Fugui/Fonts/current with regular.ttf, bold.ttf and icons.ttf.",
                "Keep StreamingAssets/Fugui/Themes and Layouts with their index JSON files."
            },
            "Minimal controller flow",
            @"// FuController.Awake does the heavy lifting:
Fugui.Initialize(settings, controller, uiCamera);

// Each FuWindowBehaviour in the scene is then registered:
behaviour.FuguiAwake();

// Each update tick:
FuRaycasting.Update();
Fugui.Update();
Fugui.Render();",
            new string[] { "FuController", "render feature", "fonts" }));

        _sections.Add(new DocSection(
            "first-window",
            "Start",
            "First Window",
            "The recommended user-facing pattern is a MonoBehaviour derived from FuWindowBehaviour.",
            "A behaviour turns scene setup into a serialized, maintainable Unity workflow. The custom inspector lets you pick a FuWindowName, flags, size, position and forced creation behaviour.",
            new string[]
            {
                "Override OnUI to draw every frame when the window needs rendering.",
                "Override OnWindowDefinitionCreated to add headers, footers, overlays or a custom FuWindow type.",
                "Override OnWindowCreated for references that need the live FuWindow instance.",
                "Use a unique FuWindowName ID and register it in a window names provider class."
            },
            "FuWindowBehaviour skeleton",
            @"using Fu;
using Fu.Framework;

public class InventoryWindow : FuWindowBehaviour
{
    private bool _enabled = true;
    private float _weight = 12.5f;

    public override void OnUI(FuWindow window, FuLayout layout)
    {
        using (new FuPanel(""inventory-panel"", FuStyle.Unpadded))
        using (var grid = new FuGrid(""inventory-grid""))
        {
            grid.CheckBox(""Enabled"", ref _enabled);
            grid.Slider(""Weight"", ref _weight, 0f, 100f);
            grid.ProgressBar(""Capacity"", _weight / 100f);
        }
    }
}",
            new string[] { "FuWindowBehaviour", "OnUI", "MonoBehaviour" }));

        _sections.Add(new DocSection(
            "window-names",
            "Windows",
            "FuWindowName Registry",
            "A FuWindowName is the stable identity used by definitions, docking layouts, menus and serialized behaviours.",
            "Each application or sample should keep a small registry class that exposes static FuWindowName values. The ID is the important layout key, so changing it will orphan existing layout assignments.",
            new string[]
            {
                "Use unique ushort IDs for project windows.",
                "Name is the visible window title.",
                "autoInstantiateWindowOnlayoutSet controls whether Fugui creates the window when a layout references it.",
                "idleFPS = -1 delegates to FuSettings.IdleFPS.",
                "The Fugui editor has a Window Names tab to manage this source file."
            },
            "Registry example",
            @"using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class MyWindowNames : FuSystemWindowsNames
{
    private static readonly FuWindowName _Tools =
        new FuWindowName(100, ""Tools"", true, -1);

    public static FuWindowName Tools
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _Tools;
    }

    public static List<FuWindowName> GetAllWindowsNames()
    {
        return new List<FuWindowName>() { _None, _FuguiSettings, _Tools };
    }
}",
            new string[] { "IDs", "layout key", "registry" }));

        _sections.Add(new DocSection(
            "window-lifecycle",
            "Windows",
            "Window Lifecycle",
            "FuWindowBehaviour.FuguiAwake registers a FuWindowDefinition, then layouts or explicit calls create FuWindow instances.",
            "A definition is the recipe. A window instance is the runtime object in a container. Fugui can create multiple instances when flags allow it, dock them, externalize them or attach them to 3D containers.",
            new string[]
            {
                "FuguiAwake creates a FuWindowDefinition with window name, OnUI callback, optional position, size and flags.",
                "OnWindowDefinitionCreated is called before instances are created.",
                "WindowDefinition_OnUIWindowCreated stores the live FuWindow and calls OnWindowCreated.",
                "Force Create Alone On Awake immediately creates the window outside the current docking layout.",
                "Use window.ForceDraw when data changes while the window is idling."
            },
            "Explicit definition and creation",
            @"var definition = new FuWindowDefinition(
    MyWindowNames.Tools,
    DrawTools,
    size: new Vector2Int(420, 320),
    flags: FuWindowFlags.Default
);

definition.SetHeaderUI(DrawHeader, 24f);
Fugui.CreateWindow(MyWindowNames.Tools);",
            new string[] { "definition", "instance", "ForceDraw" }));

        _sections.Add(new DocSection(
            "window-flags",
            "Windows",
            "Window And External Flags",
            "Flags tune interaction, docking, multi-instance behaviour and native-window integration.",
            "FuWindowFlags apply to the Fugui window itself. FuExternalWindowFlags are used when a window is externalized to a native SDL/OpenTK container, when externalization is enabled at compile time and in settings.",
            new string[]
            {
                "FuWindowFlags include NoExternalization, NoDocking, NoInterractions, NoDockingOverMe, AllowMultipleWindow, NoClosable and NoAutoRegisterWindowDefinition.",
                "FuExternalWindowFlags include UseNativeTitleBar, NoTaskBarIcon, NoFocusOnAppearing, AlwaysOnTop, NoModal, NoNotify and NoContextMenu.",
                "Default external flags are UseNativeTitleBar, NoModal and NoNotify.",
                "The demo commonly uses external flags value 49 for normal dockable windows."
            },
            tags: new string[] { "flags", "externalization", "dock" }));

        _sections.Add(new DocSection(
            "camera-windows",
            "Windows",
            "Camera Windows",
            "FuCameraWindowBehaviour renders a Unity camera into a Fugui window with camera-specific FPS and sampling controls.",
            "Use it for scene views, inspectors, previews and runtime authoring tools. The demo CameraWindow adds overlays for FPS and supersampling, and raycasts from the camera window into the 3D scene.",
            new string[]
            {
                "Fields include camera, MSAA, idle camera FPS, manipulating camera FPS and supersampling.",
                "CameraWindow exposes the live FuCameraWindow instance.",
                "Use CameraWindow.GetCameraRay for picking inside the rendered view.",
                "Header and footer UI can be attached through the window definition."
            },
            "Camera pick pattern",
            @"if (CameraWindow.Mouse.IsDown(FuMouseButton.Left))
{
    Ray ray = CameraWindow.GetCameraRay();
    if (Physics.Raycast(ray, out RaycastHit hit))
    {
        Debug.Log(hit.collider.name);
    }
}",
            new string[] { "FuCameraWindowBehaviour", "raycast", "preview" }));

        _sections.Add(new DocSection(
            "3d-windows",
            "Windows",
            "3D Windows",
            "Fu3DWindowBehaviour attaches a Fugui window to a world-space panel backed by an offscreen context.",
            "A 3D window can render with fixed resolution, scale with panel size, resize at runtime and use an optional manipulator handle. It is ideal for diegetic interfaces, VR-style surfaces and spatial tooling.",
            new string[]
            {
                "Depth, Curve and Rounding control the generated panel mesh.",
                "Render Resolution controls texture resolution; container scale controls UI density.",
                "Scale Resolution With Panel keeps render aspect aligned with the transform.",
                "Fu3DWindowManipulator can move, face a target and anchor the panel.",
                "The demo hosts the Fugui Settings window on a 3D panel."
            },
            tags: new string[] { "Fu3DWindowBehaviour", "world-space", "panel" }));

        _sections.Add(new DocSection(
            "docking-layouts",
            "Windows",
            "Docking Layouts",
            "Layouts are saved as .fdl files that store a dock tree and lists of FuWindowName IDs per leaf.",
            "Fugui.Layouts loads layouts from StreamingAssets/Fugui/Layouts and uses layouts_index.json to expose them. The editor can create, assign, normalize and save docking layouts.",
            new string[]
            {
                "Window assignments are IDs, not titles.",
                "Set Fugui.Layouts.SetLayout(\"LayoutName\") to apply a layout.",
                "The main menu can expose layouts automatically when FuLayoutSetter enables it.",
                "If a window does not appear, check the ID is registered and the layout references it.",
                "Use the editor to avoid malformed JSON and duplicate node IDs."
            },
            "Layout application",
            @"Fugui.Layouts.SetLayout(""DemoScene"");

// A layout leaf references windows by ID:
// ""WindowsDefinition"": [4, 11, 13]",
            new string[] { "FDL", "dockspace", "layouts" }));

        _sections.Add(new DocSection(
            "layout-basics",
            "UI",
            "FuLayout Basics",
            "FuLayout is the immediate-mode drawing surface passed into every window UI callback.",
            "Use it for freeform UI: text, buttons, tabs, search boxes, popups, separators, spacing, tooltips, alignment helpers and low-level composition. It tracks the current item state so custom widgets can share hover, active and click behaviour.",
            new string[]
            {
                "Use layout.Text, SmartText, ClickableText, FramedText and TextURL for text.",
                "Use layout.Button, ButtonsGroup, Toggle, CheckBox, RadioButton, Combobox, SearchBox and Tabs for controls.",
                "Use layout.Surface, FeaturePanel, Callout, NavigationItem, Pill and PillRow for reusable framed content and badges.",
                "Use layout.GetAvailableWidth and GetAvailableHeight for responsive sizing.",
                "Use layout.SetNextElementToolTipWithLabel before a control to document intent.",
                "Direct ImGui calls can be mixed when custom drawing is required."
            },
            tags: new string[] { "FuLayout", "widgets", "immediate mode" }));

        _sections.Add(new DocSection(
            "layout-surfaces",
            "UI",
            "Surfaces And Pills",
            "FuLayout includes reusable surface and pill helpers for the clean framed blocks used by the demo documentation and tree tools.",
            "These helpers keep repeated visual language out of windows: use Surface for a drawn container, FeaturePanel for a title/body/banner block, Callout for highlighted text, NavigationItem for sidebar rows and Pill or PillRow for compact tag badges.",
            new string[]
            {
                "Surface returns the drawn Rect so custom controls can be placed inside it.",
                "FeaturePanel draws a modern content block with optional colored pills.",
                "Callout sizes itself around wrapped text and adds an accent stripe.",
                "NavigationItem provides selected, hovered and clicked states without the default orange ImGui selection.",
                "PillRow wraps compact badges across lines when space is tight."
            },
            "Reusable layout chrome",
            @"layout.FeaturePanel(
    ""tool-summary"",
    ""Build runtime tools with Fugui"",
    ""Dockable runtime windows, inspectors and overlays for Unity."",
    new string[] { ""Immediate mode"", ""Dockable"", ""Unity runtime"" });

layout.Callout(""api-note"", ""Surface helpers return their Rect for custom composition."");
layout.PillRow(""tags"", new string[] { ""runtime UI"", ""Dear ImGui"", ""Unity 6"" });",
            new string[] { "surface", "callout", "pill", "badge" }));

        _sections.Add(new DocSection(
            "grid",
            "UI",
            "FuGrid",
            "FuGrid is the ergonomic choice for inspectors, forms and repeated label/value rows.",
            "A grid keeps labels and fields aligned, supports responsive definitions and exposes most layout widgets with label-aware behaviour. The demo Inspector and Widgets windows use it heavily.",
            new string[]
            {
                "Use default grids for common two-column inspectors.",
                "Use FuGridDefinition for fixed, proportional or responsive columns.",
                "SetMinimumLineHeight improves dense tooling readability.",
                "Grid widgets include sliders, drags, toggles, comboboxes, color pickers, table views and buttons.",
                "DisableNextElements and EnableNextElements can gate a whole form section."
            },
            "Inspector-style grid",
            @"using (FuGrid grid = new FuGrid(""camera-grid"", outterPadding: 8f))
{
    grid.SetMinimumLineHeight(24f);
    grid.ComboboxEnum<CameraClearFlags>(""Clear Flags"", value =>
    {
        camera.clearFlags = (CameraClearFlags)value;
    }, () => camera.clearFlags);

    grid.Slider(""Field of view"", ref fov);
}",
            new string[] { "forms", "inspectors", "responsive" }));

        _sections.Add(new DocSection(
            "panels",
            "UI",
            "Panels And Scrolling",
            "FuPanel creates a scrollable child region with Fugui styling and optional clipping.",
            "Use one panel for a major scrolling area, such as a window body. Avoid nesting FuPanel instances; the implementation warns and switches state because only one active Fugui panel is tracked at a time.",
            new string[]
            {
                "FuPanel(id) creates a scrollable panel using the current style.",
                "FuPanel(id, FuStyle.Unpadded) is useful for full-window content.",
                "FuPanelFlags.NoScroll disables scrolling.",
                "FuPanelFlags.DrawBorders adds a visible border.",
                "Use collapsables inside panels for long documentation or inspector categories."
            },
            tags: new string[] { "FuPanel", "scroll", "clipper" }));

        _sections.Add(new DocSection(
            "widgets",
            "UI",
            "Widget Catalog",
            "Fugui ships a broad set of high-level widgets for runtime tools.",
            "The Widgets demo window is the living catalog. It shows text, buttons, toggles, list controls, date pickers, file fields, colors, sliders, drags, knobs, progress bars, spinners, search, table view and charts.",
            new string[]
            {
                "Text: Text, SmartText, TextURL, FramedText, clipped and boxed text helpers.",
                "Actions: Button, ButtonsGroup, image buttons and transparent icon buttons.",
                "Input: CheckBox, Toggle, RadioButton, Combobox, ListBox, Drag, Slider, Range, Knob and DateTime.",
                "Data: SearchBox, TableView and sortable/searchable columns.",
                "Feedback: ProgressBar, loaders, spinners, notifications and modals.",
                "Media: Texture2D, RenderTexture images and video player."
            },
            "Search and table pattern",
            @"layout.SearchBox(""items-search"", filter, ""Search items..."");

layout.TableView(
    ""items-table"",
    items,
    columns,
    ref selectedIndex,
    filter.Query,
    item => item.Name + "" "" + item.Category
);",
            new string[] { "widgets", "table", "controls" }));

        _sections.Add(new DocSection(
            "charts",
            "UI",
            "Charts",
            "Charts wrap ImPlot-style plotting into Fugui-friendly series and options.",
            "Use charts for runtime telemetry, frame budgets, sensor data, simulation state and debug metrics. Options cover axis labels, ranges, legend, grid, tooltip, crosshair and custom draw callbacks.",
            new string[]
            {
                "FuChartSeries supports line, custom and other series types.",
                "FuChartOptions controls size, axis configuration and rendering flags.",
                "MaxRenderedPointsPerSeries keeps large telemetry streams efficient.",
                "AfterPlotDraw lets you add thresholds, bands or annotations.",
                "FuChartHoverState gives contextual data for inspectors."
            },
            tags: new string[] { "ImPlot", "telemetry", "graphs" }));

        _sections.Add(new DocSection(
            "styles-themes",
            "UI",
            "Themes, Styles, Fonts And Icons",
            "Themes define Fugui colors, spacing, rounding and widget feel; styles package common color sets for controls.",
            "Themes are loaded from StreamingAssets/Fugui/Themes. Fonts are configured through FontConfig and StreamingAssets/Fugui/Fonts/current. Icons use the Fugui icon font and the FuIcons or sample Icons classes.",
            new string[]
            {
                "Use Fugui.Themes.GetColor(FuColors.X) for theme-aware custom drawing.",
                "Use FuTextStyle, FuButtonStyle, FuFrameStyle and FuStyle presets for consistent widgets.",
                "Use Fugui.Push and PopColor or PopStyle around custom rendering.",
                "Use Fugui.PushFont(size, FontType.Bold) for headings or compact labels.",
                "Theme manager UI is available through Fugui.DrawThemes and can be shown in a modal."
            },
            "Theme-aware custom color",
            @"Vector4 accent = Fugui.Themes.GetColor(FuColors.Highlight, 0.8f);
Fugui.Push(ImGuiCol.Text, accent);
layout.Text(""Theme-aware text"");
Fugui.PopColor();",
            new string[] { "themes", "fonts", "icons" }));

        _sections.Add(new DocSection(
            "menus",
            "Systems",
            "Main Menu",
            "FuMainMenu can expose windows, layouts and Fugui tools through a global menu bar.",
            "FuLayoutSetter controls whether windows, layouts and the Fugui settings menu are added. Window menu items are driven by registered FuWindowName values and definitions.",
            new string[]
            {
                "Enable Add Windows To Main Menu to let users open registered windows.",
                "Enable Add Layouts To Main Menu to switch saved layouts at runtime.",
                "Enable Add Fugui To Main Menu to expose settings and theme tools.",
                "Force Hide Main Menu can produce a more kiosk-like runtime UI."
            },
            tags: new string[] { "main menu", "layout setter", "tools" }));

        _sections.Add(new DocSection(
            "popups-modals-notifications",
            "Systems",
            "Popups, Modals And Notifications",
            "Fugui includes transient UI for confirmations, theme tools, status messages and custom popups.",
            "Use modal helpers for blocking flows, notifications for non-blocking feedback, and OpenPopUp for custom anchored content. The Popups demo shows state-specific modal styles and notification variants.",
            new string[]
            {
                "Fugui.ShowInfo, ShowSuccess, ShowWarning and ShowDanger draw styled modal dialogs.",
                "Fugui.ShowModal accepts a custom body and FuModalButton actions.",
                "Fugui.Notify shows title, body and state-specific icon styling.",
                "Fugui.OpenPopUp and Fugui.DrawPopup manage custom popup lifecycle.",
                "FuSettings controls notification anchor, width, icons and duration."
            },
            "Modal and notification",
            @"if (layout.Button(""Delete"", FuButtonStyle.Danger))
{
    Fugui.ShowDanger(
        ""Remove item"",
        ""Are you sure?"",
        FuModalSize.Medium,
        new FuModalButton(""Delete"", DeleteItem, FuButtonStyle.Danger),
        new FuModalButton(""Cancel"", null, FuButtonStyle.Default));
}

Fugui.Notify(""Saved"", ""Layout saved successfully."", StateType.Success);",
            new string[] { "modal", "popup", "notify" }));

        _sections.Add(new DocSection(
            "context-menus",
            "Systems",
            "Context Menus",
            "Context menus are stackable and can be built with FuContextMenuBuilder.",
            "Push context menu items before drawing an element, then Fugui opens them on right click. Builders support separators, titles, children, images, shortcuts and enabled predicates.",
            new string[]
            {
                "Use FuContextMenuBuilder.Start() to create reusable menus.",
                "Use Fugui.PushContextMenuItems before a widget and PopContextMenuItems after it.",
                "Use BeginChild and EndChild for submenus.",
                "Context menus can include images and custom shortcuts."
            },
            "Reusable context menu",
            @"List<FuContextMenuItem> menu = FuContextMenuBuilder.Start()
    .AddItem(""Rename"", RenameSelection)
    .AddSeparator()
    .BeginChild(""Create"")
    .AddItem(""Folder"", CreateFolder)
    .AddItem(""Asset"", CreateAsset)
    .EndChild()
    .Build();

Fugui.PushContextMenuItems(menu);
layout.Text(""Right click me"");
Fugui.PopContextMenuItems();",
            new string[] { "right click", "builder", "submenu" }));

        _sections.Add(new DocSection(
            "overlays",
            "Systems",
            "Overlays",
            "FuOverlay draws a small anchored UI surface attached to a window definition or instance.",
            "Use overlays for FPS counters, quick settings, minimaps, badges or floating controls. Overlays can be anchored to window corners and configured for drag behaviour.",
            new string[]
            {
                "Create an overlay with an ID, size and drawing callback.",
                "AnchorWindowDefinition attaches it to every window created from that definition.",
                "FuOverlayAnchorLocation selects top, bottom, left, right and corner anchors.",
                "FuOverlayDragPosition controls how the overlay can be moved."
            },
            "Definition overlay",
            @"public override void OnWindowDefinitionCreated(FuWindowDefinition definition)
{
    FuOverlay overlay = new FuOverlay(
        ""fps-overlay"",
        new Vector2Int(102, 52),
        (overlay, layout) => DrawFps(layout),
        FuOverlayFlags.Default,
        FuOverlayDragPosition.Right);

    overlay.AnchorWindowDefinition(definition, FuOverlayAnchorLocation.TopRight);
}",
            new string[] { "overlay", "anchor", "HUD" }));

        _sections.Add(new DocSection(
            "drag-drop",
            "Systems",
            "Drag And Drop",
            "Fugui exposes typed payload drag and drop helpers over ImGui drag/drop primitives.",
            "Use typed payloads when building asset browsers, node editors, inspectors or reorderable tooling. Fugui tracks drag state globally so other systems can avoid conflicting interactions.",
            new string[]
            {
                "BeginDragDropSource starts a payload with a preview callback.",
                "BeginDragDropTarget<T> receives payloads of the requested type.",
                "CancelDragDrop cancels a named payload type.",
                "IsDraggingPayload and IsDraggingAnything help coordinate custom controls."
            },
            "Typed drop target",
            @"Fugui.BeginDragDropSource(""asset"", DrawPreview, payload);

Fugui.BeginDragDropTarget<MyAssetPayload>(""asset"", dropped =>
{
    AssignAsset(dropped.Asset);
});",
            new string[] { "payload", "asset browser", "typed" }));

        _sections.Add(new DocSection(
            "nodal-editor",
            "Systems",
            "Nodal Editor",
            "The nodal system provides graphs, nodes, typed ports, edges, registry, minimap, context menus and JSON serialization.",
            "The demo NodalEditorDemo registers variable and math nodes, assigns colors per data type and adds New, Save JSON and Load JSON actions to the editor context menu.",
            new string[]
            {
                "FuNodalGraph stores nodes, links and graph name.",
                "Registry.RegisterType defines typed port serialization and colors.",
                "Registry.RegisterNode exposes creatable node factories.",
                "FuNodalEditor draws the graph and can draw a minimap in an overlay.",
                "FuGraphSerializer converts graphs to and from JSON."
            },
            "Minimal nodal setup",
            @"_graph = new FuNodalGraph { Name = ""Demo Graph"" };
_editor = new FuNodalEditor(_graph, 0.5f, 2f, FuNodalEditorFlags.Default);

_graph.Registry.RegisterType(
    FuNodalType.Create<float>(""core/float"", 0f, v => v.ToString(), s => float.Parse(s)));

_graph.Registry.RegisterNode(""Variables/Float"", () => new FloatNode(Color.cyan));

public override void OnUI(FuWindow window, FuLayout layout)
{
    _editor.Draw(window);
}",
            new string[] { "nodes", "graph", "JSON" }));

        _sections.Add(new DocSection(
            "input-mobile",
            "Systems",
            "Input, Raycasting And Mobile",
            "Fugui tracks input per context and supports mouse, keyboard, 3D raycasting and mobile touch behaviour.",
            "FuRaycasting maps Unity events and 3D panels into Fugui mouse states. Mobile settings tune click, drag and scroll thresholds so the same UI can work on touch screens.",
            new string[]
            {
                "Fugui.GetWantCaptureInputs tells game code whether UI wants input.",
                "Fugui.GetKeyDown, GetKeyPressed and GetKeyUp expose keyboard state.",
                "FuWindow.Mouse and Keyboard expose window-local input.",
                "Fu3DWindowBehaviour and Test3DRaycaster demonstrate panel raycasting.",
                "Samples/MobileDemo shows a mobile shell with safe-area fitting, bottom navigation, charts and touch-oriented sliders."
            },
            tags: new string[] { "input", "touch", "raycast" }));

        _sections.Add(new DocSection(
            "external-windows",
            "Systems",
            "External Windows",
            "Fugui windows can be externalized into native containers when externalization support is enabled.",
            "External windows use FuExternalWindowContainer and a FuExternalContext backed by SDL/OpenTK. Settings and flags control native title bars, taskbar behaviour, focus, modality and context menus.",
            new string[]
            {
                "Externalization depends on the FU_EXTERNALIZATION compilation path and runtime settings.",
                "Use FuWindow.Externalize and Internalize to move between containers.",
                "FuExternalWindowFlags controls native behaviour.",
                "OnWindowExternalized, OnWindowDocked and OnWindowUnDocked events let tools react to container moves."
            },
            tags: new string[] { "native window", "SDL", "OpenTK" }));

        _sections.Add(new DocSection(
            "api-map",
            "Reference",
            "Core API Map",
            "The most important public surfaces are small enough to memorize by role.",
            "Start with FuController, FuWindowBehaviour and FuLayout. Add definitions, containers and specialized systems only when your tool needs them.",
            new string[]
            {
                "Fugui: global initialize, update, render, contexts, themes, layouts, windows, input, drag/drop and utilities.",
                "FuController: Unity component that boots and ticks the runtime.",
                "FuWindowName: stable ID and title for a window definition.",
                "FuWindowDefinition: declarative recipe for a window.",
                "FuWindow: live dockable window instance with focus, close, externalize, force draw and state APIs.",
                "FuLayout and FuGrid: widget drawing APIs, including reusable surfaces, callouts, navigation rows and pills.",
                "FuPanel: scrollable/stylable content region.",
                "FuOverlay: anchored mini-window.",
                "FuCameraWindowBehaviour and Fu3DWindowBehaviour: specialized scene integration."
            },
            "Common calls",
            @"Fugui.CreateWindow(windowName);
Fugui.IsWindowOpen(windowName);
Fugui.GetWindowInstances(windowName);
Fugui.RefreshWindowsInstances(windowName);
Fugui.CloseAllWindows();

window.ForceDraw();
window.Focus();
window.Close();
window.Externalize();
window.Internalize();",
            new string[] { "cheat sheet", "API", "classes" }));

        _sections.Add(new DocSection(
            "integration-checklist",
            "Reference",
            "Integration Checklist",
            "Use this checklist when adding Fugui to a new scene or when a demo window does not appear.",
            "Most issues come from missing controller setup, missing renderer feature, missing StreamingAssets, unregistered window names or layout IDs that do not match current names.",
            new string[]
            {
                "FuController exists in the scene and references FuSettings and the UI camera.",
                "The active URP renderer has FuguiRenderFeature and the Fugui URP mesh shader.",
                "StreamingAssets/Fugui/Fonts/current contains regular.ttf, bold.ttf and icons.ttf.",
                "Themes and layouts folders exist and their index JSON files list the available assets.",
                "Every custom window has a unique FuWindowName and appears in GetAllWindowsNames.",
                "Every FuWindowBehaviour has its _windowName set in the inspector or by code before FuguiAwake.",
                "The active .fdl layout references the intended window IDs.",
                "Window flags do not disable docking or closing unless that is intended.",
                "If using 3D windows, panel layer, raycaster, render resolution and scale config are valid."
            },
            tags: new string[] { "checklist", "scene setup", "debugging" }));

        _sections.Add(new DocSection(
            "troubleshooting",
            "Reference",
            "Troubleshooting",
            "The fastest fix is to identify which layer failed: controller, renderer, assets, window definition, layout or input.",
            "Fugui exposes editor setup tools and runtime errors through OnUIException. For layout problems, inspect the .fdl file and confirm every ID exists in the active window registry.",
            new string[]
            {
                "Nothing renders: check FuController, UI camera, URP render feature and camera layer.",
                "Fonts or icons are missing: check FontConfig and StreamingAssets/Fugui/Fonts/current.",
                "Layout is not loaded: check layouts_index.json, layout name and file path.",
                "A window is missing: check FuWindowName ID, GetAllWindowsNames and the active layout WindowsDefinition list.",
                "Externalization does nothing: check settings, compile symbol and FuExternalWindowFlags.",
                "3D panel input misses: check raycaster, panel collider/mesh, layer masks, camera and render resolution."
            },
            tags: new string[] { "debug", "layout", "rendering" }));

        _sections.Add(new DocSection(
            "maintenance",
            "Reference",
            "Maintenance Notes",
            "Keep generated or editor-managed data stable, especially Unity GUIDs, FuWindowName IDs and layout node IDs.",
            "Fugui is immediate-mode, so expensive UI data should be cached outside OnUI and only recomputed when source data changes. Use ForceDraw for event-driven idle windows and keep custom drawing theme-aware.",
            new string[]
            {
                "Do not change existing FuWindowName IDs after layouts ship.",
                "Prefer editor tools for layout and window-name updates.",
                "Cache large tables, trees and chart series outside OnUI.",
                "Use search filters, clipping panels and max rendered chart points for large datasets.",
                "Respect user theme colors instead of hard-coded palettes.",
                "Keep sample windows focused: one behaviour, one window name, one clear demo purpose."
            },
            tags: new string[] { "performance", "IDs", "theme" }));
    }
    #endregion
}
