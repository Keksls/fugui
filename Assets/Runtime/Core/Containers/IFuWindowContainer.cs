using System;
using UnityEngine;

namespace Fu.Core
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
        public float Scale { get; }

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

        /// <summary>
        /// get texture ID for current graphic context
        /// </summary>
        /// <param name="texture">texture to get id</param>
        /// <returns>graphic ID of the texture</returns>
        public IntPtr GetTextureID(Texture2D texture);

        /// <summary>
        /// get texture ID for current graphic context
        /// </summary>
        /// <param name="texture">texture to get id</param>
        /// <returns>graphic ID of the texture</returns>
        public IntPtr GetTextureID(RenderTexture texture);

        /// <summary>
        /// Draw ImGui Image regardless to GL context
        /// </summary>
        /// <param name="texture">renderTexture to draw</param>
        /// <param name="size">size of the image</param>
        public void ImGuiImage(RenderTexture texture, Vector2 size);

        /// <summary>
        /// Draw ImGui Image regardless to GL context
        /// </summary>
        /// <param name="texture">texture2D to draw</param>
        /// <param name="size">size of the image</param>
        public void ImGuiImage(Texture2D texture, Vector2 size);

        /// <summary>
        /// Draw ImGui Image regardless to GL context
        /// </summary>
        /// <param name="texture">renderTexture to draw</param>
        /// <param name="size">size of the image</param>
        public void ImGuiImage(RenderTexture texture, Vector2 size, Vector4 color);

        /// <summary>
        /// Draw ImGui Image regardless to GL context
        /// </summary>
        /// <param name="texture">texture2D to draw</param>
        /// <param name="size">size of the image</param>
        public void ImGuiImage(Texture2D texture, Vector2 size, Vector4 color);

        /// <summary>
        /// Draw ImGui ImageButton regardless to GL context
        /// </summary>
        /// <param name="texture">texture2D to draw</param>
        /// <param name="size">size of the image</param>
        /// <returns>true if clicked</returns>
        public bool ImGuiImageButton(Texture2D texture, Vector2 size);

        /// <summary>
        /// Draw ImGui ImageButton regardless to GL context
        /// </summary>
        /// <param name="texture">texture2D to draw</param>
        /// <param name="size">size of the image</param>
        /// <param name="color">tint additive color of the image</param>
        /// <returns>true if clicked</returns>
        public bool ImGuiImageButton(Texture2D texture, Vector2 size, Vector4 color);
    }
}