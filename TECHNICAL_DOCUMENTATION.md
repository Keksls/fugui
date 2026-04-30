# Documentation technique Fugui Unity6

Cette documentation decrit l'architecture et les APIs principales du package Fugui. Elle a ete generee a partir de l'inventaire complet du depot, de tous les fichiers texte exploitables et des signatures publiques du runtime.

## Inventaire lu

- 1226 fichiers detectes dans le package.
- 475 fichiers texte lus sans erreur: C#, JSON, Markdown, shaders, HLSL, layouts, themes, scenes/prefabs Unity textuels, assets textuels, headers et fichiers de config.
- Les fichiers binaires ont ete inventories par type: DLL, PNG, TTF, PDF, ZIP, libs natives, databases Unity/IDE.
- 349 fichiers C# detectes, incluant le runtime Fugui, les samples et les bindings ImGui/SDL.

## Assemblies et dependances

`Runtime/Fugui.asmdef`

- Assembly: `Fugui`.
- `allowUnsafeCode`: `true`.
- References precompilees: `System.Runtime.CompilerServices.Unsafe.dll`, `ImGui.NET.dll`, `ImGuizmo.NET.dll`, `ImNodes.NET.dll`, `ImPlot.NET.dll`, `OpenTK.dll`, `Triangle.dll`, `Ookii.Dialogs.dll`, `Newtonsoft.Json.dll`.
- References Unity par GUID.
- Auto reference active.

`Package.json`

- Nom: `com.keksls.fugui`.
- Version: `0.1.2`.
- Display name: `Fugui Unity6`.
- Dependence package: `com.unity.postprocessing` version `3.2.2`.
- Sample declare: `Samples~/Demo`.

## Architecture generale

Fugui est compose de quatre couches.

1. Core runtime:
   - `Fugui` orchestre l'initialisation, les contextes, les fenetres, les containers, le rendu, les inputs, les fonts, les callbacks et le drag/drop.
   - `FuController` relie le runtime a la boucle Unity.
   - `FuContext`, `FuUnityContext`, `FuExternalContext` encapsulent les contextes ImGui.
   - `TextureManager`, `FontSet`, `SpriteInfo` gerent textures, atlas et fonts.

2. Containers et windows:
   - `FuMainWindowContainer`: container principal dans la camera Unity.
   - `Fu3DWindowContainer`: container offscreen rendu sur panneau 3D.
   - `FuExternalWindowContainer`: container natif SDL/OpenTK, compile sous `FU_EXTERNALIZATION`.
   - `FuWindow`: fenetre UI standard.
   - `FuCameraWindow`: fenetre specialisee pour une camera.
   - `FuOverlay`: sous-fenetre ancree a une fenetre.

3. Framework utilisateur:
   - `FuLayout`: API de dessin immediate-mode.
   - `FuGrid`: layout tabulaire responsive.
   - `FuPanel`: panel scrollable/stylable.
   - Widgets: boutons, textes, inputs, sliders, ranges, lists, gradients, videos, loaders, etc.
   - Styles/themes: `FuStyle`, `FuButtonStyle`, `FuTextStyle`, `FuTheme`, `FuThemeManager`.

4. Outils:
   - `FuDockingLayoutManager`: creation, chargement, sauvegarde et application des layouts.
   - `FuMainMenu`: menu principal.
   - `FuContextMenuBuilder`: menus contextuels.
   - `FuNodalEditor`: editeur de graphes nodaux.
   - `FileBrowser`: file/folder panels natifs.

## Cycle de vie

### Initialisation

`FuController.Awake()`:

1. appelle `Fugui.Initialize(_settings, this, _uiCamera)`;
2. attache `Fugui.OnUIException` au log Unity si `_logErrors` est actif;
3. parcourt les `MonoBehaviour` de la scene, actifs ou inactifs, et leur envoie `FuguiAwake`.

`Fugui.Initialize`:

1. initialise les dictionnaires de fenetres et definitions;
2. cree les managers de themes/layouts;
3. initialise le handler d'assert ImGui;
4. cree `DefaultContext` via `CreateUnityContext`;
5. cree `DefaultContainer`;
6. applique la config de scale;
7. enregistre la fenetre systeme `FuguiSettings`.

### Update

`FuController.FuUpdate()`:

```csharp
FuRaycasting.Update();
Fugui.Update();
Fugui.Render();
```

`Fugui.Update()` execute les callbacks planifies sur le main thread et met a jour `Fugui.Time`.

### Render

`Fugui.Render()` prepare les donnees ImGui de chaque fenetre/context. Le dessin GPU est ensuite fait par `FuguiRenderFeature`.

### Nettoyage

`FuController.Dispose()` appelle `Fugui.Dispose()`, ferme les contextes et, sous `FU_EXTERNALIZATION`, quitte SDL.

## API globale `Fugui`

### Etat principal

- `CurrentContext`: contexte ImGui courant.
- `Scale`: scale du contexte courant.
- `Contexts`: tous les contextes enregistres.
- `Settings`: settings runtime.
- `Time`: temps unscaled partage.
- `DefaultContainer`: container principal.
- `DefaultContext`: contexte Unity principal.
- `UIWindows`: instances de fenetres.
- `UIWindowsDefinitions`: definitions de fenetres.
- `Themes`: `FuThemeManager`.
- `Layouts`: `FuDockingLayoutManager`.

