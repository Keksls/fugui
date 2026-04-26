using ImGuiNET;
using System;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// A class that represent a 3D UI Container
    /// </summary>
    public class Fu3DWindowContainer : IFuWindowContainer
    {
        #region Variables
        public string ID { get; private set; }
        public FuWindow Window { get; private set; }
        public FuContext Context => _fuguiContext;
        public bool IsClosed { get; private set; }
        public Vector2Int LocalMousePos => _localMousePos;
        public Vector2Int Position => Vector2Int.zero;
        public Vector2Int Size => _size;
        public RenderTexture RenderTexture { get; private set; }
        private GameObject _panelGameObject;
        public int FuguiContextID { get { return _fuguiContext != null ? _fuguiContext.ID : -1; } }
        public FuMouseState Mouse => _mouseState;
        public FuKeyboardState Keyboard => _keyboardState;
        private FuMouseState _mouseState;
        private FuKeyboardState _keyboardState;
        private Vector2Int _localMousePos;
        private Vector2Int _size;
        private FuUnityContext _fuguiContext;
        private static int _3DContextindex = 0;
        private Material _uiMaterial;
        #endregion

        /// <summary>
        /// Instantiate a new 3D Container
        /// </summary>
        /// <param name="window">Window to add to this container</param>
        /// <param name="position">world 3D position of this container</param>
        /// <param name="rotation">world 3D rotation of this container</param>
        public Fu3DWindowContainer(FuWindow window, Vector3? position = null, Quaternion? rotation = null)
        {
            _3DContextindex++;
            ID = "3DContext_" + _3DContextindex;

            _localMousePos = new Vector2Int(-1, -1);

            // remove the window from it's old container if has one
            window.TryRemoveFromContainer();
            // add the window to this container
            if (!TryAddWindow(window))
            {
                Debug.Log("Fail to create 3D container.");
                Close();
                return;
            }

            // resize the window
            Window.Size = new Vector2Int(512, 512);
            Window.Is3DWindow = true;
            _size = window.Size;

            // Create RenderTexture
            RenderTexture = createRenderTexture(_size);

            if (!RenderTexture.IsCreated())
            {
                Debug.LogError("RenderTexture failed to create.");
                return;
            }

            // create ui material
            _uiMaterial = GameObject.Instantiate(Fugui.Settings.UIMaterial);
            _uiMaterial.SetTexture("_MainTex", RenderTexture);

            // create the fugui 3d context
            Rect rect = new Rect(Vector2.zero, new Vector2(_size.x, _size.y));
            _fuguiContext = Fugui.CreateUnityContext(rect, Fugui.Settings.Windows3DSuperSampling, Fugui.Settings.Windows3DFontScale);
            _fuguiContext.OnRender += RenderFuWindows;
            _fuguiContext.OnPrepareFrame += context_OnPrepareFrame;
            _fuguiContext.OnFramePrepared += _fuguiContext_OnFramePrepared;
            _fuguiContext.AutoUpdateMouse = false;
            _fuguiContext.SetTargetTexture(RenderTexture);

            // create panel game object
            createPanel();

            // instantiate inputs states
            _mouseState = new FuMouseState();
            _keyboardState = new FuKeyboardState(_fuguiContext.IO);

            // apply the theme to this context
            Fugui.Themes.SetTheme(Fugui.Themes.CurrentTheme);

            // register on theme change
            Fugui.Themes.OnThemeSet += ThemeManager_OnThemeSet;

            // set default position
            SetPosition(position.HasValue ? position.Value : Vector3.zero);
            SetRotation(rotation.HasValue ? rotation.Value : Quaternion.identity);

            // initialize the window
            window.InitializeOnContainer();
        }

        /// <summary>
        /// Whenever a theme is set
        /// </summary>
        /// <param name="theme">setted theme</param>
        private void ThemeManager_OnThemeSet(FuTheme theme)
        {
            createPanel();
        }

        /// <summary>
        /// Create the render texture used by the 3D UI panel.
        /// </summary>
        /// <param name="size">Pixel size of the render target.</param>
        /// <returns>The created render texture.</returns>
        private RenderTexture createRenderTexture(Vector2Int size)
        {
            size = sanitizeSize(size);

            RenderTexture renderTexture = new RenderTexture(size.x, size.y, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB);
            int aaSamples = QualitySettings.antiAliasing;
            if (aaSamples <= 0)
                aaSamples = 1;

            renderTexture.antiAliasing = aaSamples;
            renderTexture.useDynamicScale = false;
            renderTexture.Create();

            return renderTexture;
        }

        /// <summary>
        /// Clamp render target sizes to values Unity and ImGui can use.
        /// </summary>
        /// <param name="size">Requested size.</param>
        /// <returns>Sanitized size.</returns>
        private Vector2Int sanitizeSize(Vector2Int size)
        {
            return new Vector2Int(
                Mathf.Max(1, size.x),
                Mathf.Max(1, size.y)
            );
        }

        /// <summary>
        /// Apply a new render size to the render texture, Fugui context, material and panel mesh.
        /// </summary>
        /// <param name="size">Target render size in pixels.</param>
        private void setRenderSize(Vector2Int size)
        {
            size = sanitizeSize(size);

            bool sizeChanged = _size != size;
            bool textureInvalid = RenderTexture == null ||
                                  RenderTexture.width != size.x ||
                                  RenderTexture.height != size.y ||
                                  !RenderTexture.IsCreated();

            _size = size;

            if (textureInvalid)
            {
                RenderTexture oldTexture = RenderTexture;
                RenderTexture = createRenderTexture(_size);
                _uiMaterial?.SetTexture("_MainTex", RenderTexture);

                if (_fuguiContext != null)
                {
                    _fuguiContext.SetTargetTexture(RenderTexture);
                }

                if (oldTexture != null)
                {
                    oldTexture.Release();
                    UnityEngine.Object.Destroy(oldTexture);
                }
            }

            if (_fuguiContext != null)
            {
                _fuguiContext.SetPixelRect(new Rect(Vector2.zero, new Vector2(_size.x, _size.y)));
                _fuguiContext.SetTargetTexture(RenderTexture);
            }

            if (sizeChanged || _panelGameObject == null)
            {
                createPanel();
            }
        }

        /// <summary>
        /// Create the 3D UI Panel GameObject of this container
        /// </summary>
        private void createPanel()
        {
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            if (_panelGameObject != null)
            {
                position = _panelGameObject.transform.position;
                rotation = _panelGameObject.transform.rotation;
                GameObject.Destroy(_panelGameObject);
            }

            _panelGameObject = new GameObject(ID + "_Panel");
            FuPanelMesh rectangleMesh = _panelGameObject.AddComponent<FuPanelMesh>();
            float round = Fugui.Themes.WindowRounding * Context.Scale;
            MeshCollider collider = _panelGameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = rectangleMesh.CreateMesh(_size.x / Context.Scale, _size.y / Context.Scale, 1f / 1000f * Fugui.Settings.Windows3DScale, round, round, round, round, Fugui.Settings.UIPanelWidth, 32, _uiMaterial, Fugui.Settings.UIPanelMaterial);
            int layer = (int)Mathf.Log(Fugui.Settings.UILayer.value, 2);
            _panelGameObject.layer = layer;
            foreach (Transform child in _panelGameObject.transform)
            {
                child.gameObject.layer = layer;
            }
            _panelGameObject.transform.position = position;
            _panelGameObject.transform.rotation = rotation;
        }

        /// <summary>
        /// Set the world position of the UI Panel
        /// </summary>
        /// <param name="position">Position of the UI Panel</param>
        public void SetPosition(Vector3 position)
        {
            if (IsClosed || _panelGameObject == null)
                return;

            _panelGameObject.transform.position = position;
        }

        /// <summary>
        /// Set the world rotation of the UI Panel
        /// </summary>
        /// <param name="rotation">Rotation of the UI Panel</param>
        public void SetRotation(Quaternion rotation)
        {
            if (IsClosed || _panelGameObject == null)
                return;

            _panelGameObject.transform.rotation = rotation;
        }

        /// <summary>
        /// Try to prepare the Fugui Context rendering (time to inject inputs)
        /// </summary>
        /// <returns>must return false if the window will not be draw this frame</returns>
        private bool context_OnPrepareFrame()
        {
            if (Window == null)
            {
                return false;
            }

            // get input state for this container
            InputState inputState = FuRaycasting.GetInputState(ID, _panelGameObject);

            // force to draw if hover in
            if (inputState.Hovered)
            {
                Vector2 scaledMousePosition = inputState.MousePosition * (1000f / Fugui.Settings.Windows3DScale) * Context.Scale;
                // calculate IO mouse pos
                _localMousePos = new Vector2Int((int)scaledMousePosition.x, (int)scaledMousePosition.y);
                _localMousePos.x += _size.x / 2;
                _localMousePos.y = Size.y - _localMousePos.y;
            }
            else
            {
                _localMousePos = new Vector2Int(-1, -1);
            }

            // update context mouse position
            _fuguiContext.UpdateMouse(_localMousePos, new Vector2(0f, inputState.MouseWheel), inputState.MouseDown[0], inputState.MouseDown[1], inputState.MouseDown[2]);

            return true;
        }

        private void _fuguiContext_OnFramePrepared()
        {
            // update mouse states
            _mouseState.UpdateState(this);
            _keyboardState.UpdateState();
        }

        /// <summary>
        /// Whatever this container must force the local position of it's windows
        /// </summary>
        /// <returns></returns>
        public bool ForcePos()
        {
            return true;
        }

        /// <summary>
        /// Whatever this container own a window
        /// </summary>
        /// <param name="id">name of the window to check</param>
        /// <returns></returns>
        public bool HasWindow(string id)
        {
            return Window != null && Window.ID == id;
        }

        /// <summary>
        /// Execute a callback on each windows on this container
        /// </summary>
        /// <param name="callback">callback to execute on each windows</param>
        public void OnEachWindow(Action<FuWindow> callback)
        {
            callback?.Invoke(Window);
        }

        /// <summary>
        /// Resize the 3D window so the generated panel mesh matches a target local world size.
        /// </summary>
        /// <param name="localSize">Target local size of the 3D placeholder.</param>
        public void SetLocalSize(Vector2 localSize)
        {
            if (IsClosed || Window == null || Context == null)
                return;

            float inversePanelScale = 1000f / Fugui.Settings.Windows3DScale;

            Vector2Int targetSize = new Vector2Int(
                Mathf.Max(1, Mathf.RoundToInt(localSize.x * Context.Scale * inversePanelScale)),
                Mathf.Max(1, Mathf.RoundToInt(localSize.y * Context.Scale * inversePanelScale))
            );

            if (Window.Size == targetSize &&
                _size == targetSize &&
                RenderTexture != null &&
                RenderTexture.width == targetSize.x &&
                RenderTexture.height == targetSize.y &&
                RenderTexture.IsCreated())
            {
                return;
            }

            if (Window.Size != targetSize)
            {
                Window.Size = targetSize;
            }

            setRenderSize(targetSize);
        }

        #region Image & ImageButton
        public IntPtr GetTextureID(Texture2D texture)
        {
            return _fuguiContext.TextureManager.GetTextureId(texture);
        }

        public IntPtr GetTextureID(RenderTexture texture)
        {
            return _fuguiContext.TextureManager.GetTextureId(texture);
        }

        public void ImGuiImage(RenderTexture texture, Vector2 size)
        {
            if (texture == null)
            {
                ImGui.Dummy(size);
                return;
            }
            ImGui.Image(GetTextureID(texture), size);
        }

        public void ImGuiImage(Texture2D texture, Vector2 size)
        {
            if (texture == null)
            {
                ImGui.Dummy(size);
                return;
            }
            ImGui.Image(GetTextureID(texture), size);
        }

        public void ImGuiImage(RenderTexture texture, Vector2 size, Vector4 color)
        {
            if (texture == null)
            {
                ImGui.Dummy(size);
                return;
            }
            ImGui.Image(GetTextureID(texture), size, Vector2.zero, Vector2.one, color);
        }

        public void ImGuiImage(Texture2D texture, Vector2 size, Vector4 color)
        {
            if (texture == null)
            {
                ImGui.Dummy(size);
                return;
            }
            ImGui.Image(GetTextureID(texture), size, Vector2.zero, Vector2.one, color);
        }

        public bool ImGuiImageButton(Texture2D texture, Vector2 size)
        {
            if (texture == null)
            {
                ImGui.Dummy(size);
                return false;
            }
            // TODO : add ID to image button
            return ImGui.ImageButton("", GetTextureID(texture), size);
        }

        public bool ImGuiImageButton(Texture2D texture, Vector2 size, Vector4 color)
        {
            if (texture == null)
            {
                ImGui.Dummy(size);
                return false;
            }
            // TODO : add ID to image button
            return ImGui.ImageButton("", GetTextureID(texture), size, Vector2.zero, Vector2.one, ImGui.GetStyle().Colors[(int)ImGuiCol.Button], color);
        }
        #endregion

        /// <summary>
        /// Render a window into this container
        /// </summary>
        /// <param name="FuWindow">the window to draw</param>
        public void RenderFuWindow(FuWindow FuWindow)
        {
            // force to place window to local container position zero
            if (FuWindow.LocalPosition.x != 0 || FuWindow.LocalPosition.y != 0)
            {
                FuWindow.LocalPosition = Vector2Int.zero;
            }
            // update the window state (Idle / Manipulating etc)
            FuWindow.UpdateState(_fuguiContext.IO.MouseDown[0]);
            // call UIWindow.DrawWindow
            FuWindow.DrawWindow();
        }

        /// <summary>
        /// Render each Windows of this container
        /// </summary>
        public void RenderFuWindows()
        {
            if (Window != null)
            {
                // draw the window
                RenderFuWindow(Window);

                // draw the context menu
                Fugui.RenderContextMenu();
            }
        }

        /// <summary>
        /// Try to add a window into this container
        /// </summary>
        /// <param name="FuWindow">The window to add</param>
        /// <returns></returns>
        public bool TryAddWindow(FuWindow FuWindow)
        {
            if (IsClosed)
                return false;

            if (Window == null)
            {
                Window = FuWindow;
                Window.OnClosed += Window_OnClosed;
                Window.OnResize += Window_OnResized;
                Window.LocalPosition = Vector2Int.zero;
                Window.Container = this;
                Window.LocalPosition = Vector2Int.zero;
                Window.AddWindowFlag(ImGuiWindowFlags.NoMove);
                Window.AddWindowFlag(ImGuiWindowFlags.NoResize);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Whenever a window is resized
        /// </summary>
        /// <param name="window">the resized window</param>
        private void Window_OnResized(FuWindow window)
        {
            setRenderSize(window.Size);
        }

        /// <summary>
        /// Whenever a window is closed
        /// </summary>
        /// <param name="window">the closed window</param>
        private void Window_OnClosed(FuWindow window)
        {
            Close();
        }

        /// <summary>
        /// Try to remove a window from this container
        /// </summary>
        /// <param name="id">ID of the window to remove</param>
        /// <returns></returns>
        public bool TryRemoveWindow(string id)
        {
            if (Window != null && Window.ID == id)
            {
                Close();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Close this container
        /// </summary>
        public void Close()
        {
            if (IsClosed)
                return;

            IsClosed = true;
            string windowID = Window?.ID;

            if (Window != null)
            {
                Window.OnClosed -= Window_OnClosed;
                Window.OnResize -= Window_OnResized;
                Window.Container = null;
                Window.RemoveWindowFlag(ImGuiWindowFlags.NoMove);
                Window.RemoveWindowFlag(ImGuiWindowFlags.NoResize);
                Window.Is3DWindow = false;
                Window = null;
            }
            if (_fuguiContext != null)
            {
                _fuguiContext.OnRender -= RenderFuWindows;
                _fuguiContext.OnPrepareFrame -= context_OnPrepareFrame;
                _fuguiContext.OnFramePrepared -= _fuguiContext_OnFramePrepared;
                Fugui.DestroyContext(_fuguiContext);
                _fuguiContext = null;
            }
            Fugui.Themes.OnThemeSet -= ThemeManager_OnThemeSet;
            if (_panelGameObject != null)
            {
                UnityEngine.Object.Destroy(_panelGameObject);
                _panelGameObject = null;
            }
            if (RenderTexture != null)
            {
                RenderTexture.Release();
                UnityEngine.Object.Destroy(RenderTexture);
                RenderTexture = null;
            }

            Fugui.Unregister3DWindow(windowID);
        }
    }
}
