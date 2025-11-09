using System;
using System.Runtime.InteropServices;
using ImGuiNET;
using SDL2;
using UnityEngine;

namespace Fu
{
    public unsafe class SDLPlatform : PlatformBase
    {
        private FuExternalWindow _window;
        private bool _initialized;

        public SDLPlatform(FuExternalWindow window)
        {
            _window = window;
        }

        public override bool Initialize(ImGuiIOPtr io, ImGuiPlatformIOPtr platformIO, string platformName = null)
        {
            base.Initialize(io, platformIO, platformName ?? "Fugui SDL Platform");
            _initialized = true;

            io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors | ImGuiBackendFlags.HasSetMousePos;
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

            //platformIO.Platform_CreateWindow = Marshal.GetFunctionPointerForDelegate((Platform_CreateWindowCallback)CreateWindow);
            //platformIO.Platform_DestroyWindow = Marshal.GetFunctionPointerForDelegate((Platform_DestroyWindowCallback)DestroyWindow);
            //platformIO.Platform_ShowWindow = Marshal.GetFunctionPointerForDelegate((Platform_ShowWindowCallback)ShowWindow);
            //platformIO.Platform_SetWindowPos = Marshal.GetFunctionPointerForDelegate((Platform_SetWindowPosCallback)SetWindowPos);
            //platformIO.Platform_SetWindowSize = Marshal.GetFunctionPointerForDelegate((Platform_SetWindowSizeCallback)SetWindowSize);
            //platformIO.Platform_SetWindowFocus = Marshal.GetFunctionPointerForDelegate((Platform_SetWindowFocusCallback)SetWindowFocus);
            //platformIO.Platform_GetWindowPos = Marshal.GetFunctionPointerForDelegate((Platform_GetWindowPosCallback)GetWindowPos);
            //platformIO.Platform_GetWindowSize = Marshal.GetFunctionPointerForDelegate((Platform_GetWindowSizeCallback)GetWindowSize);
            //platformIO.Platform_GetWindowFocus = Marshal.GetFunctionPointerForDelegate((Platform_GetWindowFocusCallback)GetWindowFocus);
            //platformIO.Platform_RenderWindow = Marshal.GetFunctionPointerForDelegate((Platform_RenderWindowCallback)RenderWindow);

            return true;
        }

        #region Platform Callbacks
        private static void CreateWindow(ImGuiViewportPtr vp)
        {
            string title = "Fugui SDL Window";
            int width = (int)vp.Size.x;
            int height = (int)vp.Size.y;

            IntPtr window = SDL.SDL_CreateWindow(
                title,
                SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED,
                width, height,
                SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN
            );

            vp.PlatformHandleRaw = window;
            vp.PlatformUserData = window;
        }

        private static void DestroyWindow(ImGuiViewportPtr vp)
        {
            if (vp.PlatformHandleRaw != IntPtr.Zero)
                SDL.SDL_DestroyWindow(vp.PlatformHandleRaw);
        }

        private static void ShowWindow(ImGuiViewportPtr vp)
        {
            if (vp.PlatformHandleRaw != IntPtr.Zero)
                SDL.SDL_ShowWindow(vp.PlatformHandleRaw);
        }

        private static void SetWindowPos(ImGuiViewportPtr vp, Vector2 pos)
        {
            if (vp.PlatformHandleRaw == IntPtr.Zero) return;
            SDL.SDL_SetWindowPosition(vp.PlatformHandleRaw, (int)pos.x, (int)pos.y);
        }

        private static void SetWindowSize(ImGuiViewportPtr vp, Vector2 size)
        {
            if (vp.PlatformHandleRaw == IntPtr.Zero) return;
            SDL.SDL_SetWindowSize(vp.PlatformHandleRaw, (int)size.x, (int)size.y);
        }

        private static void SetWindowFocus(ImGuiViewportPtr vp)
        {
            if (vp.PlatformHandleRaw != IntPtr.Zero)
                SDL.SDL_RaiseWindow(vp.PlatformHandleRaw);
        }