### Evenements

- `OnUIException(Exception)`.
- `OnWindowExternalized(FuWindow)`.
- `OnWindowResized(FuWindow)`.
- `OnWindowClosed(FuWindow)`.
- `OnWindowDocked(FuWindow)`.
- `OnWindowUnDocked(FuWindow)`.
- `OnWindowAddToContainer(FuWindow)`.
- `OnWindowRemovedFromContainer(FuWindow)`.

### Fenetres

```csharp
Fugui.RegisterWindowDefinition(definition);
Fugui.UnregisterWindowDefinition(definition);
Fugui.CreateWindow(windowName);
Fugui.CreateWindowAsync(windowName, callback);
Fugui.CreateWindows(names);
Fugui.CloseAllWindows();
Fugui.IsWindowOpen(windowName);
Fugui.GetWindowInstances(windowName);
Fugui.GetNbWindowInstances(windowName);
Fugui.RefreshWindowsInstances(windowName);
```

### Contextes

```csharp
FuUnityContext ctx = Fugui.CreateUnityContext(camera, scale: 1f, fontScale: 1f);
FuUnityContext offscreen = Fugui.CreateUnityContext(pixelRect, scale: 1f, fontScale: 1f);
Fugui.SetCurrentContext(ctx);
Fugui.DestroyContext(ctx);
```

### UI 3D

```csharp
Fugui.Add3DWindow(window, settings, position, rotation);
Fugui.Add3DWindow(window, panelSize, renderResolution);
Fugui.Add3DWindowScaledWithPanel(window, panelSize, referenceResolution, referencePanelSize);
```

### Styles, couleurs et fonts

```csharp
Fugui.Push(ImGuiCol.Text, Color.white);
Fugui.Push(FuColors.Text, Color.white);
Fugui.Push(ImGuiStyleVar.FramePadding, new Vector2(8, 4));
Fugui.PushFont(18, FontType.Bold);

Fugui.PopColor();
Fugui.PopStyle();
Fugui.PopFont();
```

### Inputs

```csharp
bool captured = Fugui.GetWantCaptureInputs(onlyCurrentContext: true);
bool down = Fugui.GetKeyDown(FuKeysCode.Space);
bool pressed = Fugui.GetKeyPressed(FuKeysCode.Space);
bool up = Fugui.GetKeyUp(FuKeysCode.Space);
FuMouseState mouse = Fugui.GetCurrentMouse();
```

### Drag and drop

```csharp
Fugui.BeginDragDropSource("asset", ImGuiDragDropFlags.None, DrawPreview, payload);
Fugui.BeginDragDropTarget<MyPayload>("asset", OnDrop);
Fugui.CancelDragDrop("asset");
bool active = Fugui.IsDraggingPayload("asset");
```

### Utilitaires

- `CalcTextSize`.
- `IsDuoToneChar`.
- `DrawDuotoneSecondaryGlyph`.
- `ColorsAreEqual`.
- `IsAnyWindowDragging`.
- `IsAnyWindowHoverContent`.
- `IsAnyOverlayDragging`.
- `IsDraggingAnything`.
- `ReadAllBytes`.
- `ReadAllText`.
- `ExecuteAfterRenderWindows`.
- `ExecuteBeforeRenderWindows`.
- `ExecuteInMainThread`.
- `ForceDrawAllWindows`.
- `SetScale`.

## `FuController`

Composant MonoBehaviour de bootstrap.

Champs:

- `_settings`: instance de `FuSettings`.
- `_uiCamera`: camera cible du container principal.
- `_logErrors`: log des exceptions UI.
- `_updateMode`: `Update`, `LateUpdate` ou `Manual`.

Mode manuel:

```csharp
public class ManualDriver : MonoBehaviour
{
    [SerializeField] private FuController _controller;

    private void Update()
    {
        if (ShouldRenderUi())
        {
            _controller.FuUpdate();
        }
    }
}
```

## `FuSettings`

`FuSettings` centralise les parametres runtime.

Groupes principaux:

- chemins: `ThemesFolder`, `LayoutsFolder`;
- fenetres: `EnableExternalizations`, `IdleFPS`, `ExternalWindowFlags`;
- 3D: `Windows3DScale`, `Windows3DSuperSampling`, `Windows3DFontScale`, `UIPanelWidth`;
- UI: `GlobalScale`, `FontGlobalScale`, animations, icons, materials, cursors;
- containers: `EnableContainerScaler`, `ContainerReferenceResolution`, `ContainerMatchWidthOrHeight`, min/max scale, font scaling;
- docking: `DockingFlags`, `DisplayTabBarButton`, `TabBarButtonRight`;
- ImGui IO: double-click, drag threshold, key repeat, framebuffer scale, docking options, cursor blink, window resizing;
- mobile: seuils click/drag mobiles;
- raycast: `UILayer`, `UIRaycastDistance`;
- notifications: anchor, width, icon size, default duration.

`ApplyTo(ImGuiIOPtr io)` applique les settings ImGui.

## Fenetres

### `FuWindowName`

Identifie une definition de fenetre.

```csharp
new FuWindowName(
    id: 42,
    name: "Inspector",
    autoInstantiateWindowOnlayoutSet: true,
    idleFPS: -1
);
```

`idleFPS = -1` utilise `FuSettings.IdleFPS`.

### `FuWindowDefinition`

Definition declarative:

