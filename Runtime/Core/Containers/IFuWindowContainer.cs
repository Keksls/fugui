using System;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Interface that represent what should implement an UI window container
    /// A container is a piece of code that can host UI windows
    /// - Unity main Window Container : should be unique instance
    /// - OpenTK window Container : can be multiples, used for external windows
    /// </summary>
    public interface IFuWindowContainer
    {
        public Vector2Int LocalMousePos { get; }
        public FuContext Context { get; }
        public Vector2Int Position { get; }
        public Vector2Int Size { get; }
        public FuKeyboardState Keyboard { get; }
        public FuMouseState Mouse { get; }

        /// <summary>
        /// Execute a callback on each windows on this container
        /// </summary>
        /// <param name="callback">callback to execute on each windows</param>
        public void OnEachWindow(Action<FuWindow> callback);

        /// <summary>
        /// Try to add an UI window to this container
        /// </summary>
        /// <param name="UIWindow">the window object to add</param>
        /// <returns>true if success</returns>
        public bool TryAddWindow(FuWindow UIWindow);

        /// <summary>
        /// Try to remove an UI window from this container
        /// </summary>
        /// <param name="id">id of the window object to add</param>
        /// <returns>true if success</returns>
        public bool TryRemoveWindow(string id);

        /// <summary>
        /// Whatever this container contains a window
        /// </summary>
        /// <param name="id">ID of the window to check</param>
        /// <returns>true if contains</returns>
        public bool HasWindow(string id);

        /// <summary>
        /// Method that render every UI windows hosted by this container
        /// Must call RenderUIWindo(UIWindow UIWindow) for each hoster windows
        /// </summary>
        public void RenderFuWindows();

        /// <summary>
        /// Methos that render a single UIWindow object
        /// </summary>
        /// <param name="UIWindow">UIWindow object to render</param>
        public void RenderFuWindow(FuWindow UIWindow);

        /// <summary>
        /// Did the container must force UI window position to it self context ?
        /// Unity main WUIWindowContainer must return false, because UI window can move into it
        /// OpenTK container must return false, because the graphic context move instead of the UI window itself
        /// </summary>
        /// <returns>true or false according to the container</returns>
        public bool ForcePos();
    }
}