        private static Vector2 GetWindowPos(ImGuiViewportPtr vp)
        {
            int x = 0, y = 0;
            SDL.SDL_GetWindowPosition(vp.PlatformHandleRaw, ref x, ref y);
            return new Vector2(x, y);
        }

        private static Vector2 GetWindowSize(ImGuiViewportPtr vp)
        {
            int w = 0, h = 0;
            SDL.SDL_GetWindowSize(vp.PlatformHandleRaw, ref w, ref h);
            return new Vector2(w, h);
        }

        private static bool GetWindowFocus(ImGuiViewportPtr vp)
        {
            uint flags = SDL.SDL_GetWindowFlags(vp.PlatformHandleRaw);
            return (flags & (uint)SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS) != 0;
        }

        private static void RenderWindow(ImGuiViewportPtr vp, void* renderArg)
        {
            // handled elsewhere
        }
        #endregion

        public override void PrepareFrame(ImGuiIOPtr io, Rect rect, bool updateMouse, bool updateKeyboard)
        {
            if (!_initialized) return;
            base.PrepareFrame(io, rect, updateMouse, updateKeyboard);

            SDL.SDL_Event e;
            while (Fugui.SDLEventRooter.Poll(_window.SdlWindowId, out e))
            {
                switch (e.type)
                {
                    case SDL.SDL_EventType.SDL_KEYDOWN:
                    case SDL.SDL_EventType.SDL_KEYUP:
                        HandleKey(io, e.key);
                        break;

                    case SDL.SDL_EventType.SDL_TEXTINPUT:
                        HandleTextInput(io, e.text);
                        break;

                    case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                        HandleMouseWheel(io, e.wheel);
                        break;

                    case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                        HandleMouseButton(io, e.button);
                        break;

                    case SDL.SDL_EventType.SDL_MOUSEMOTION:
                        HandleMouseMotion(io, e.motion);
                        break;

                    // Unhandled events are pushed back to the rooter
                    default:
                        Fugui.SDLEventRooter.Push(_window.SdlWindowId, ref e);
                        break;
                }
            }
        }

        private void HandleMouseMotion(ImGuiIOPtr io, SDL.SDL_MouseMotionEvent m)
        {
            io.AddMousePosEvent(m.x, m.y);
        }

        private void HandleMouseWheel(ImGuiIOPtr io, SDL.SDL_MouseWheelEvent w)
        {
            float x = w.x;
            float y = w.y;
            Debug.Log($"Mouse Wheel: x={x}, y={y}");
            io.AddMouseWheelEvent(x, y);
        }

        private void HandleMouseButton(ImGuiIOPtr io, SDL.SDL_MouseButtonEvent b)
        {
            int index =
                b.button == SDL.SDL_BUTTON_LEFT ? 0 :
                b.button == SDL.SDL_BUTTON_RIGHT ? 1 :
                b.button == SDL.SDL_BUTTON_MIDDLE ? 2 : -1;

            if (index >= 0)
                io.AddMouseButtonEvent(index, b.state == SDL.SDL_PRESSED);
        }

        private void HandleTextInput(ImGuiIOPtr io, SDL.SDL_TextInputEvent text)
        {
            unsafe
            {
                string s = new string(text.text);

                if (!string.IsNullOrEmpty(s))
                {
                    foreach (char c in s)
                        io.AddInputCharacter(c);
                }
            }
        }

        private void HandleKey(ImGuiIOPtr io, SDL.SDL_KeyboardEvent key)
        {
            bool down = key.state == SDL.SDL_PRESSED;
            ImGuiKey imKey = SDLKeyToImGuiKey(key.keysym.scancode);

            if (imKey != ImGuiKey.None)
                io.AddKeyEvent(imKey, down);

            SDL.SDL_Keymod mods = SDL.SDL_GetModState();

            io.AddKeyEvent(ImGuiKey.ModCtrl, (mods & SDL.SDL_Keymod.KMOD_CTRL) != 0);
            io.AddKeyEvent(ImGuiKey.ModShift, (mods & SDL.SDL_Keymod.KMOD_SHIFT) != 0);
            io.AddKeyEvent(ImGuiKey.ModAlt, (mods & SDL.SDL_Keymod.KMOD_ALT) != 0);
            io.AddKeyEvent(ImGuiKey.ModSuper, (mods & SDL.SDL_Keymod.KMOD_GUI) != 0);
        }