```csharp
var definition = new FuWindowDefinition(
    windowName,
    Draw,
    pos: new Vector2Int(50, 50),
    size: new Vector2Int(420, 320),
    flags: FuWindowFlags.AllowMultipleWindow,
    externalFlags: FuExternalWindowFlags.Default
);

definition
    .SetHeaderUI(DrawHeader, 24f)
    .SetFooterUI(DrawFooter, 20f)
    .SetCustomWindowType(def => new MyWindow(def));
```

Flags:

- `NoExternalization`;
- `NoDocking`;
- `NoInterractions`;
- `NoDockingOverMe`;
- `AllowMultipleWindow`;
- `NoClosable`;
- `NoAutoRegisterWindowDefinition`.

External flags:

- `UseNativeTitleBar`;
- `NoTaskBarIcon`;
- `NoFocusOnAppearing`;
- `AlwaysOnTop`;
- `NoModal`;
- `NoNotify`;
- `NoContextMenu`;
- `Default = UseNativeTitleBar | NoModal | NoNotify`.

### `FuWindow`

Expose les etats et operations de fenetre:

- `ForceDraw(int nbFrames = 1)`;
- `MustBeDraw()`;
- `Externalize()`;
- `Internalize()`;
- `TryAddToContainer(container)`;
- `TryRemoveFromContainer()`;
- `AutoDock()`;
- `Close()`;
- `ForceFocusOnNextFrame()`;
- `UpdateState(leftMouseButtonState)`;
- `AddWindowFlag(ImGuiWindowFlags flag)`;
- `RemoveWindowFlag(ImGuiWindowFlags flag)`.

Etats notables:

- idle/manipulating;
- opened/visible/hovered/docked/external;
- resizing/dragging;
- target FPS/current FPS;
- working area, size, position;
- mouse/keyboard associes.

## Behaviours Unity

### `FuWindowBehaviour`

Base MonoBehaviour pour fenetres.

Override principaux:

```csharp
public override void OnWindowDefinitionCreated(FuWindowDefinition definition) {}
public override void OnWindowCreated(FuWindow window) {}
public override void OnUI(FuWindow window, FuLayout layout) {}
```

### `FuCameraWindowBehaviour`

Specialisation pour camera:

- `_camera`;
- `_msaa`;
- `_idleCameraFPS`;
- `_manipulatingCameraFPS`;
- `_superSampling`;
- `CameraWindow => _fuWindow as FuCameraWindow`.

### `Fu3DWindowBehaviour`

Specialisation world-space:

- cree ou attache une `FuWindow`;
- cree un `Fu3DWindowContainer`;
- synchronise position, rotation et taille depuis le transform;
- verrouille `scale.z` sur `Depth`;
- propose handles editor pour resize;
- expose `Create3DWindow`, `AttachWindow`, `Close3DWindow`, `SetRuntimeResizable`, `SetContainerScaleConfig`, `Get3DWindowSettings`.

## Containers

### `IFuWindowContainer`

Contrat de container: ajout/suppression de fenetres, rendu, scale config, position, taille, souris/clavier, contexte.

### `FuContainerScaleConfig`

Deux modes principaux:

```csharp
FuContainerScaleConfig.Disabled(baseScale, baseFontScale);

FuContainerScaleConfig.Reference(
    referenceResolution,
    matchWidthOrHeight,
    minScale,
    maxScale,
    baseScale,
    baseFontScale,
    scaleFont
);
```

### `FuMainWindowContainer`

Container du contexte Unity principal. Gere dockspace principal, footer optionnel, rendu des fenetres et liaison avec la camera UI.

### `Fu3DWindowContainer`

Container offscreen sur render texture + mesh/panel 3D.

APIs:

- `SetRuntimeResizable`;
- `Set3DScale`;
- `SetContainerScaleConfig`;
- `Set3DWindowSettings`;
- `SetRenderResolution`;
- `SetPanelDepth`;
- `SetPosition`;
- `SetRotation`;
- `SetLocalSize`;
- `GetTextureID`;
- `ImGuiImage`;
- `ImGuiImageButton`;
- `Close`.

### `Fu3DWindowSettings`

Factories:

```csharp
Fu3DWindowSettings.FixedResolution(panelSize, resolution);
Fu3DWindowSettings.ScaledResolutionWithPanel(panelSize, referenceResolution, referencePanelSize);
Fu3DWindowSettings.FixedResolutionMatchingPanelAspect(panelSize, referenceResolution, referencePanelSize);
```

Methodes:

- `Sanitize`;
- `ComputeResolution`;
- `ComputeAspectMatchedResolution`.

### `FuExternalWindowContainer`

Compile sous `FU_EXTERNALIZATION`. Gere une fenetre externe native avec contexte external, rendu et manipulation.

## Rendu

`FuguiRenderFeature` est une `ScriptableRendererFeature` URP.

Fonctionnement:

- filtre les cameras par `_cameraLayer`;
- cree un pass par camera;
- rend le contexte default sur `activeColorTexture`;
- rend les contextes offscreen sur leurs `RenderTexture`;
- ignore les `FuExternalContext` sous `FU_EXTERNALIZATION`;
- convertit les `DrawData` Fugui en mesh Unity dynamique;
- applique scissor rect par `ImDrawCmd`;
- resolve les textures via `TextureManager`.

Champs:

- `PassEvent`;
- `_shader`;
- `_cameraLayer`;
- `_passPerCamera`.

