# Fugui Unity6

Fugui est un framework d'interface immediate-mode pour Unity, base sur Dear ImGui / ImGui.NET, pense pour construire des outils runtime, des interfaces dockables, des vues camera, des overlays et des panneaux UI 3D avec un cout de rendu controlable.

Le package fournit un runtime complet: contexte ImGui Unity, renderer URP, gestion des fenetres, docking layouts sauvegardables, themes, widgets haut niveau, systeme de notifications/modales, menus contextuels, explorateur de fichiers natif, fenetres externes optionnelles, support mobile et editeur nodal.

## Sommaire

- [Fonctionnalites](#fonctionnalites)
- [Structure du package](#structure-du-package)
- [Installation](#installation)
- [Setup Unity](#setup-unity)
- [Premier ecran Fugui](#premier-ecran-fugui)
- [Fenetre par MonoBehaviour](#fenetre-par-monobehaviour)
- [Fenetre camera](#fenetre-camera)
- [Fenetre UI 3D](#fenetre-ui-3d)
- [Docking layouts](#docking-layouts)
- [Themes, fonts et assets](#themes-fonts-et-assets)
- [Widgets disponibles](#widgets-disponibles)
- [Popups, modales, notifications et menus](#popups-modales-notifications-et-menus)
- [Externalisation de fenetres](#externalisation-de-fenetres)
- [Mobile et inputs](#mobile-et-inputs)
- [Samples](#samples)
- [Depannage](#depannage)
- [Documentation technique](#documentation-technique)

## Fonctionnalites

- UI immediate-mode basee sur Dear ImGui, exposee via `Fu.Fugui`, `Fu.Framework.FuLayout` et `Fu.Framework.FuGrid`.
- Fenetres dockables, closables, multi-instances, externalisables et configurables par `FuWindowDefinition`.
- Gestion de performance par etat de fenetre: idle FPS configurable, redraw force, manipulation a FPS eleve.
- Docking layouts persistants en `.fdl`, indexes JSON et outil Unity Editor de creation/edition (`Tools > Fugui > Editor`, onglets `Layouts` et `Window Names`).
- Themes Fugui en `.fskin`, chargement depuis `StreamingAssets`, theme manager et couleurs extensibles.
- Widgets haut niveau: texte enrichi, boutons, groupes de boutons, checkbox, toggles, sliders, ranges, drags, knobs, combobox, listbox, color picker, gradient, image, tree, tabs, date/time, progress bar, loaders, video player, path fields.
- Layouts ergonomiques: `FuLayout` pour les widgets libres, `FuGrid` pour formulaires et inspectors responsives, `FuPanel` pour zones scrollables.
- Fenetres camera avec render texture, MSAA, supersampling, FPS camera idle/manipulation et raycast depuis la vue camera.
- Panneaux UI 3D en world-space avec resolution fixe ou calculee depuis la taille du panneau, scaling de contexte et resizing runtime.
- Overlays ancrables, deplacables, collapsables, avec points d'ancrage et offsets.
- Modales, notifications, popup messages et popups custom.
- Menus contextuels empilables, sous-menus, images et shortcuts.
- Systeme de drag and drop generique avec payload typable.
- Editeur nodal: graphe, nodes, ports, edges, registry de types, minimap, clipboard, serialization JSON.
- File browser natif via `FileBrowser` et `ExtensionFilter`.
- Support d'inputs par contexte/fenetre: clavier, souris, raycasting 3D, touch/mobile.
- Integration URP via `FuguiRenderFeature`.
- Bindings et plugins embarques: ImGui.NET, ImNodes, ImPlot, ImGuizmo, OpenTK, SDL2, Triangle, SharpFont, Ookii dialogs, Newtonsoft.Json.

## Structure du package

```text
Runtime/
  Fugui.cs                       API globale, lifecycle, contexts, windows, inputs, drag/drop
  FuController.cs                composant d'initialisation et de boucle Update/Render
  Core/
    Containers/                  containers main, 3D, external, scaling
    Contexts/                    contextes ImGui Unity/external, textures, fonts
    Inputs/                      clavier, souris, raycasting, etats d'input
    Mobile/                      scroll/touch mobile
    Rendering/                   render feature URP, draw data, draw lists
    StandaloneFileBrowser/       open/save file/folder panels
    UnityConfiguration/          fonts, cursors, configs ScriptableObject
    Windows/                     FuWindow, FuCameraWindow, FuOverlay, definitions
  Framework/
    Behaviours/                  MonoBehaviours de fenetres et fenetres 3D
    Grid/                        FuGrid, AutoGrid et attributs
    Layouts/                     dockspaces et layout manager
    MainMenu/                    barre de menu globale
    NodalSystem/                 graphe nodal, editor, serialization
    Styles/                      styles predefinis
    Themes/                      theme manager et couleurs
    Widgets/                     widgets de haut niveau
  Resources/                     shaders, images, cursors, prefab controller, FontConfig
StreamingAssets/Fugui/
  Fonts/                         polices runtime et icones
  Layouts/                       layouts `.fdl` et `layouts_index.json`
  Themes/                        themes `.fskin` et `themes_index.json`
Samples/Demo/                    scene demo, fenetres, nodal editor, materiaux
Logo/                            logos Fugui
```

## Installation

### Via Unity Package Manager

1. Ouvre `Window > Package Manager`.
2. Clique sur `+ > Add package from git URL...`.
3. Ajoute l'URL du depot Fugui.
4. Verifie que le package `com.keksls.fugui` apparait dans le projet.

### Via dossier local

1. Copie le dossier du package dans `Packages/` ou `Assets/`.
2. Si le package est dans `Assets/`, garde les dossiers `Runtime`, `StreamingAssets`, `Samples` et `Logo` ensemble.
3. Laisse Unity importer les `.meta`.

### Dependances

Le package declare `com.unity.postprocessing` en dependance. L'assembly `Runtime/Fugui.asmdef` reference aussi plusieurs assemblies precompilees embarquees dans `Runtime/Plugins`.

Le rendu principal est prevu pour URP avec `FuguiRenderFeature`.

## Setup Unity

### Setup automatique

Ouvre `Tools > Fugui > Editor`, puis l'onglet `Setup`, pour diagnostiquer le projet et appliquer les correctifs courants: ajout du prefab `FuguiController`, installation/reparation de `FuguiRenderFeature` sur le renderer URP actif, copie de `StreamingAssets/Fugui`, verification des fonts et conseil sur l'input handling.

### 1. Ajouter le controller

Ajoute le prefab `Runtime/Resources/FuguiController.prefab` dans ta scene, ou cree un GameObject avec `FuController`.

Configure dans l'inspector:

- `Settings`: instance de `FuSettings`.
- `UI Camera`: camera qui rend le container principal Fugui.
- `Update Mode`: `Update`, `LateUpdate` ou `Manual`.
- `Log Errors`: log les exceptions UI via `Fugui.OnUIException`.

Au `Awake`, `FuController` initialise Fugui, cree le contexte Unity principal, le container principal, les managers de themes/layouts, puis appelle `FuguiAwake` sur les MonoBehaviours de la scene.

### 2. Ajouter le renderer URP

Dans ton URP Renderer:

1. Ajoute `FuguiRenderFeature`.
2. Assigne le shader Fugui URP (`Runtime/Resources/Shaders/Fugui_URP_Mesh.shader`).
3. Regle `_cameraLayer` pour correspondre au layer de la camera UI. La valeur par defaut est `5` (layer UI Unity).

La render feature execute les draw lists Fugui apres les transparents, rend le contexte principal sur la camera active et les contextes offscreen dans leurs render textures.

### 3. Configurer les assets runtime

Les assets de runtime attendus sont dans `Runtime/Resources` et `StreamingAssets/Fugui`:

- `FontConfig.asset`: configuration des tailles et polices.
- `StreamingAssets/Fugui/Fonts/current/`: `regular.ttf`, `bold.ttf`, `icons.ttf`.
- `StreamingAssets/Fugui/Themes/`: themes `.fskin`.
- `StreamingAssets/Fugui/Layouts/`: layouts `.fdl`.
- `Runtime/Resources/Cursors/`: packs de cursors.
- `Runtime/Resources/Images/`: icons de notification et fallback texture.

## Premier ecran Fugui

Le chemin recommande est de declarer une definition de fenetre, puis de l'instancier.

```csharp
using Fu;
using Fu.Framework;
using UnityEngine;

public class MyFuguiBootstrap : MonoBehaviour
{
    private static readonly FuWindowName Tools =
        new FuWindowName(100, "Tools", true, -1);

    private void Start()
    {
        new FuWindowDefinition(
            Tools,
            DrawTools,
            size: new Vector2Int(360, 240),
            flags: FuWindowFlags.Default
        );

        Fugui.CreateWindow(Tools);
    }

    private void DrawTools(FuWindow window, FuLayout layout)
    {
        layout.Text("Hello Fugui");

        if (layout.Button("Force redraw"))
        {
            window.ForceDraw();
        }
    }
}
```

## Fenetre par MonoBehaviour

Pour une fenetre maintenable dans Unity, derive de `FuWindowBehaviour`.

```csharp
using Fu;
using Fu.Framework;

public class InventoryWindow : FuWindowBehaviour
{
    private bool _enabled = true;
    private float _weight = 12.5f;

    public override void OnUI(FuWindow window, FuLayout layout)
    {
        using (new FuPanel("inventory-panel", FuStyle.Unpadded))
        using (var grid = new FuGrid("inventory-grid"))
        {
            grid.CheckBox("Enabled", ref _enabled);
            grid.Slider("Weight", ref _weight, 0f, 100f);
            grid.ProgressBar("Capacity", _weight / 100f);
        }
    }
}
```

Dans l'inspector, `FuWindowBehaviourEditor` affiche une popup de `FuWindowName`, les flags, la taille, la position et l'option `Force Create Alone On Awake`.

## Fenetre camera

Derive de `FuCameraWindowBehaviour` pour afficher une camera dans une fenetre Fugui.

```csharp
using Fu;
using Fu.Framework;
using UnityEngine;

public class SceneViewWindow : FuCameraWindowBehaviour
{
    public override void OnWindowCreated(FuWindow window)
    {
        CameraWindow.ForceRenderCamera();
    }

    public override void OnWindowDefinitionCreated(FuWindowDefinition definition)
    {
        definition.SetHeaderUI((win, size) =>
        {
            win.Layout.CenterNextItemH("Scene View");
            win.Layout.Text("Scene View");
        }, 24f);
    }

    public override void OnUI(FuWindow window, FuLayout layout)
    {
        layout.Text("Overlay UI below/above the camera view");
    }
}
```

`FuCameraWindow` gere:

- camera source;
- FPS idle et FPS manipulation;
- supersampling;
- MSAA;
- ray camera via `GetCameraRay()`;
- `ForceRenderCamera()`;
- headers/footers de fenetre.

## Fenetre UI 3D

`Fu3DWindowBehaviour` permet d'attacher une fenetre Fugui a un panneau world-space.

Fonctionnement:

- le transform sert de placeholder;
- `scale.x` et `scale.y` pilotent la taille du panneau;
- `Depth` pilote l'epaisseur;
- `Render Resolution` pilote la resolution texture/contexte;
- le container peut etre resizable runtime;
- le context scale et font scale peuvent etre fixes ou derives d'un scaler.

Creation par API:

```csharp
FuWindow window = Fugui.CreateWindow(MyWindowNames.Tools, autoAddToMainContainer: false);

Fu3DWindowSettings settings = Fu3DWindowSettings.FixedResolutionMatchingPanelAspect(
    panelSize: new Vector2(1.2f, 0.8f),
    referenceResolution: new Vector2Int(1024, 768),
    referencePanelSize: new Vector2(1.2f, 0.8f),
    contextScale: 1f,
    fontScale: 1f
);

Fu3DWindowContainer container = Fugui.Add3DWindow(
    window,
    settings,
    position: new Vector3(0f, 1.5f, 2f),
    rotation: Quaternion.identity
);
```

## Docking layouts

Fugui charge les layouts depuis `Application.streamingAssetsPath + "/" + Fugui.Settings.LayoutsFolder`.

Par defaut:

- `LayoutsFolder = "Fugui/Layouts"`;
- `layouts_index.json` liste les layouts;
- chaque layout est un fichier `.fdl`;
- `Fugui.Layouts` est l'instance de `FuDockingLayoutManager`.

Operations courantes:

```csharp
Fugui.Layouts.SetLayout("FuguiDemoScene");

FuDockingLayoutDefinition generated = Fugui.Layouts.GenerateCurrentLayout();
Fugui.Layouts.SaveLayoutFile(
    System.IO.Path.Combine(Application.streamingAssetsPath, Fugui.Settings.LayoutsFolder),
    generated
);
```

Les fenetres sont associees aux dockspaces par l'ID de leur `FuWindowName`.

## Themes, fonts et assets

`Fugui.Themes` expose `FuThemeManager`.

Fonctions importantes:

- `LoadAllThemes()`;
- `SetTheme(FuTheme theme, bool allContexts = true)`;
- `GetTheme(string themeName, out FuTheme theme)`;
- `SaveTheme(FuTheme theme)`;
- `DeleteTheme(FuTheme theme)`;
- `GetColor(FuColors color)`;
- `GetColorU32(FuColors color)`;
- `ExtendThemes(Enum themesExtension)` pour ajouter des couleurs metier.

Les polices sont configurees avec `FontConfig`. Les glyphes d'icones respectent deux plages:

- icons classiques: `0xE000` a `0xEC7E`;
- icons duotone: `0xEC7F` a `0xF8FF`.

Les atlas de polices peuvent etre bakes depuis `Tools > Fugui > Editor`, onglet `Font Atlas`. Les textures sont stockees dans `Assets/StreamingAssets/Fugui/FontAtlases/<font-hash>/scale_<scale>.png` et le runtime les recharge automatiquement quand `FontConfig.UseBakedFontAtlas` est actif. Si aucun atlas bake ne correspond au `FontScale` courant, Fugui reconstruit l'atlas en memoire comme avant.

Utilisation:

```csharp
Fugui.PushFont(18, FontType.Bold);
layout.Text("Titre");
Fugui.PopFont();

Fugui.Push(FuColors.Text, Color.cyan);
layout.Text("Texte colore");
Fugui.PopColor();
```

## Widgets disponibles

Les widgets sont exposes principalement sur `FuLayout` et herites par `FuGrid`.

Principales familles:

- Texte: `Text`, `SmartText`, `TextURL`, `ClickableText`, `FramedText`, wrapping `None`, `Clip`, `Wrap`.
- Boutons: `Button`, `ImageButton`, `ButtonsGroup`.
- Booleens: `CheckBox`, `Toggle`, `RadioButton`.
- Valeurs numeriques: `Slider`, `Range`, `Drag`, `Knob`.
- Selection: `Combobox`, `ComboboxEnum`, `ListBox`, `Tabs`.
- Couleurs et textures: `ColorPicker`, `Gradient`, `Image`.
- Fichiers: `InputFile`, `InputFolder`.
- Hierarchie: `FuTree<T>`.
- Feedback: `ProgressBar`, loaders/spinners, notifications.
- Video: `FuVideoPlayer`, `GetVideoPlayer`, `DrawImage`, `DrawTimeLine`.
- UI structurelle: `FuPanel`, `FuGrid`, `Collapsable`, `Separator`, `SameLine`, `Dummy`, groups.

Exemple de formulaire:

```csharp
using (var grid = new FuGrid("settings", FuGridDefinition.DefaultFixed, FuGridFlag.LinesBackground))
{
    grid.SetNextElementToolTipWithLabel("Active ou desactive le module");
    grid.Toggle("Enabled", ref enabled);

    grid.SetNextElementToolTip("Valeur entre 0 et 100");
    grid.Slider("Intensity", ref intensity, 0f, 100f);

    grid.ColorPicker("Tint", ref tint);
}
```

## Popups, modales, notifications et menus

### Popup custom

```csharp
if (layout.Button("Options"))
{
    Fugui.OpenPopUp("options-popup", () =>
    {
        layout.Text("Options");
        layout.CheckBox("Enabled", ref enabled);
    }, new Vector2(240f, 120f));
}

Fugui.DrawPopup("options-popup");
```

### Modale

```csharp
Fugui.ShowWarning(
    "Confirmer",
    "Cette action modifie la scene.",
    FuModalSize.Medium,
    new FuModalButton("OK", () => Apply(), FuButtonStyle.Warning),
    new FuModalButton("Cancel")
);
```

### Notification

```csharp
Fugui.Notify("Sauvegarde", "Layout enregistre", StateType.Success, duration: 2f);
```

### Menu contextuel

```csharp
List<FuContextMenuItem> items = FuContextMenuBuilder.Start()
    .AddItem("Duplicate", DuplicateSelection)
    .AddSeparator()
    .BeginChild("Advanced")
    .AddItem("Reset", ResetSelection)
    .EndChild()
    .Build();

Fugui.PushContextMenuItems(items);
layout.Text("Right click me");
Fugui.TryOpenContextMenuOnItemClick();
Fugui.PopContextMenuItems();
```

## Externalisation de fenetres

L'externalisation permet de sortir une fenetre Fugui du container Unity vers une fenetre native separee.

Pour l'activer:

1. Ajoute le scripting define symbol `FU_EXTERNALIZATION`.
2. Verifie que `FuSettings.EnableExternalizations` est actif.
3. Configure les flags `FuExternalWindowFlags` si besoin.

```csharp
FuWindow window = Fugui.CreateWindow(MyWindowNames.Tools);
window.Externalize();
window.Internalize();
```

Sans `FU_EXTERNALIZATION`, les appels affichent un message indiquant d'ajouter le define.

## Mobile et inputs

Sur Android/iOS, le code active `FUMOBILE` et adapte:

- touch scrolling;
- begin/end child avec scrolling mobile;
- seuils mobile (`DragThresholdMobile`, `ClickMaxDistMobile`);
- etat input via `InputSystemPlatform` / `InputManagerPlatform`.

Pour interroger les inputs Fugui:

```csharp
if (Fugui.GetKeyDown(FuKeysCode.Enter))
{
    Debug.Log("Enter dans un contexte Fugui");
}

FuMouseState mouse = Fugui.GetCurrentMouse();
```

## Samples

Le dossier `Samples/Demo` contient:

- `WidgetsWindow`: showcase des widgets, panels, grids, sliders, drags, loaders, text, combobox, listbox.
- `CameraWindow`: fenetre camera, overlays, header/footer, raycast dans la vue camera.
- `InspectorWindow`: inspector runtime pour transform/camera.
- `TreeWindow`: `FuTree<T>` avec selection, suppression et clipping.
- `PopupsWindow`: modales, notifications, menus contextuels, popups.
- `NodalEditorDemo`: graphe nodal, types, nodes variables/math, minimap, save/load JSON.
- `FuWindowsNames`: registre de noms de fenetres de demo.

Le sample package reference `Samples~/Demo` dans `Package.json`; dans ce workspace, le dossier present est `Samples/Demo`.

## Depannage

### Rien ne s'affiche

- Verifie que `FuController` est present et que `UI Camera` est renseignee.
- Verifie que la camera UI est sur le layer attendu par `FuguiRenderFeature._cameraLayer`.
- Verifie que `FuguiRenderFeature` est ajoutee au renderer URP.
- Verifie que le shader Fugui URP est assigne.
- Verifie que la fenetre est bien creee (`Fugui.CreateWindow`) ou auto-instanciee par un layout.

### Erreurs de police ou glyphes manquants

- Verifie `FontConfig.asset`.
- Verifie `StreamingAssets/Fugui/Fonts/current`.
- Pour les icones custom, respecte les ranges de glyphes documentees dans `FontConfig`.

### Layout non charge

- Verifie `StreamingAssets/Fugui/Layouts/layouts_index.json`.
- Verifie que les `.fdl` listes existent.
- Verifie `FuSettings.LayoutsFolder`.

### Theme non charge

- Verifie `StreamingAssets/Fugui/Themes/themes_index.json`.
- Verifie que les `.fskin` listes existent.
- Verifie `FuSettings.ThemesFolder`.

### Externalisation inactive

- Ajoute `FU_EXTERNALIZATION` aux scripting define symbols.
- Verifie les plugins SDL/OpenTK dans `Runtime/Plugins`.
- Verifie `FuSettings.EnableExternalizations`.

## Documentation technique

La documentation technique complete est dans [TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md).
