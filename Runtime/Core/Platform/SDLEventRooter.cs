using SDL2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static UnityEngine.InputManagerEntry;

namespace Fu
{
    /// <summary>
    /// Routes SDL events per-window using SDL_GetWindowID.
    /// Allows ImGui (or any backend) to poll events window-by-window.
    /// </summary>
    public class SDLEventRooter
    {
        /// <summary>
        /// Stores FIFO queues per windowID.
        /// </summary>
        private readonly Dictionary<uint, Queue<SDL.SDL_Event>> _eventsByWindow =
            new Dictionary<uint, Queue<SDL.SDL_Event>>();

        /// <summary>
        /// Stores window IDs that currently exist (optional but useful to purge).
        /// </summary>
        private readonly HashSet<uint> _knownWindows =
            new HashSet<uint>();

        #region Window Registration
        /// <summary>
        /// Register a new SDL window so its event queue is prepared.
        /// </summary>
        public void RegisterWindow(uint id)
        {
            if (!_eventsByWindow.ContainsKey(id))
                _eventsByWindow[id] = new Queue<SDL.SDL_Event>();

            _knownWindows.Add(id);
        }

        /// <summary>
        /// Unregister a destroyed window and clear its events.
        /// </summary>
        public void UnregisterWindow(uint id)
        {
            _knownWindows.Remove(id);
            _eventsByWindow.Remove(id);
        }
        #endregion

        #region Update Polling
        /// <summary>
        /// Polls ALL SDL events and sorts them per window.
        /// Must be called once per frame.
        /// </summary>
        public void Update()
        {
            SDL.SDL_Event ev;

            // Loop over all pending SDL events
            while (SDL.SDL_PollEvent(out ev) == 1)
            {
                uint winId = ExtractWindowID(ref ev);

                // If event has no window (e.g. SDL_QUIT), route to ALL windows OR a special queue.
                if (winId == 0)
                {
                    // Option 1: broadcast QUIT to all windows
                    foreach (uint id in _knownWindows)
                        _eventsByWindow[id].Enqueue(ev);

                    continue;
                }

                // Normal window event
                if (!_eventsByWindow.ContainsKey(winId))
                    _eventsByWindow[winId] = new Queue<SDL.SDL_Event>();

                _eventsByWindow[winId].Enqueue(ev);
            }
        }
        #endregion

        #region Event Extraction Logic
        /// <summary>
        /// Extracts windowID depending on event type.
        /// SDL2-CS exposes windowID on all window-related events.
        /// </summary>
        private static uint ExtractWindowID(ref SDL.SDL_Event e)
        {
            switch (e.type)
            {
                case SDL.SDL_EventType.SDL_WINDOWEVENT:
                    return e.window.windowID;

                case SDL.SDL_EventType.SDL_KEYDOWN:
                case SDL.SDL_EventType.SDL_KEYUP:
                    return e.key.windowID;

                case SDL.SDL_EventType.SDL_TEXTEDITING:
                    return e.edit.windowID;

                case SDL.SDL_EventType.SDL_TEXTINPUT:
                    return e.text.windowID;

                case SDL.SDL_EventType.SDL_MOUSEMOTION:
                    return e.motion.windowID;

                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    return e.button.windowID;

                case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                    return e.wheel.windowID;

                // No window ID (e.g. SDL_QUIT). Caller must decide how to route.
                default:
                    return 0;
            }
        }
        #endregion

        #region Per-window Event Poll
        /// <summary>
        /// Works like SDL_PollEvent, but scoped to a specific window.
        /// Returns true if an event was available.
        /// </summary>
        public bool Poll(uint windowId, out SDL.SDL_Event e)
        {
            e = default;

            if (_eventsByWindow.TryGetValue(windowId, out Queue<SDL.SDL_Event> queue))
            {
                if (queue.Count > 0)
                {
                    e = queue.Dequeue();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Pushes an event back into the queue for a specific window.
        /// </summary>
        /// <param name="windowId"> The window ID to push the event to. </param>
        /// <param name="e"> The event to push back. </param>
        public void Push(uint windowId, ref SDL.SDL_Event e)
        {
            if (_eventsByWindow.TryGetValue(windowId, out Queue<SDL.SDL_Event> queue))
            {
                queue.Enqueue(e);
            }
            else
            {
                _eventsByWindow[windowId] = new Queue<SDL.SDL_Event>();
                _eventsByWindow[windowId].Enqueue(e);
            }
        }
        #endregion
    }
}