Shaders:

- `Fugui_URP_Mesh.shader`;
- `Fugui_HDRP_Mesh.shader`;
- `URP_DebugPositionOnly.shader`;
- `3DWindowShader.shader`;
- `Common.hlsl`;
- `PassesHD.hlsl`.

## Layouts

### `FuLayout`

Base de dessin immediate-mode. Points cles:

- stack `CurrentDrawerPath`;
- gestion begin/end element;
- tooltips;
- disabled state;
- animations;
- alignements;
- groups;
- helpers de mesure.

Methodes structurelles:

```csharp
layout.SetNextElementToolTip("line 1", "line 2");
layout.SetNextElementToolTipWithLabel("description");
layout.SetNextElementToolTipStyles(FuTextStyle.Info);
layout.DisableNextElement();
layout.DisableNextElements();
layout.EnableNextElements();
layout.Separator();
layout.Spacing();
layout.SameLine();
layout.Dummy(8f, 8f);
layout.BeginGroup();
layout.EndGroup();
layout.CenterNextItemH("Text");
layout.CenterNextItemV("Text", height);
layout.CenterNextItemHV("Text", width, height);
```

### `FuGrid`

Herite de `FuLayout`, utilise `ImGui.BeginTable`.

Constructeurs:

```csharp
new FuGrid("id");
new FuGrid("id", FuGridDefinition.DefaultFixed, FuGridFlag.LinesBackground);
```

Operations:

- `NextColumn`;
- `NextLine`;
- `SetMinimumLineHeight`;
- `NextElementYPadding`;
- `DrawObject<T>` pour AutoGrid.

Definitions:

- `DefaultAuto`: colonnes auto;
- `DefaultFixed`: deux colonnes, premiere fixe 96 px;
- `DefaultRatio`: ratio 50/50;
- `DefaultFlexible`: colonnes flexibles selon largeur.

Flags:

- `LinesBackground`;
- `NoAutoLabels`;
- `DoNotDisableLabels`;
- `AutoToolTipsOnLabels`.

## Widgets

Tous les widgets de `FuLayout` sont disponibles sur `FuGrid`.

### Texte

```csharp
layout.Text("Default");
layout.Text("Info", FuTextStyle.Info);
layout.Text("Long text", FuTextWrapping.Wrap);
layout.SmartText("Hello <b>bold</b> <color=red>red</color>");
layout.TextURL("docs", "https://example.com");
bool clicked = layout.ClickableText("Click");
layout.FramedText("Status", FuElementSize.AutoSize);
```

Styles texte: `Default`, `Deactivated`, `Selected`, `Highlight`, `Info`, `Warning`, `Danger`, `Success`.

### Boutons

```csharp
layout.Button("Button");
layout.Button("Danger", FuButtonStyle.Danger);
layout.Button("Sized", new FuElementSize(120f, 28f));
layout.ButtonsGroup("Mode", labels, index => selected = index);
layout.ButtonsGroup<MyEnum>("Enum", value => selected = value);
```

Styles bouton: `Default`, `Transparent`, `Selected`, `Highlight`, `Info`, `Success`, `Warning`, `Danger`, `Collapsable`.

### Inputs numeriques

```csharp
layout.Slider("Float", ref value, 0f, 1f);
layout.Slider("Int", ref count, 0, 10);
layout.Range("Range", ref min, ref max, 0f, 100f);
layout.Drag("Vector3", ref position, "X", "Y", "Z", -10f, 10f);
layout.Knob("Gain", ref gain, 0f, 1f, FuKnobVariant.Wiper);
```

Flags slider:

- `LeftDrag`;
- `NoDrag`;
- `NoKnobs`;
- `UpdateOnBarClick`.

### Booleens et selection

```csharp
layout.CheckBox("Enabled", ref enabled);
layout.Toggle("Power", ref power, "Off", "On");
layout.RadioButton("Option", selected);
layout.Combobox("Items", items, item => selected = item);
layout.ComboboxEnum<MyEnum>("Mode", i => mode = (MyEnum)i, () => mode);
layout.ListBox("List", items, item => selected = item);
layout.Tabs("tabs", new[] { "A", "B" }, index => currentTab = index);
```

### Recherche et tables

```csharp
layout.SearchBox("objects-search", ref search, "Search...");

layout.TableView(
    "objects-table",
    objects,
    new[]
    {
        new FuTableViewColumn<MyObject>("Name", x => x.Name),
        new FuTableViewColumn<MyObject>("Category", x => x.Category),
        new FuTableViewColumn<MyObject>("Count", x => x.Count.ToString(),
            sortComparison: (a, b) => a.Count.CompareTo(b.Count))
    },
    ref selectedSourceIndex,
    search,
    height: 260f,
    flags: FuTableViewFlags.Default | FuTableViewFlags.ScrollY);
```

`TableView` conserve la selection comme index de la liste source, meme apres filtre ou tri. Les colonnes textuelles sont triables par defaut; les colonnes custom peuvent fournir `sortComparison` et `searchGetter`.

### Charts

Les charts sont rendus en drawlist ImGui via `Chart`, `FuChartSeries` et `FuChartOptions`. Les types integres sont `Line`, `Area`, `Bar`, `Scatter` et `Custom`.

