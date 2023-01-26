using Fugui.Framework;
using ImGuiNET;
using OpenTK.Input;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Fugui.Core
{
    public class UI3DWindowContainer : IUIWindowContainer
    {
        public string ID { get; private set; }
        public UIWindow Window { get; private set; }
        public Vector2Int LocalMousePos => _localMousePos;
        public Vector2Int Position => Vector2Int.zero;
        public Vector2Int Size => _size;
        public RectTransform ImageTransform { get; private set; }
        public RenderTexture RenderTexture { get; private set; }
        public Camera Camera { get; private set; }
        public BoxCollider Collider { get; private set; }
        public int FuguiContextID { get { return _fuguiContext.ID; } }
        private Vector2Int _localMousePos;
        private Vector2Int _size;
        private UnityContext _fuguiContext;
        private static int _3DContextindex = 0;

        public UI3DWindowContainer(UIWindow window)
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

            // Get UI Layer Mask
            int layerMask = LayerMask.NameToLayer(FuGui.Settings.UILayer);

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

            // Create Canvas GameObject
            GameObject canvasGameObject = new GameObject(ID + "_Canvas");
            canvasGameObject.transform.SetParent(cameraGameObject.transform);
            canvasGameObject.transform.position = Vector3.zero;
            canvasGameObject.transform.rotation = Quaternion.identity;
            Canvas canvas = canvasGameObject.AddComponent<Canvas>();

            // Create RawImage GameObject
            GameObject imageGameObject = new GameObject(ID + "_Image");
            imageGameObject.transform.SetParent(canvasGameObject.transform);
            imageGameObject.transform.position = Vector3.zero;
            imageGameObject.transform.rotation = Quaternion.identity;
            RawImage image = imageGameObject.AddComponent<RawImage>();
            ImageTransform = imageGameObject.GetComponent<RectTransform>();
            ImageTransform.sizeDelta = Window.Size;
            Collider = imageGameObject.AddComponent<BoxCollider>();
            Collider.size = new Vector3(Window.Size.x, Window.Size.y, 0.1f);
            imageGameObject.layer = layerMask;

            // apply image scale
            ImageTransform.transform.localScale = Vector3.one * (1f / 1000f) * FuGui.Settings.Windows3DScale;

            // Assignate RenderTexture to RawImage
            image.texture = RenderTexture;

            // create the fugui 3d context
            _fuguiContext = FuGui.CreateUnityContext(Camera);
            _fuguiContext.OnRender += _context_OnRender;
            _fuguiContext.OnPrepareFrame += context_OnPrepareFrame;

            // apply the theme to this context
            ThemeManager.SetTheme(ThemeManager.CurrentTheme);

            // set default position
            SetPosition(new Vector3(0f, 1f, 0f));

            // resize the window
            Window.Size = new Vector2Int(512, 256);

            // release window
            window.IsBusy = false;
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
            InputState inputState = InputManager.GetInputState(ID, ImageTransform.gameObject);
            // Get UI IO
            ImGuiIOPtr io = ImGui.GetIO();

            // send mouse buttons state to UI IO
            io.MouseDown[0] = inputState.MouseDown[0];
            io.MouseDown[1] = inputState.MouseDown[1];
            io.MouseDown[2] = inputState.MouseDown[2];

            _localMousePos = new Vector2Int((int)inputState.MousePosition.x, (int)inputState.MousePosition.y);
            _localMousePos.x += _size.x / 2;
            _localMousePos.y = Size.y - (_localMousePos.y + (Size.y / 2));

            // set UI IO mouse pos
            io.MousePos = _localMousePos;
            // set UI IO mouse scroll wheel
            io.MouseWheel = inputState.MouseWheel;
            io.MouseWheelH = 0f;

            if (inputState.Hovered)
            {
                Debug.Log(_localMousePos);
            }

            // force to draw if hover in
            if (inputState.Hovered && !Window.IsHovered)
            {
                Window.ForceDraw();
            }

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
            // force set UI pos on appear (don't need to any frame because ForcePos() return true, it's checked into UIWindow src)
            ImGui.SetNextWindowPos(Vector2.zero, ImGuiCond.Always);
            // call UIWindow.DrawWindow
            UIWindow.DrawWindow();
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
                Window.Container = this;
                Window.OnClosed += Window_OnClosed;
                Window.OnResized += Window_OnResized;
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
            // resize image size
            ImageTransform.sizeDelta = new Vector2(_size.x, _size.y);
            // apply image scale
            ImageTransform.transform.localScale = Vector3.one * (1f / 1000f) * FuGui.Settings.Windows3DScale;
            // resize collider
            Collider.size = new Vector3(Window.Size.x, Window.Size.y, 1f);
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
                FuGui.DestroyContext(_fuguiContext);
            }
            if (Camera != null)
            {
                UnityEngine.Object.Destroy(Camera.gameObject);
            }
        }
    }
}