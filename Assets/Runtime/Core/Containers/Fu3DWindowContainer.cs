using Fu.Framework;
using ImGuiNET;
using System;
using UnityEngine;

namespace Fu.Core
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
        public Vector2Int LocalMousePos => _localMousePos;
        public Vector2Int Position => Vector2Int.zero;
        public Vector2Int Size => _size;
        public RenderTexture RenderTexture { get; private set; }
        public Camera Camera { get; private set; }
        private GameObject _panelGameObject;
        public int FuguiContextID { get { return _fuguiContext.ID; } }
        public float Scale => _scale;
        private float _scale;
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
            _scale = Fugui.Settings.Windows3DSuperSampling;

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

            // Create Camera GameObject
            GameObject cameraGameObject = new GameObject(ID + "_Camera");
            cameraGameObject.transform.position = Vector3.zero;
            cameraGameObject.transform.rotation = Quaternion.identity;
            Camera = cameraGameObject.AddComponent<Camera>();
            Camera.pixelRect = new Rect(0f, 0f, Window.Size.x, Window.Size.y);
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = Color.black;
            Camera.cullingMask = 0;

            // Create RenderTexture
            RenderTexture = new RenderTexture(Camera.pixelWidth, Camera.pixelHeight, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB);
            RenderTexture.antiAliasing = 8;
            RenderTexture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;
            RenderTexture.useDynamicScale = true;
            RenderTexture.Create();

            // Assignate RenderTexture to Camera
            Camera.targetTexture = RenderTexture;

            // create ui material
            _uiMaterial = GameObject.Instantiate(Fugui.Settings.UIMaterial);
            _uiMaterial.SetTexture("_MainTex", RenderTexture);

            // create panel game object
            createPanel();

            // create the fugui 3d context
            _fuguiContext = Fugui.CreateUnityContext(Camera, Fugui.Settings.Windows3DSuperSampling, Fugui.Settings.Windows3DFontScale);
            _fuguiContext.OnRender += _context_OnRender;
            _fuguiContext.OnPrepareFrame += context_OnPrepareFrame;
            _fuguiContext.AutoUpdateMouse = false;

            // apply the theme to this context
            FuThemeManager.SetTheme(FuThemeManager.CurrentTheme);

            // register on theme change
            FuThemeManager.OnThemeSet += ThemeManager_OnThemeSet;

            // set default position
            SetPosition(position.HasValue ? position.Value : Vector3.zero);
            SetRotation(rotation.HasValue ? rotation.Value : Quaternion.identity);

            // release window
            window.IsBusy = false;
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
        /// Create the 3D UI Panel GameObject of this container
        /// </summary>
        private void createPanel()
        {
            if (_panelGameObject != null)
            {
                GameObject.Destroy(_panelGameObject);
            }

            _panelGameObject = new GameObject(ID + "_Panel");
            _panelGameObject.transform.SetParent(Camera.transform);
            FuPanelMesh rectangleMesh = _panelGameObject.AddComponent<FuPanelMesh>();
            float round = FuThemeManager.CurrentTheme.WindowRounding * _scale;
            MeshCollider collider = _panelGameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = rectangleMesh.CreateMesh(Window.Size.x / _scale, Window.Size.y / _scale, 1f / 1000f * Fugui.Settings.Windows3DScale, round, round, round, round, Fugui.Settings.UIPanelWidth, 32, _uiMaterial, Fugui.Settings.UIPanelMaterial);
            int layer = (int)Mathf.Log(Fugui.Settings.UILayer.value, 2);
            _panelGameObject.layer = layer;
            foreach (Transform child in Camera.transform)
            {
                child.gameObject.layer = layer;
            }
        }

        /// <summary>
        /// Set the world position of the UI Panel
        /// </summary>
        /// <param name="position">Position of the UI Panel</param>
        public void SetPosition(Vector3 position)
        {
            Camera.transform.position = position;
        }

        /// <summary>
        /// Set the world rotation of the UI Panel
        /// </summary>
        /// <param name="rotation">Rotation of the UI Panel</param>
        public void SetRotation(Quaternion rotation)
        {
            Camera.transform.rotation = rotation;
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
            if (inputState.Hovered && !Window.IsHovered)
            {
                Window.ForceDraw();
            }

            Vector2 scaledMousePosition = inputState.MousePosition * (1000f / Fugui.Settings.Windows3DScale) * Scale;
            // calculate IO mouse pos
            _localMousePos = new Vector2Int((int)scaledMousePosition.x, (int)scaledMousePosition.y);
            if (inputState.Hovered)
            {
                _localMousePos.x += _size.x / 2;
                _localMousePos.y = Size.y - _localMousePos.y;
            }

            // update context mouse position
            _fuguiContext.UpdateMouse(_localMousePos, new Vector2(0f, inputState.MouseWheel), inputState.MouseDown[0], inputState.MouseDown[1], inputState.MouseDown[2]);

            // return whatever the mouse need to be drawn
            return Window.MustBeDraw();
        }

        /// <summary>
        /// whenever the fugui context render the frame
        /// </summary>
        private void _context_OnRender()
        {
            RenderFuWindows();
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
            ImGui.Image(GetTextureID(texture), size);
        }

        public void ImGuiImage(Texture2D texture, Vector2 size)
        {
            ImGui.Image(GetTextureID(texture), size);
        }

        public void ImGuiImage(RenderTexture texture, Vector2 size, Vector4 color)
        {
            ImGui.Image(GetTextureID(texture), size, Vector2.zero, Vector2.one, color);
        }

        public void ImGuiImage(Texture2D texture, Vector2 size, Vector4 color)
        {
            ImGui.Image(GetTextureID(texture), size, Vector2.zero, Vector2.one, color);
        }

        public bool ImGuiImageButton(Texture2D texture, Vector2 size)
        {
            // TODO : add ID to image button
            return ImGui.ImageButton("", GetTextureID(texture), size);
        }

        public bool ImGuiImageButton(Texture2D texture, Vector2 size, Vector4 color)
        {
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
            // call UIWindow.DrawWindow
            FuWindow.DrawWindow();
            // update the window state (Idle / Manipulating etc)
            FuWindow.UpdateState(_fuguiContext.IO.MouseDown[0]);
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
            }
        }

        /// <summary>
        /// Try to add a window into this container
        /// </summary>
        /// <param name="FuWindow">The window to add</param>
        /// <returns></returns>
        public bool TryAddWindow(FuWindow FuWindow)
        {
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
            _size = window.Size;
            RenderTexture.Release();
            RenderTexture.width = _size.x;
            RenderTexture.height = _size.y;
            Camera.targetTexture = RenderTexture;
            Camera.pixelRect = new Rect(Vector2.zero, new Vector2(_size.x, _size.y));
            createPanel();
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
            if (Window != null)
            {
                Window.OnClosed -= Window_OnClosed;
                Window.OnResized -= Window_OnResized;
                Window.Container = null;
                Window.Fire_OnRemovedFromContainer();
                Window.RemoveWindowFlag(ImGuiWindowFlags.NoMove);
                Window.RemoveWindowFlag(ImGuiWindowFlags.NoResize);
            }
            if (_fuguiContext != null)
            {
                _fuguiContext.OnRender -= _context_OnRender;
                _fuguiContext.OnPrepareFrame -= context_OnPrepareFrame;
                Fugui.DestroyContext(_fuguiContext);
            }
            FuThemeManager.OnThemeSet -= ThemeManager_OnThemeSet;
            if (Camera != null)
            {
                Camera.targetTexture.Release();
                Camera.targetTexture = null;
                UnityEngine.Object.Destroy(Camera.gameObject);
            }
            UnityEngine.Object.Destroy(RenderTexture);
        }
    }
}