```csharp
var series = new List<FuChartSeries>()
{
    new FuChartSeries("Signal", signalPoints, FuChartSeriesType.Line),
    new FuChartSeries("Fill", envelopePoints, FuChartSeriesType.Area)
    {
        Baseline = 0f,
        FillAlpha = 0.20f
    },
    FuChartSeries.Custom("Overlay", (ctx, s) =>
    {
        Vector2 left = ctx.ToScreen(new Vector2(ctx.Min.x, 0.5f));
        Vector2 right = ctx.ToScreen(new Vector2(ctx.Max.x, 0.5f));
        ctx.DrawList.AddLine(left, right, Fugui.Themes.GetColorU32(FuColors.TextWarning), 1.5f);
    })
};

var options = new FuChartOptions()
{
    Size = new FuElementSize(-1f, 260f),
    Flags = FuChartFlags.Default | FuChartFlags.ZeroLine,
    MaxRenderedPointsPerSeries = 2048
};
options.XAxis.Label = "Time";
options.YAxis.Label = "Value";
options.YAxis.SetAutoRange(includeZero: true);

FuChartHoverState hover;
layout.Chart("chart", series, options, out hover);
```

`FuChartOptions.BeforePlotDraw`, `AfterPlotDraw` et `FuChartSeries.Custom` exposent `FuChartDrawContext`, qui donne acces au `DrawList`, au `PlotRect`, aux bornes `Min/Max`, au hover courant et aux conversions `ToScreen` / `ToValue`. Pour les gros datasets, `MaxRenderedPointsPerSeries` limite le nombre de segments/points dessines et utilises par le hit-test; fixez les ranges des axes pour eviter le scan automatique si les donnees sont deja bornees.

### Images, couleurs, gradients

```csharp
layout.Image("Logo", texture, new FuElementSize(64f, 64f));
layout.ImageButton("Logo Button", texture, Vector2.one * 32f, Vector2.zero);
layout.ColorPicker("Tint", ref color);

FuGradient gradient = new FuGradient();
gradient.AddColorKey(0f, Color.black);
gradient.AddColorKey(1f, Color.white);
Texture2D preview = gradient.GetGradientTexture();
```

### Panels et collapsables

```csharp
using (new FuPanel("panel", FuStyle.Unpadded))
{
    layout.Collapsable("Section", () =>
    {
        layout.Text("Content");
    });
}
```

### Fichiers

```csharp
layout.InputFile("File", path => selectedFile = path, selectedFile,
    new ExtensionFilter("Images", "png", "jpg"));

layout.InputFolder("Folder", path => selectedFolder = path, selectedFolder);
```

### Loaders

Methodes:

- `Loader_CircleSpinner`;
- `Loader_Spinner`;
- `Loader_Clocker`;
- `Loader_Pulsar`;
- `Loader_PulsingLines`;
- `Loader_SquareCircleDance`;
- `Loader_WavyLine`;
- `Loader_Squares`;
- `Loader_SpikedWheel`;
- `Loader_Wheel`;
- `Loader_ElipseSpinner`.

### Video

```csharp
FuVideoPlayer player = layout.GetVideoPlayer("preview");
player.SetFile(path);
player.SetLoop(true);
player.Play();
player.DrawImage(320f, 180f);
player.DrawTimeLine();
```

## AutoGrid

Le systeme AutoGrid dessine un objet a partir d'attributs.

Attributs disponibles:

- `FuHidden`;
- `FuTooltip`;
- `FuDisabled`;
- `FuText`;
- `FuTextField`;
- `FuToggle`;
- `FuToggleField`;
- `FuSlider`;
- `FuSliderField`;
- `FuDrag`;
- `FuDragField`;
- `FuCheckboxField`;
- `FuColorPicker`;
- `FuColorPickerField`;
- `FuComboboxField`;
- `FuImage`;
- `FuImageField`;
- `FuNonEditableField`.

Exemple:

```csharp
public class ToolSettings
{
    [FuTooltip("Display name")]
    [FuTextField]
    public string Name = "Tool";

    [FuToggle]
    public bool Enabled = true;

    [FuSlider(0f, 100f)]
    public float Strength = 50f;
}

using (var grid = new FuGrid("tool-settings"))
{
    grid.DrawObject("tool", settings);
}
```

## Panels

`FuPanel` encadre une zone scrollable/stylable.

```csharp
using (new FuPanel("content", FuPanelStyle.Default, FuPanelFlags.Default))
{
    layout.Text("Scrollable content");
}
```

Styles panel:

- `Default`;
- `PopUp`;
- `Transparent`.

Flags panel:

- `NoScroll`;
- `DrawBorders`.

## Themes

`FuTheme` contient les couleurs, tailles, paddings et styles ImGui/Fugui. `FuThemeManager` charge/sauve les themes dans le dossier de settings.

Exemple:

```csharp
if (Fugui.Themes.GetTheme("DarkSky", out FuTheme theme))
{
    Fugui.Themes.SetTheme(theme);
}

Vector4 text = Fugui.Themes.GetColor(FuColors.Text);
uint textU32 = Fugui.Themes.GetColorU32(FuColors.Text);
```

Couleurs `FuColors`:

Le fichier `Runtime/Framework/Themes/FuColors.cs` definit l'enum de couleurs internes utilisees par les styles et widgets.

Extension:

```csharp
public enum MyColors
{
    WarningBorder = 1000,
    GraphNode = 1001
}

Fugui.Themes.ExtendThemes(typeof(MyColors));
```

