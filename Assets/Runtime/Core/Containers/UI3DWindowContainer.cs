using Fugui.Framework;
using ImGuiNET;
using System;
using UnityEngine;

namespace Fugui.Core
{
    public class UI3DWindowContainer : IUIWindowContainer
    {
        public string ID { get; private set; }
        public UIWindow Window { get; private set; }
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
        private UnityContext _fuguiContext;
        private static int _3DContextindex = 0;
        private Material _uiMaterial;

        public UI3DWindowContainer(UIWindow window)
        {
            _3DContextindex++;
            ID = "3DContext_" + _3DContextindex;

            _localMousePos = new Vector2Int(-1, -1);
            _scale = FuGui.Settings.Windows3DSuperSampling;

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
            _uiMaterial = GameObject.Instantiate(FuGui.Settings.UIMaterial);
            _uiMaterial.SetTexture("_MainTex", RenderTexture);

            // create panel game object
            createPanel();

            // create the fugui 3d context
            _fuguiContext = FuGui.CreateUnityContext(Camera, FuGui.Settings.Windows3DSuperSampling, FuGui.Settings.Windows3DFontScale);
            _fuguiContext.OnRender += _context_OnRender;
            _fuguiContext.OnPrepareFrame += context_OnPrepareFrame;
            _fuguiContext.AutoUpdateMouse = false;

            // apply the theme to this context
            ThemeManager.SetTheme(ThemeManager.CurrentTheme);

            // set default position
            SetPosition(new Vector3(0f, 0f, 0f));
            SetRotation(Quaternion.Euler(Vector3.up * 180f));

            // release window
            window.IsBusy = false;
        }

        private void createPanel()
        {
            if (_panelGameObject != null)
            {
                GameObject.Destroy(_panelGameObject);
            }

            _panelGameObject = new GameObject(ID + "_Panel");
            _panelGameObject.transform.SetParent(Camera.transform);
            RoundedRectangleMesh rectangleMesh = _panelGameObject.AddComponent<RoundedRectangleMesh>();
            float round = ThemeManager.CurrentTheme.WindowRounding;
            MeshCollider collider = _panelGameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = rectangleMesh.CreateMesh(Window.Size.x / _scale, Window.Size.y / _scale, 1f / 1000f * FuGui.Settings.Windows3DScale, round, round, round, round, FuGui.Settings.UIPanelWidth, 32, _uiMaterial, FuGui.Settings.UIPanelMaterial);
            int layer = (int)Mathf.Log(FuGui.Settings.UILayer.value, 2);
            _panelGameObject.layer = layer;
            foreach (Transform child in Camera.transform)
            {
                child.gameObject.layer = layer;
            }
        }

        public void SetPosition(Vector3 position)
        {
            Camera.transform.position = position;
        }

        public void SetRotation(Quaternion rotation)
        {
            Camera.transform.rotation = rotation;
        }

        private bool context_OnPrepareFrame()
        {
            if (Window == null)
            {
                return false;
            }

            // get input state for this container
            InputState inputState = InputManager.GetInputState(ID, _panelGameObject);

            // force to draw if hover in
            if (inputState.Hovered && !Window.IsHovered)
            {
                Window.ForceDraw();
            }

            Vector2 scaledMousePosition = inputState.MousePosition * (1000f / FuGui.Settings.Windows3DScale) * Scale;
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

        private void _context_OnRender()
        {
            RenderUIWindows();
        }

        public bool ForcePos()
        {
            return true;
        }

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

        public void RenderUIWindow(UIWindow UIWindow)
        {
            // call UIWindow.DrawWindow
            UIWindow.DrawWindow();
            UIWindow.UpdateState(_fuguiContext.IO.MouseDown[0]);
        }

        public void RenderUIWindows()
        {
            if (Window != null)
            {
                // draw the window
                RenderUIWindow(Window);
            }
        }

        public bool TryAddWindow(UIWindow UIWindow)
        {
            if (Window == null)
            {
                Window = UIWindow;
                Window.OnClosed += Window_OnClosed;
                Window.OnResized += Window_OnResized;
                Window.LocalPosition = Vector2Int.zero;
                Window.Container = this;
                Window.LocalPosition = Vector2Int.zero;
                return true;
            }
            return false;
        }

        private void Window_OnResized(UIWindow window)
        {
            _size = window.Size;
            RenderTexture.Release();
            RenderTexture.width = _size.x;
            RenderTexture.height = _size.y;
            Camera.targetTexture = RenderTexture;
            Camera.pixelRect = new Rect(Vector2.zero, new Vector2(_size.x, _size.y));
            createPanel();
        }

        private void Window_OnClosed(UIWindow window)
        {
            Close();
        }

        public bool TryRemoveWindow(string id)
        {
            if (Window != null && Window.ID == id)
            {
                Close();
                return true;
            }
            return false;
        }

        public void Close()
        {
            if (Window != null)
            {
                Window.Fire_OnRemovedFromContainer();
                Window.OnClosed -= Window_OnClosed;
                Window.OnResized -= Window_OnResized;
                Window.Container = null;
            }
            if (_fuguiContext != null)
            {
                _fuguiContext.OnRender -= _context_OnRender;
                _fuguiContext.OnPrepareFrame -= context_OnPrepareFrame;
                FuGui.DestroyContext(_fuguiContext);
            }
            if (Camera != null)
            {
                UnityEngine.Object.Destroy(Camera.gameObject);
            }
        }
    }
}