        public override void Shutdown(ImGuiIOPtr io, ImGuiPlatformIOPtr platformIO)
        {
            _initialized = false;
        }

        private static ImGuiKey SDLKeyToImGuiKey(SDL.SDL_Scancode sc)
        {
            switch (sc)
            {
                case SDL.SDL_Scancode.SDL_SCANCODE_A: return ImGuiKey.A;
                case SDL.SDL_Scancode.SDL_SCANCODE_B: return ImGuiKey.B;
                case SDL.SDL_Scancode.SDL_SCANCODE_C: return ImGuiKey.C;
                case SDL.SDL_Scancode.SDL_SCANCODE_D: return ImGuiKey.D;
                case SDL.SDL_Scancode.SDL_SCANCODE_E: return ImGuiKey.E;
                case SDL.SDL_Scancode.SDL_SCANCODE_F: return ImGuiKey.F;
                case SDL.SDL_Scancode.SDL_SCANCODE_G: return ImGuiKey.G;
                case SDL.SDL_Scancode.SDL_SCANCODE_H: return ImGuiKey.H;
                case SDL.SDL_Scancode.SDL_SCANCODE_I: return ImGuiKey.I;
                case SDL.SDL_Scancode.SDL_SCANCODE_J: return ImGuiKey.J;
                case SDL.SDL_Scancode.SDL_SCANCODE_K: return ImGuiKey.K;
                case SDL.SDL_Scancode.SDL_SCANCODE_L: return ImGuiKey.L;
                case SDL.SDL_Scancode.SDL_SCANCODE_M: return ImGuiKey.M;
                case SDL.SDL_Scancode.SDL_SCANCODE_N: return ImGuiKey.N;
                case SDL.SDL_Scancode.SDL_SCANCODE_O: return ImGuiKey.O;
                case SDL.SDL_Scancode.SDL_SCANCODE_P: return ImGuiKey.P;
                case SDL.SDL_Scancode.SDL_SCANCODE_Q: return ImGuiKey.Q;
                case SDL.SDL_Scancode.SDL_SCANCODE_R: return ImGuiKey.R;
                case SDL.SDL_Scancode.SDL_SCANCODE_S: return ImGuiKey.S;
                case SDL.SDL_Scancode.SDL_SCANCODE_T: return ImGuiKey.T;
                case SDL.SDL_Scancode.SDL_SCANCODE_U: return ImGuiKey.U;
                case SDL.SDL_Scancode.SDL_SCANCODE_V: return ImGuiKey.V;
                case SDL.SDL_Scancode.SDL_SCANCODE_W: return ImGuiKey.W;
                case SDL.SDL_Scancode.SDL_SCANCODE_X: return ImGuiKey.X;
                case SDL.SDL_Scancode.SDL_SCANCODE_Y: return ImGuiKey.Y;
                case SDL.SDL_Scancode.SDL_SCANCODE_Z: return ImGuiKey.Z;

                case SDL.SDL_Scancode.SDL_SCANCODE_SPACE: return ImGuiKey.Space;
                case SDL.SDL_Scancode.SDL_SCANCODE_RETURN: return ImGuiKey.Enter;
                case SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE: return ImGuiKey.Escape;
                case SDL.SDL_Scancode.SDL_SCANCODE_BACKSPACE: return ImGuiKey.Backspace;
                case SDL.SDL_Scancode.SDL_SCANCODE_TAB: return ImGuiKey.Tab;
                case SDL.SDL_Scancode.SDL_SCANCODE_LEFT: return ImGuiKey.LeftArrow;
                case SDL.SDL_Scancode.SDL_SCANCODE_RIGHT: return ImGuiKey.RightArrow;
                case SDL.SDL_Scancode.SDL_SCANCODE_UP: return ImGuiKey.UpArrow;
                case SDL.SDL_Scancode.SDL_SCANCODE_DOWN: return ImGuiKey.DownArrow;

                case SDL.SDL_Scancode.SDL_SCANCODE_LCTRL: return ImGuiKey.LeftCtrl;
                case SDL.SDL_Scancode.SDL_SCANCODE_RCTRL: return ImGuiKey.RightCtrl;
                case SDL.SDL_Scancode.SDL_SCANCODE_LSHIFT: return ImGuiKey.LeftShift;
                case SDL.SDL_Scancode.SDL_SCANCODE_RSHIFT: return ImGuiKey.RightShift;
                case SDL.SDL_Scancode.SDL_SCANCODE_LALT: return ImGuiKey.LeftAlt;
                case SDL.SDL_Scancode.SDL_SCANCODE_RALT: return ImGuiKey.RightAlt;

                case SDL.SDL_Scancode.SDL_SCANCODE_0: return ImGuiKey._0;
                case SDL.SDL_Scancode.SDL_SCANCODE_1: return ImGuiKey._1;
                case SDL.SDL_Scancode.SDL_SCANCODE_2: return ImGuiKey._2;
                case SDL.SDL_Scancode.SDL_SCANCODE_3: return ImGuiKey._3;
                case SDL.SDL_Scancode.SDL_SCANCODE_4: return ImGuiKey._4;
                case SDL.SDL_Scancode.SDL_SCANCODE_5: return ImGuiKey._5;
                case SDL.SDL_Scancode.SDL_SCANCODE_6: return ImGuiKey._6;
                case SDL.SDL_Scancode.SDL_SCANCODE_7: return ImGuiKey._7;
                case SDL.SDL_Scancode.SDL_SCANCODE_8: return ImGuiKey._8;
                case SDL.SDL_Scancode.SDL_SCANCODE_9: return ImGuiKey._9;

                case SDL.SDL_Scancode.SDL_SCANCODE_DELETE: return ImGuiKey.Delete;

                case SDL.SDL_Scancode.SDL_SCANCODE_HOME: return ImGuiKey.Home;
                case SDL.SDL_Scancode.SDL_SCANCODE_END: return ImGuiKey.End;
                case SDL.SDL_Scancode.SDL_SCANCODE_PAGEUP: return ImGuiKey.PageUp;
                case SDL.SDL_Scancode.SDL_SCANCODE_PAGEDOWN: return ImGuiKey.PageDown;

                case SDL.SDL_Scancode.SDL_SCANCODE_INSERT: return ImGuiKey.Insert;
                case SDL.SDL_Scancode.SDL_SCANCODE_MENU: return ImGuiKey.Menu;

                default: return ImGuiKey.None;
            }
        }
    }

    #region ImGui Platform Callbacks
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void Platform_CreateWindowCallback(ImGuiViewportPtr vp);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void Platform_DestroyWindowCallback(ImGuiViewportPtr vp);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void Platform_ShowWindowCallback(ImGuiViewportPtr vp);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void Platform_SetWindowPosCallback(ImGuiViewportPtr vp, Vector2 pos);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void Platform_SetWindowSizeCallback(ImGuiViewportPtr vp, Vector2 size);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void Platform_SetWindowFocusCallback(ImGuiViewportPtr vp);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate Vector2 Platform_GetWindowPosCallback(ImGuiViewportPtr vp);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate Vector2 Platform_GetWindowSizeCallback(ImGuiViewportPtr vp);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool Platform_GetWindowFocusCallback(ImGuiViewportPtr vp);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void Platform_RenderWindowCallback(ImGuiViewportPtr vp, void* renderArg);
    #endregion
}