## Styles

Styles implementant `IFuElementStyle`:

- `FuStyle`: styles generiques/layout/grid.
- `FuPanelStyle`: panels.
- `FuFrameStyle`: frames.
- `FuButtonStyle`: boutons.
- `FuButtonsGroupStyle`: groupes de boutons.
- `FuComboboxStyle`: combobox.
- `FuCollapsableStyle`: collapsables.
- `FuTextStyle`: textes.

Chaque style pousse/popup les couleurs et variables ImGui necessaires autour de l'element.

## Docking layouts

### `FuDockingLayoutDefinition`

Structure serialisable:

- `Name`;
- `ID`;
- `Proportion`;
- `Orientation`;
- `Children`;
- `WindowsDefinition`;
- `AutoHideTopBar`;
- `LayoutType`.

Methodes:

- `GetTotalChildren`;
- `Serialize`;
- `Deserialize`.

### `FuDockingLayoutManager`

Etat:

- `CurrentLayout`;
- `Layouts`;
- `IsSettingLayout`;
- events `OnDockLayoutSet`, `OnBeforeDockLayoutSet`, `OnDockLayoutReloaded`.

Operations:

```csharp
Fugui.Layouts.LoadLayouts(folder);
Fugui.Layouts.SetLayout("Default");
Fugui.Layouts.AutoDockWindow(window);
Fugui.Layouts.SaveLayoutFile(folder, layout);
Fugui.Layouts.DeleteLayout(folder, "MyLayout");
FuDockingLayoutDefinition current = Fugui.Layouts.GenerateCurrentLayout();
```

`SetLayout` reporte automatiquement la demande si des transitions de fenetre/container sont en cours.

## Main menu

API:

```csharp
Fugui.RegisterMainMenuItem("File/Save", Save, shortcut: "Ctrl+S");
Fugui.RegisterMainMenuSeparator("File");
Fugui.DisableMainMenuItem("File/Save");
Fugui.EnableMainMenuItem("File/Save");
Fugui.HideMainMenu();
Fugui.ShowMainMenu();
Fugui.RenderMainMenu();
```

`OnMainMenuDraw(Rect)` est emis lors du dessin.

## Menus contextuels

Builder:

```csharp
var items = FuContextMenuBuilder.Start()
    .SetTitle("Actions")
    .AddItem("Rename", Rename, shortcut: "F2")
    .AddSeparator()
    .BeginChild("Advanced")
    .AddItem("Reset", Reset)
    .EndChild()
    .Build();
```

Stack API:

```csharp
Fugui.PushContextMenuItems(items);
Fugui.TryOpenContextMenuOnItemClick(FuMouseButton.Right);
Fugui.PopContextMenuItems();
```

## Popups et modales

Popups:

```csharp
Fugui.OpenPopUp("id", DrawUi, new Vector2(240, 160), onClose);
Fugui.DrawPopup("id");
Fugui.ClosePopup("id");
Fugui.ForceCloseOpenPopup();
```

Modales:

```csharp
Fugui.ShowModal("Title", body, FuModalSize.Medium, buttons);
Fugui.ShowYesNoModal("Confirm", ok => {}, FuModalSize.Small);
Fugui.ShowInfo("Info", "Message", FuModalSize.Medium);
Fugui.ShowWarning("Warning", "Message", FuModalSize.Medium);
Fugui.ShowDanger("Danger", "Message", FuModalSize.Medium);
Fugui.ShowSuccess("Success", "Message", FuModalSize.Medium);
Fugui.CloseModal();
```

`FuModalButton` accepte texte, callback, style et raccourci clavier.

## Notifications

`Fugui.Notify` est expose dans les widgets notification. Les notifications utilisent:

- `StateType`: `Danger`, `Success`, `Info`, `Warning`;
- `NotificationAnchorPosition`;
- `NotifyPanelWidth`;
- `NotifyIconSize`;
- `NotifyAnimlationDuration`;
- stacking de notifications identiques;
- collapse force via `FuNotification`.

Exemple:

```csharp
Fugui.Notify("Import", "Termine", StateType.Success, duration: 3f);
```

## Overlays

Creation:

```csharp
FuOverlay overlay = new FuOverlay(
    "stats",
    new Vector2Int(160, 80),
    (o, layout) => layout.Text("FPS"),
    FuOverlayFlags.Default,
    FuOverlayDragPosition.Right
);

overlay.AnchorWindowDefinition(definition, FuOverlayAnchorLocation.TopRight);
```

APIs:

- `Show`;
- `Hide`;
- `AnchorWindowDefinition`;
- `AnchorWindow`;
- `SetMinimumWindowSize`;
- `SetStyle`;
- `SetLocalRect`.

Flags:

- `NoMove`;
- `NoClose`;
- `NoBackground`;
- `NoEditAnchor`.

Ancrages:

- `TopLeft`, `TopCenter`, `TopRight`;
- `MiddleLeft`, `MiddleCenter`, `MiddleRight`;
- `BottomLeft`, `BottomCenter`, `BottomRight`.

## File browser

API typique:

```csharp
string[] files = FileBrowser.OpenFilePanel(
    "Open",
    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
    new ExtensionFilter("Images", "png", "jpg"),
    multiselect: false
);

string path = FileBrowser.SaveFilePanel("Save", directory, "graph.json", "json");
```

