using ImGuiNET;
using SDL2;
using System.Collections.Generic;
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

            return true;
        }

        Queue<SDL.SDL_Event> forwardingEvts = new Queue<SDL.SDL_Event>();
        public override void PrepareFrame(ImGuiIOPtr io, Rect rect, bool updateMouse, bool updateKeyboard)
        {
            if (!_initialized) return;
            base.PrepareFrame(io, rect, updateMouse, updateKeyboard);

            // Update window events
            _window.Update();

            forwardingEvts.Clear();
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
                        HandleMouseButton(io, e.button, true);
                        break;

                    case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                        HandleMouseButton(io, e.button, false);
                        break;

                    case SDL.SDL_EventType.SDL_MOUSEMOTION:
                        HandleMouseMotion(io, e.motion);
                        break;

                    // Unhandled events are pushed back to the rooter
                    default:
                        forwardingEvts.Enqueue(e);
                        break;
                }
            }

            // Push unhandled events back to the rooter
            while (forwardingEvts.Count > 0)
            {
                SDL.SDL_Event evt = forwardingEvts.Dequeue();
                Fugui.SDLEventRooter.Push(_window.SdlWindowId, ref evt);
            }

            // If mouse is outside the window, reset mouse position and buttons
            //if (!_window.IsMouseHover && FuWindow.InputFocusedWindow != _window.Window)
            //{
            //    _mouseX = -float.MaxValue;
            //    _mouseY = -float.MaxValue;
            //    _mouseDown[0] = false;
            //    _mouseDown[1] = false;
            //    _mouseDown[2] = false;
            //}
            //else
            //{
            uint state = 0;
            int x = 0, y = 0;
            if (_window.IsDragging && !_window.CanInternalize)
                state = SDL.SDL_GetGlobalMouseState(out x, out y);
            else
                state = SDL.SDL_GetMouseState(out x, out y);
            _mouseX = x;
            _mouseY = y;
            if (!_window.IsMouseHover && FuWindow.InputFocusedWindow != _window.Window)
            {
                _mouseX = -float.MaxValue;
                _mouseY = -float.MaxValue;
            }
            _mouseDown[0] = (state & SDL.SDL_BUTTON(SDL.SDL_BUTTON_LEFT)) != 0;
            _mouseDown[1] = (state & SDL.SDL_BUTTON(SDL.SDL_BUTTON_MIDDLE)) != 0;
            _mouseDown[2] = (state & SDL.SDL_BUTTON(SDL.SDL_BUTTON_RIGHT)) != 0;
            //}

            // Feed continuous mouse buttons state 
            io.AddMouseButtonEvent(0, _mouseDown[0]);
            io.AddMouseButtonEvent(1, _mouseDown[2]);
            io.AddMouseButtonEvent(2, _mouseDown[1]);

            // Feed continuous mouse position
            io.AddMousePosEvent(_mouseX, _mouseY);
        }

        private float _mouseX;
        private float _mouseY;
        private void HandleMouseMotion(ImGuiIOPtr io, SDL.SDL_MouseMotionEvent m)
        {
            _mouseX = m.x;
            _mouseY = m.y;
        }

        private void HandleMouseWheel(ImGuiIOPtr io, SDL.SDL_MouseWheelEvent w)
        {
            float x = w.x;
            float y = w.y;
            io.AddMouseWheelEvent(x, y);
        }

        // Store mouse button states (0 = left, 1 = right, 2 = middle)
        private readonly bool[] _mouseDown = new bool[3];
        private void HandleMouseButton(ImGuiIOPtr io, SDL.SDL_MouseButtonEvent b, bool isDown)
        {
            if (b.button < 1 || b.button > 3)
                return;
            _mouseDown[b.button - 1] = isDown;
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
}