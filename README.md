# FuGui: The Ultimate Unity UI Framework
Imediate GUI Framework for Unity based on https://github.com/ocornut/imgui.
FuGui is here to revolutionize the way you build UI for your Unity projects! Based on DearImgui, FuGui offers a range of powerful features to make UI development fast and easy.
Follow the avancement here : https://trello.com/b/2zY2a0y2/fugui

## Key Features
- Target FPS handling for smooth and optimal UI window performance
- Display of cameras within UI windows for improved idle performance
- native (almost free-cost) camera supersampling rendering
- Overlay support for adding extra visual elements to your camera windows
- Save/restore functionality for custom docking layouts
- Display UI windows outside of the Unity build window using OpenTK
- Attractive and customizable themes
- Window definition support for instantiating multiple instances of the same window
- Responsive and unified layout components

## Installation
Installing FuGui into your project is a breeze thanks to the unity package manager. Simply add this git to the unity package manager into your project and you're ready to start building your UI.

## Target FPS Handling
Ensure your UI windows are running smoothly with FuGui's target FPS handling feature. Each UI window will be draw at low FPS (fully customizable throw configuration), ensuring we do not slow your project. FuGui will raise UI whenever it's needed so it feel realy smooth.

## Camera Display
FuGui makes it easy to display cameras within your UI windows, realy improving the project's performances. Each camera can have a diferent target FPS according to FuGui camera windows' state. Whenever it's needed, the camera render slowly, to keep huge projects realy smooth even if a lot of objects are loaded into 3D context, and when it's requiered, Figui will incrase camera FPS so it feel smooth.

## Overlay Support
Add extra visual elements to your camera windows with FuGui's overlay support. Overlays can be dragged, collapsed, snapped into a virtual grid and fully customisable realy easily.

## External Window Support
FuGui supports the display of UI windows outside of the Unity build window using OpenTK. Any window (including camera view) can be dragged outside of Unity render context and keep performances. Externnal render run on separate thread to keep maximum performances.

## Attractive Themes
FuGui comes with a range of attractive and customizable themes to give your UI a professional look and feel.

## Window Definitions
FuGui makes it easy to instantiate multiple instances of the same UI window with its window definition support. Simply define your window and let FuGui handle the rest!

We hope you have as much fun using FuGui as we had building it. For more information about how to use FuGui, see Wiki on this git. If you have any questions or encounter any issues, don't hesitate to reach out for support. Happy coding!