Implementations:

- Windows via `Ookii.Dialogs`;
- Linux helper;
- macOS bundle;
- interface commune `IStandaloneFileBrowser`.

## Nodal system

### Graphe

`FuNodalGraph` contient:

- `Name`;
- nodes;
- edges;
- registry;
- dirty state;
- clipboard;
- execution order.

Methodes:

- `GetNode`;
- `TryConnect`;
- `DeleteEdge`;
- `ComputeGraphIfDirty`;
- `TryGetExecutionOrder`;
- `CopyNodes`;
- `PasteNodes`;
- `DeleteNode`;
- `AddNode`;
- `SetRegistry`.

### Registry

```csharp
graph.Registry.RegisterType(FuNodalType.Create<float>(
    "core/float",
    0f,
    v => v.ToString(),
    s => float.Parse(s),
    color: Color.cyan
));

graph.Registry.RegisterNode("Math/Add", () => new AddNode());
```

### Node

```csharp
public sealed class FloatNode : FuNode
{
    public override string Title => "Float";
    public override float Width => 96f;

    public override void CreateDefaultPorts()
    {
        AddPort(new FuNodalPort
        {
            Name = "Out",
            Direction = FuNodalPortDirection.Out,
            Multiplicity = FuNodalMultiplicity.Many,
            DataType = "core/float",
            AllowedTypes = new HashSet<string> { "core/float" },
            Data = 1f
        });
    }

    public override void OnDraw(FuLayout layout)
    {
        float value = GetPortValue<float>("Out");
        if (layout.Drag("##value", ref value))
        {
            SetPortValue("Out", "core/float", value);
        }
    }

    public override void Compute() {}
    public override void SetDefaultValues(FuNodalPort port) {}
}
```

### Editor

```csharp
FuNodalGraph graph = new FuNodalGraph { Name = "Graph" };
FuNodalEditor editor = new FuNodalEditor(graph, minZoom: 0.5f, maxZoom: 2f);

editor.RegisterCustomContextMenu(builder =>
{
    builder.AddSeparator()
        .AddItem("Save JSON", Save)
        .AddItem("Load JSON", Load);
});

editor.Draw(window);
editor.DrawMiniMap(new Vector2(128, 128));
```

### Serialization

```csharp
string json = graph.ToJson();
graph.FromJson(json);
```

`FuPortValueSerializer` utilise le registry de types pour serialiser/deserialiser les donnees des ports.

## Tree

`FuTree<T>` dessine une hierarchie avec selection, open/close, clipping et callbacks.

Pattern:

```csharp
FuTree<MyItem> tree = new FuTree<MyItem>(
    "tree",
    getAllItems,
    FuTextStyle.Default,
    drawItem,
    getItemSize,
    onOpen,
    onClose,
    onSelect,
    onDeselect,
    getLevel,
    equals,
    getChildren,
    isOpen,
    isSelected,
    itemHeight
);

tree.UpdateTree(rootItems);
tree.DrawTree();
```

Operations:

- `TryOpen`;
- `TryClose`;
- `UpdateTree`;
- `Select`;
- `Deselect`;
- `SelectAll`;
- `DeselectAll`;
- `DrawTree`.

## Inputs et raycasting

Classes:

- `FuMouseState`;
- `FuKeyboardState`;
- `FuKeyState`;
- `FuButtonState`;
- `InputState`;
- `FuRaycasting`;
- `FuRaycaster`;

Raycasting 3D:

- utilise `FuSettings.UILayer`;
- distance max `UIRaycastDistance`;
- met a jour les etats souris/clavier pour les containers 3D;
- `FuCameraWindow.GetCameraRay()` convertit la position souris en rayon camera.

## Mobile

Sur Android/iOS:

- `FUMOBILE` est defini par compilation conditionnelle;
- `Fugui.BeginMobileFrame()` et `Fugui.EndMobileFrame()` encapsulent la frame mobile;
- `FuTouchScroll` remplace certains `BeginChild`/`EndChild` pour scroll tactile;
- les seuils de click/drag utilisent les valeurs mobiles de `FuSettings`.

## Externalisation

Activee uniquement avec `FU_EXTERNALIZATION`.

Composants impliques:

- `FuExternalWindow`;
- `FuExternalContext`;
- `FuExternalWindowContainer`;
- `SDLPlatform`;
- `SDLEventRooter`;
- wrappers SDL2;
- plugins SDL/OpenTK.

Workflow:

```csharp
window.Externalize();
window.Internalize();
```

`FuExternalWindow` gere:

- creation native;
- rendu;
- resize/minimize/maximize;
- drag;
- copie de textures Unity vers contexte externe;
- menu/context natif selon flags;
- boutons custom si pas de barre native.

## Fonts et icones

`FontConfig`:

- `DefaultSize`;
- `FontSizeConfig[] Fonts`;
- `FontsFolder`.

`SubFontConfig` et `FontSizeConfig` decrivent les fontes par taille/type.

`FuIcons` expose les glyphes Fugui de base. Le sample `Samples/Demo/Scripts/Icons.cs` etend les icones.

Ranges:

- non duotone: `57344` a `60542`;
- duotone: `60543` a `63743`.

## Cursors

`CursorShapesAsset` mappe les `ImGuiMouseCursor` vers des textures Unity:

