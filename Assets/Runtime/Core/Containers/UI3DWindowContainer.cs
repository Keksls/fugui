using Fugui.Framework;
using ImGuiNET;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Fugui.Core
{
    public class UI3DWindowContainer : IUIWindowContainer
    {
        public UIWindow Window { get; private set; }
        public Vector2Int LocalMousePos => _localMousePos;
        public Vector2Int Position => Vector2Int.zero;
        public Vector2Int Size => _size;
        public RectTransform ImageTransform { get; private set; }
        public RenderTexture RenderTexture { get; private set; }
        public Camera Camera { get; private set; }
        private Vector2Int _localMousePos;
        private Vector2Int _size;
        private UnityContext _fuguiContext;
        private static int _3DContextindex = 0;

        public UI3DWindowContainer(UIWindow window)
        {
            // remove the window from it's old container if has one
            window.TryRemoveFromContainer();
            // add the window to this container
            if (!TryAddWindow(window))
            {
                Debug.Log("Fail to create 3D container.");
                Close();
                return;
            }

            _3DContextindex++;

            // Create Camera GameObject
            GameObject cameraGameObject = new GameObject("3DContext_" + _3DContextindex + "_Camera");
            cameraGameObject.transform.position = Vector3.zero;
            cameraGameObject.transform.rotation = Quaternion.identity;
            Camera = cameraGameObject.AddComponent<Camera>();
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
            GameObject canvasGameObject = new GameObject("3DContext_" + _3DContextindex + "_Canvas");
            canvasGameObject.transform.SetParent(cameraGameObject.transform);
            canvasGameObject.transform.position = Vector3.zero;
            canvasGameObject.transform.rotation = Quaternion.identity;
            Canvas canvas = canvasGameObject.AddComponent<Canvas>();

            // Create RawImage GameObject
            GameObject imageGameObject = new GameObject("3DComtext_" + _3DContextindex + "_Image");
            imageGameObject.transform.SetParent(canvasGameObject.transform);
            imageGameObject.transform.position = Vector3.zero;
            imageGameObject.transform.rotation = Quaternion.identity;
            RawImage image = imageGameObject.AddComponent<RawImage>();
            ImageTransform = imageGameObject.GetComponent<RectTransform>();
            ImageTransform.sizeDelta = Window.Size;

            // apply image scale
            ImageTransform.transform.localScale = Vector3.one * (1f / 1000f) * FuGui.Settings.Windows3DScale;

            // Assignate RenderTexture to RawImage
            image.texture = RenderTexture;

            // create the fugui 3d context
            _fuguiContext = FuGui.CreateUnityContext(Camera);
            _fuguiContext.OnRender += _fuguiContext_OnRender;

            // apply the theme to this context
            ThemeManager.SetTheme(ThemeManager.CurrentTheme);

            // release window
            window.IsBusy = false;
        }

        private void _fuguiContext_OnRender()
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
                // handle Inputs
                injectImguiInputs();
                // draw the window
                RenderUIWindow(Window);
            }
        }

        private void injectImguiInputs()
        {

        }

        public bool TryAddWindow(UIWindow UIWindow)
        {
            if (Window == null)
            {
                Window = UIWindow;
                Window.Container = this;
                Window.OnClosed += Window_OnClosed;
                Window.OnResized += Window_OnResized;
                return true;
            }
            return false;
        }

        private void Window_OnResized(UIWindow window)
        {
            _size = window.Size;
            //RenderTexture.Release();
            //RenderTexture.width = _size.x;
            //RenderTexture.height = _size.y;
            //Camera.targetTexture = RenderTexture;
            //Camera.pixelRect = new Rect(Vector2.zero, new Vector2(_size.x, _size.y));
            // resize image size
            ImageTransform.sizeDelta = new Vector2(_size.x, _size.y);
            // apply image scale
            ImageTransform.transform.localScale = Vector3.one * (1f / 1000f) * FuGui.Settings.Windows3DScale;
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