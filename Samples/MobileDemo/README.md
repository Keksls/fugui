# Fugui Mobile Demo

This sample adds a touch-first Fugui window that behaves like a small smart home mobile app.
The window is implemented as a `FuWindowBehaviour` and the UI is built with Fugui layouts and widgets.

## What it demonstrates

- `FuWindowBehaviour` lifecycle with header/footer window chrome.
- Safe-area fitting on mobile, and a centered phone preview in the editor.
- Bottom navigation sized for thumb interaction with Fugui tabs.
- A complete app flow: dashboard, rooms/devices, activity timeline and automations.
- Scrollable Fugui panels using the mobile touch-scroll path.
- Fugui controls: tabs, collapsables, framed text, charts, progress bars, toggles, sliders, search, modals and notifications.

## Setup

1. Add `Runtime/Resources/FuguiController.prefab` to a scene, or keep the existing Fugui demo setup.
2. Add `MobileDemoBootstrap` to any GameObject in the scene.
3. Enter Play Mode. The `Mobile Demo` window is created automatically.

The behaviour can optionally configure the main Fugui container scale for a 430x760 mobile reference. Disable `Configure Mobile Scale` if the scene already owns its scaling policy.