- Arrow;
- TextInput;
- ResizeAll;
- ResizeNS;
- ResizeEW;
- ResizeNESW;
- ResizeNWSE;
- Hand;
- NotAllowed.

`FuSettings.TargetCursorSize` controle la taille cible.

## Ressources runtime

`Runtime/Resources`:

- `FuguiController.prefab`;
- `FontConfig.asset`;
- images de status: info, success, warning, error;
- directions: left, right, top, bottom, center;
- `NonReadableTexture.png`;
- cursors default/macOS;
- shaders.

`StreamingAssets/Fugui`:

- fonts runtime;
- layouts;
- themes.

## Exemples complets

### Fenetre de settings avec grid

```csharp
using Fu;
using Fu.Framework;
using UnityEngine;

public sealed class ToolWindow : FuWindowBehaviour
{
    private bool _enabled = true;
    private float _radius = 5f;
    private Vector4 _color = Color.cyan;

    public override void OnUI(FuWindow window, FuLayout layout)
    {
        using (new FuPanel("tool-panel", FuStyle.Unpadded))
        using (var grid = new FuGrid("tool-grid", FuGridDefinition.DefaultFixed, FuGridFlag.LinesBackground))
        {
            grid.SetNextElementToolTipWithLabel("Active l'outil");
            grid.Toggle("Enabled", ref _enabled);

            if (!_enabled)
            {
                grid.DisableNextElements();
            }

            grid.Slider("Radius", ref _radius, 0.1f, 50f);
            grid.ColorPicker("Color", ref _color);

            if (!_enabled)
            {
                grid.EnableNextElements();
            }
        }
    }
}
```

### Definition de fenetre avec overlay

```csharp
FuWindowName Stats = new FuWindowName(200, "Stats", true, 10);

FuWindowDefinition definition = new FuWindowDefinition(
    Stats,
    (window, layout) =>
    {
        layout.Text("Main stats window");
    },
    size: new Vector2Int(360, 240)
);

FuOverlay overlay = new FuOverlay(
    "stats-overlay",
    new Vector2Int(120, 52),
    (o, layout) =>
    {
        layout.Text("Stats");
        layout.Text("Time " + Fugui.Time.ToString("0.00"));
    }
);

overlay.AnchorWindowDefinition(definition, FuOverlayAnchorLocation.TopRight);
Fugui.CreateWindow(Stats);
```

### Sauvegarder un layout courant

```csharp
string folder = System.IO.Path.Combine(
    Application.streamingAssetsPath,
    Fugui.Settings.LayoutsFolder
);

FuDockingLayoutDefinition layout = Fugui.Layouts.GenerateCurrentLayout();

if (layout != null)
{
    layout.Name = "RuntimeLayout";
    Fugui.Layouts.SaveLayoutFile(folder, layout);
}
```

### Drag/drop typable

```csharp
public sealed class AssetPayload
{
    public string Path;
}

if (layout.Button("Drag asset"))
{
    // element source dessine avant BeginDragDropSource
}

Fugui.BeginDragDropSource(
    "asset-payload",
    ImGuiDragDropFlags.None,
    () => layout.Text("Dragging asset"),
    new AssetPayload { Path = "Assets/My.asset" }
);

layout.FramedText("Drop here");
Fugui.BeginDragDropTarget<AssetPayload>("asset-payload", payload =>
{
    Debug.Log(payload.Path);
});
```

### Graph nodal minimal

```csharp
FuNodalGraph graph = new FuNodalGraph { Name = "Runtime Graph" };
graph.Registry.RegisterType(FuNodalType.Create<float>(
    "core/float",
    0f,
    v => v.ToString(System.Globalization.CultureInfo.InvariantCulture),
    s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture),
    color: Color.green
));

graph.Registry.RegisterNode("Variables/Float", () => new FloatNode(Color.green));

FuNodalEditor editor = new FuNodalEditor(graph, 0.5f, 2f, FuNodalEditorFlags.Default);

public override void OnUI(FuWindow window, FuLayout layout)
{
    editor.Draw(window);
}
```

## Checklist d'integration

- `FuController` est present dans la scene.
- `FuSettings` est assigne.
- `UI Camera` est assignee.
- La camera UI est sur le layer attendu par `FuguiRenderFeature`.
- `FuguiRenderFeature` est dans le renderer URP.
- Le shader Fugui URP est assigne.
- `FontConfig.asset` pointe vers les fonts dans `StreamingAssets`.
- `StreamingAssets/Fugui/Themes/themes_index.json` existe.
- `StreamingAssets/Fugui/Layouts/layouts_index.json` existe si les layouts sont utilises.
- Les fenetres ont des IDs `FuWindowName` uniques.
- Les `FuGrid`, `FuPanel` et styles sont utilises avec `using`.
- `FU_EXTERNALIZATION` est ajoute seulement si les fenetres natives sont voulues.

## Notes de maintenance

- Le workspace actuel contient beaucoup de modifications non commitees hors documentation; cette doc ne les annule pas.
- Le dossier sample declare dans `Package.json` est `Samples~/Demo`, tandis que le dossier present ici est `Samples/Demo`.
- Plusieurs fichiers ImGui/SDL sont des bindings generes ou wrappers natifs; ils doivent etre traites comme dependances bas niveau.
- Le renderer cible explicitement URP RenderGraph Unity 6, avec fallback `Execute` obsolete pour versions avant `UNITY_6000_4_OR_NEWER`.
