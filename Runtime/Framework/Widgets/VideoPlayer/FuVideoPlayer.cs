using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if HAS_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif
using UnityEngine.Video;

namespace Fu.Framework
{
    public class FuVideoPlayer
    {
        #region Variables
        // public properties
        public string ID { get; private set; }
        public string CurrentFilePath => Player?.url ?? string.Empty;
        public RenderTexture Texture { get; private set; }
        public ulong FrameCount => Player?.frameCount ?? 1;
        public bool IsPlaying => Player?.isPlaying ?? false;
        public bool IsPaused => Player?.isPaused ?? false;
        public long CurrentFrame
        {
            get { return Player?.frame ?? 0; }
            set { if (Player != null) Player.frame = value; }
        }
        public bool IsBuffering { get; private set; }
        public Dictionary<long, string> TimeStamps => _timeStamps;
        // private variables
        public VideoPlayer Player { get; private set; }
        private GameObject _playerGameObject;
        private long _lastFramePlayed = 0;
        private Dictionary<long, string> _timeStamps;
        private bool _isFullScreen = false;
        private bool _autoPlay = true;
        private Vector2 _lastFrameMousePos;
        private float _lastFrameMousePosTime = 0f;
        private System.Action _executeOncePrepared;
        private bool _isPreparing = false;
        #endregion

        #region Initialization
        /// <summary>
        /// Create a new instance a FuVideoPlayer with the given ID
        /// Must be call only y fugui. If you need an instance of FuVideoPlayer, please call layout.GetVideoPlayer() instead
        /// </summary>
        /// <param name="id">UniqueID of the FuVideoPlayer</param>
        internal FuVideoPlayer(string id)
        {
            ID = id;
            Fugui.ExecuteInMainThread(() =>
            {
                _playerGameObject = new GameObject("FuVideoPlayer");
                Player = _playerGameObject.AddComponent<VideoPlayer>();
                Player.playOnAwake = false;
                Player.prepareCompleted += _player_prepareCompleted;
            });
        }

        /// <summary>
        /// Whenever the video player is prepared with a given media
        /// </summary>
        /// <param name="source">videoPlayer just prepared</param>
        private void _player_prepareCompleted(VideoPlayer source)
        {
            // player is ready, let's create texture
            Texture = new RenderTexture((int)Player.width, (int)Player.height, 24, RenderTextureFormat.RGB111110Float, 0);

            // get the first FuCameraWindowDefinition to get the current SRP MSAA sample count
            FuCameraWindowDefinition camDef = Fugui.UIWindowsDefinitions
                .FirstOrDefault(wd => wd.Value is FuCameraWindowDefinition).Value as FuCameraWindowDefinition;

            if (camDef != null)
            {
                Texture.antiAliasing = (int)camDef.MSAASamples;
            }
            else
            {
                Texture.antiAliasing = 0; // by default no MSAA to avoid flickering issues and fail on Metal
            }

#if HAS_URP
    // if render graph is enabled, we need a depth buffer to avoid issues
    bool isRenderGraphEnabled = !GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode;
    if (isRenderGraphEnabled)
        Texture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D16_UNorm;
    else
        Texture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;
#endif

#if HAS_HDRP
            // HDRP always expects a valid depth buffer; safer to force a 32-bit depth-stencil
            Texture.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D32_SFloat_S8_UInt;
#endif

            Texture.useDynamicScale = true;
            Texture.Create();

            Player.targetTexture = Texture;

            _executeOncePrepared?.Invoke();
            _executeOncePrepared = null;

            if (_autoPlay)
            {
                Play();
            }

            _isPreparing = false;
        }

        #endregion

        /// <summary>
        /// Execute a callback once the video player is prepared
        /// </summary>
        /// <param name="callback">callback to execute once the video plaoyer is prepared</param>
        public void ExecuteOncePrepared(System.Action callback)
        {
            _executeOncePrepared = callback;
        }

        /// <summary>
        /// Set a file to play in the video player
        /// </summary>
        /// <param name="path">path of the file to play (mp4, avi, etc...)
        /// See https://docs.unity3d.com/Manual/VideoSources-FileCompatibility.html to get compatible formats</param>
        public void SetFile(string path)
        {
            if (_isPreparing)
            {
                return;
            }

            Fugui.ExecuteInMainThread(() =>
            {
                // release texture
                if (Texture != null)
                {
                    Texture.Release();
                    Texture = null;
                }
                // set new file to read and wait for player end prepare
                Player.renderMode = VideoRenderMode.RenderTexture;
                Player.url = path;
                Player.Prepare();
            });
        }

        /// <summary>
        /// Set a video clip to play in the video player
        /// </summary>
        /// <param name="clip">video clip to play (mp4, avi, etc...)</param>
        /// <param name="looping">whatever the player will loop playing video</param>
        public void SetVideoClip(VideoClip clip, bool looping)
        {
            if (_isPreparing)
            {
                return;
            }

            Fugui.ExecuteInMainThread(() =>
            {
                _isPreparing = true;
                // release texture
                if (Texture != null)
                {
                    Texture.Release();
                    Texture = null;
                }
                // set new file to read and wait for player end prepare
                Player.renderMode = VideoRenderMode.RenderTexture;
                Player.clip = clip;
                Player.isLooping = looping;
                Player.Prepare();
            });
        }

        /// <summary>
        /// Set whatever the player will loop playing video
        /// </summary>
        /// <param name="looping">whatever the player will loop playing video</param>
        public void SetLoop(bool looping)
        {
            if (Player == null || !Player.isPrepared)
            {
                return;
            }
            Fugui.ExecuteInMainThread(() =>
            {
                Player.isLooping = looping;
            });
        }

        /// <summary>
        /// Set whatever the player will auto play video on clip or file set
        /// </summary>
        /// <param name="autoPlay">whatever the player will auto play video on clip or file set</param>
        public void SetAutoPlay(bool autoPlay)
        {
            if (Player == null || !Player.isPrepared)
            {
                return;
            }
            Fugui.ExecuteInMainThread(() =>
            {
                _autoPlay = autoPlay;
            });
        }

        /// <summary>
        /// Play the loaded video
        /// </summary>
        public void Play()
        {
            if (Player == null || !Player.isPrepared)
            {
                return;
            }
            Fugui.ExecuteInMainThread(() =>
            {
                Player.Play();
            });
        }

        /// <summary>
        /// Pause the loaded video
        /// </summary>
        public void Pause()
        {
            if (Player == null || !Player.isPrepared)
            {
                return;
            }
            Fugui.ExecuteInMainThread(() =>
            {
                Player.Pause();
            });
        }

        /// <summary>
        /// Stop the current playing video
        /// </summary>
        public void Stop()
        {
            if (Player == null || !Player.isPrepared)
            {
                return;
            }
            Fugui.ExecuteInMainThread(() =>
            {
                Player.Stop();
            });
        }

        /// <summary>
        /// Set the current playing frame (will trigger buffer)
        /// </summary>
        /// <param name="frame">Frame index to set</param>
        public void SetFrame(long frame)
        {
            if (Player == null || !Player.isPrepared)
            {
                return;
            }
            Fugui.ExecuteInMainThread(() =>
            {
                Player.frame = frame;
                IsBuffering = true;
            });
        }

        /// <summary>
        /// Set video player timestamps
        /// </summary>
        /// <param name="timeStamps">timestamps of  the video</param>
        public void SetTimeStamp(Dictionary<long, string> timeStamps)
        {
            _timeStamps = timeStamps;
        }

        /// <summary>
        /// [INTERNAL] Will kill and release all resources created for this video player
        /// </summary>
        internal void Kill()
        {
            Fugui.ExecuteInMainThread(() =>
            {
                Player.Stop();
                Texture.Release();
                Object.Destroy(Texture);
                Object.Destroy(Player);
            });
        }

        /// <summary>
        /// Draw the video image
        /// </summary>
        /// <param name="enableInterractions">Whatever you want to enable manipulations, such as click to play/pause, shortcut to play/fullscreen</param>
        /// <returns>the rect of  the drawed image</returns>
        public Rect DrawImage(bool enableInterractions = true)
        {
            return DrawImage(ImGui.GetContentRegionAvail().x, enableInterractions);
        }

        /// <summary>
        /// Draw the video image
        /// </summary>
        /// <param name="width">width of the video image</param>
        /// <param name="enableInterractions">Whatever you want to enable manipulations, such as click to play/pause, shortcut to play/fullscreen</param>
        /// <returns>the rect of  the drawed image</returns>
        public Rect DrawImage(float width, bool enableInterractions = true)
        {
            Vector2 size = GetImageSize(width);
            return DrawImage(size.x, size.y, enableInterractions);
        }

        /// <summary>
        /// Get the size of the image
        /// </summary>
        /// <param name="width">width of the image</param>
        /// <returns>the size of the image</returns>
        public Vector2 GetImageSize(float width)
        {
            float height = width * ((Texture && Texture.width > 0 && Texture.height > 0) ? ((float)Texture.height / (float)Texture.width) : (9f / 16f));
            return new Vector2(width, height);
        }

        /// <summary>
        /// Draw the video image
        /// </summary>
        /// <param name="width">width of the image to draw</param>
        /// <param name="height">height of the video to play</param>
        /// <param name="enableInterractions">Whatever you want to enable manipulations, such as click to play/pause, shortcut to play/fullscreen</param>
        /// <returns>the rect of  the drawed image</returns>
        public Rect DrawImage(float width, float height, bool enableInterractions = true)
        {
            if (!_isFullScreen)
            {
                return _drawImage(width, height, enableInterractions);
            }

            Vector2 size = new Vector2(width, height);
            Vector2 min = ImGui.GetCursorScreenPos();
            Vector2 max = min + size;
            ImGui.GetWindowDrawList().AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.FrameBg));
            if (FuLayout.CurrentDrawerPath.Count > 0)
            {
                FuLayout.CurrentDrawerPath.Peek().EnboxedText("Video player ready\n\nPlease import file to play", min, size, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), FuTextWrapping.None);
            }
            ImGui.Dummy(size);
            return new Rect(ImGui.GetCursorScreenPos(), size);
        }

        /// <summary>
        /// Set player full screen mode
        /// </summary>
        /// <param name="fullScreen">true for fullscreen, false to exit full screen</param>
        public void SetFullScreen(bool fullScreen)
        {
            Fugui.ExecuteInMainThread(() =>
            {
                if (_isFullScreen == fullScreen)
                {
                    return;
                }
                _isFullScreen = fullScreen;

                // enter full screen
                if (_isFullScreen)
                {
                    (FuWindow.CurrentDrawingWindow != null ? FuWindow.CurrentDrawingWindow.Container : Fugui.MainContainer).Context.OnLastRender += fullScreen_draw_event;
                }
                // exit full screen
                else
                {
                    (FuWindow.CurrentDrawingWindow != null ? FuWindow.CurrentDrawingWindow.Container : Fugui.MainContainer).Context.OnLastRender -= fullScreen_draw_event;
                }
            });
        }

        /// <summary>
        /// fullscreen on post draw event on main container
        /// </summary>
        private void fullScreen_draw_event()
        {
            _drawImage(0f, 0f);
        }

        /// <summary>
        /// Draw the video image
        /// </summary>
        /// <param name="width">width of the image to draw</param>
        /// <param name="height">height of the video to play</param>
        /// <returns>the rect of  the drawed image</returns>
        private Rect _drawImage(float width, float height, bool enableInterractions = true)
        {
            bool startFCState = _isFullScreen;
            Vector2 size;
            Vector2 pos;
            Vector2 padding = new Vector2(4f, 4f) * Fugui.CurrentContext.Scale;

            // detect if mouse has moved the last frame to show / hide timeline
            Vector2 mousePos = ImGui.GetMousePos();
            if (mousePos != _lastFrameMousePos)
            {
                _lastFrameMousePos = mousePos;
                _lastFrameMousePosTime = Time.time;
            }

            // get current container
            IFuWindowContainer currentContainer = FuWindow.CurrentDrawingWindow != null ? FuWindow.CurrentDrawingWindow.Container : Fugui.MainContainer;

            // draw in full screen if video is full screen (draw in a new full screen imgui window)
            if (startFCState)
            {
                size = currentContainer.Size;
                pos = Vector2.zero;
                // set black window bg
                ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0f, 0f, 0f, 1f));
                ImGui.SetNextWindowSize(size, ImGuiCond.Always);
                ImGui.SetNextWindowPos(Vector2.zero, ImGuiCond.Always);
                ImGui.Begin("FuVideoPlayerFullScreen", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoNavFocus);
                ImGui.SetWindowFocus();
                ImGui.SetCursorPos(Vector2.zero);
            }
            else
            {
                size = new Vector2(width, height);
                pos = ImGui.GetCursorScreenPos();
            }
            var drawList = ImGui.GetWindowDrawList();

            // we actualy are playing a video, draw it
            if (Texture != null)
            {
                // get video image size from size var and video ratio
                Vector2 videoSize = new Vector2(Texture.width, Texture.height);
                Vector2 videoRatio = new Vector2(videoSize.x / videoSize.y, videoSize.y / videoSize.x);
                Vector2 sizeRatio = new Vector2(size.x / size.y, size.y / size.x);
                Vector2 videoImageSize = size;
                if (videoRatio.x > sizeRatio.x)
                {
                    videoImageSize = new Vector2(size.x, size.x * videoRatio.y);
                }
                else
                {
                    videoImageSize = new Vector2(size.y * videoRatio.x, size.y);
                }

                // draw a dummy before video image to center video image if fullscreen
                if (startFCState)
                {
                    ImGui.Dummy((size - videoImageSize) / 2f);
                    ImGui.SameLine();
                }

                // draw video image
                currentContainer.ImGuiImage(Texture, videoImageSize);

                // video is playing, check if it's buffering
                if (Player != null && Player.isPlaying)
                {
                    // when the video is playing, check each time that the video image get update based in the video's frame rate
                    if (Player.frameRate == 0 || (Time.frameCount % (int)(Player.frameRate + 1)) == 0)
                    {
                        // if the video time is the same as the previous check, that means it's buffering cuz the video is Playing.
                        if (_lastFramePlayed == Player.frame && Player.clip == null) // buffering
                        {
                            IsBuffering = true;
                        }
                        else // not buffering
                        {
                            IsBuffering = false;
                        }
                        _lastFramePlayed = Player.frame;
                    }
                    // we are buffering, draw a buffering text
                    if (IsBuffering)
                    {
                        pos += padding;
                        string txt = "Buffering...";
                        Vector2 txtSize = ImGui.CalcTextSize(txt);

                        // draw a black background with a border and the text
                        drawList.AddRectFilled(pos, pos + txtSize + padding * 2f, ImGui.GetColorU32(ImGuiCol.FrameBg, 0.66f), 4f * Fugui.CurrentContext.Scale);
                        drawList.AddRect(pos, pos + txtSize + padding * 2f, ImGui.GetColorU32(ImGuiCol.Border), 4f * Fugui.CurrentContext.Scale);
                        drawList.AddText(pos + padding, ImGui.GetColorU32(ImGuiCol.Text), txt);
                    }
                    // force window draw to update the video image
                    FuWindow.CurrentDrawingWindow?.ForceDraw();
                }
            }
            // no video image, draw a dummy image
            else
            {
                Vector2 min = ImGui.GetCursorScreenPos();
                Vector2 max = min + size;
                // draw a black background
                drawList.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.FrameBg));
                if (FuLayout.CurrentDrawerPath.Count > 0)
                {
                    // draw a text to inform the user that he need to import a video
                    FuLayout.CurrentDrawerPath.Peek().EnboxedText("Video player ready\n\nPlease import file to play", min, size, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), FuTextWrapping.None);
                }
                ImGui.Dummy(size);
            }

            // store drawing data
            float fullscreenTimelineHeight = 20f * Fugui.CurrentContext.Scale;
            Vector2 fullScreenButtonSize = new Vector2(20f, 20f) * Fugui.CurrentContext.Scale;
            Vector2 fullScreenButtonPos = pos + size - fullScreenButtonSize - padding - (_isFullScreen ? new Vector2(0f, fullscreenTimelineHeight) : Vector2.zero);

            // store hover states (video image, timeline, full screen button)
            bool isFullScreenHover = ImGui.IsMouseHoveringRect(fullScreenButtonPos, fullScreenButtonPos + fullScreenButtonSize);
            bool isTimelineHover = !isFullScreenHover && ImGui.IsMouseHoveringRect(pos + new Vector2(0f, size.y - fullscreenTimelineHeight), pos + size);
            bool isVideoHover = ImGui.IsMouseHoveringRect(pos, pos + size);

            // set hand cursor if mouse is hover the video image
            if (isVideoHover && enableInterractions)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                // draw pause logo (2 vertical bars in a circle) semi alpha if video is playing
                if (!startFCState && IsPlaying)
                {
                    Vector2 center = pos + size / 2f;
                    float radius = size.y / 4f;
                    drawList.AddCircleFilled(center, radius, ImGui.GetColorU32(ImGuiCol.Text, 0.4f));
                    drawList.AddRectFilled(center + new Vector2(-radius / 2f, -radius / 2f), center + new Vector2(-radius / 4f, radius / 2f), ImGui.GetColorU32(ImGuiCol.Text));
                    drawList.AddRectFilled(center + new Vector2(radius / 4f, -radius / 2f), center + new Vector2(radius / 2f, radius / 2f), ImGui.GetColorU32(ImGuiCol.Text));
                }

                // draw full screen button background 
                drawList.AddRectFilled(fullScreenButtonPos, fullScreenButtonPos + fullScreenButtonSize, ImGui.GetColorU32(ImGuiCol.FrameBg));
                drawList.AddRect(fullScreenButtonPos, fullScreenButtonPos + fullScreenButtonSize, ImGui.GetColorU32(ImGuiCol.Border));
                // draw full screen button icon using drawlist
                float small = (isFullScreenHover ? 2f : 5f) * Fugui.CurrentContext.Scale;
                float big = (isFullScreenHover ? 18f : 15f) * Fugui.CurrentContext.Scale;
                drawList.AddTriangleFilled(fullScreenButtonPos + new Vector2(small, small), fullScreenButtonPos + new Vector2(big, small), fullScreenButtonPos + new Vector2(small, big), ImGui.GetColorU32(ImGuiCol.Text));
                drawList.AddTriangleFilled(fullScreenButtonPos + new Vector2(big, big), fullScreenButtonPos + new Vector2(small, big), fullScreenButtonPos + new Vector2(big, small), ImGui.GetColorU32(ImGuiCol.Text));

                // set tooltip if mouse is hover the full screen button
                if (isFullScreenHover)
                {
                    ImGui.SetTooltip("Full screen (F / Esc)");
                    // switch to full screen if mouse is clicked on the full screen button
                    if (currentContainer.Mouse.IsDown(FuMouseButton.Left))
                    {
                        SetFullScreen(!startFCState);
                    }
                }
                else if (!isTimelineHover && currentContainer.Mouse.IsDown(FuMouseButton.Left))
                {
                    if (Player.isPlaying)
                    {
                        Pause();
                    }
                    else
                    {
                        Play();
                    }
                }
            }

            // check container keyboard if F is pressed to switch to full screen
            if (currentContainer.Keyboard.GetKeyDown(FuKeysCode.F) && enableInterractions)
            {
                SetFullScreen(!startFCState);
            }
            // check container keyboard if F is pressed to switch to full screen
            else if (currentContainer.Keyboard.GetKeyDown(FuKeysCode.Space) && enableInterractions)
            {
                if (Player?.isPlaying ?? false)
                {
                    Pause();
                }
                else
                {
                    Play();
                }
            }

            // draw play logo (Triangle in a circle) semi alpha if video is not playing
            if (!Player?.isPlaying ?? false && enableInterractions)
            {
                Vector2 center = pos + size / Fugui.CurrentContext.Scale / 2f;
                float radius = size.y / 4f;
                drawList.AddCircleFilled(center, radius, ImGui.GetColorU32(ImGuiCol.Text, 0.4f));
                drawList.AddTriangleFilled(center + new Vector2(-radius / 2f, -radius / 2f), center + new Vector2(radius / 2f, 0f), center + new Vector2(-radius / 2f, radius / 2f), ImGui.GetColorU32(ImGuiCol.Text));
            }

            // we start drawing in full screen, draw the timeline
            if (startFCState)
            {
                // exit full screen if escape is pressed
                if (currentContainer.Keyboard.GetKeyPressed(FuKeysCode.Escape) && enableInterractions)
                {
                    SetFullScreen(false);
                }

                // detect if mouse has moved the last 2 seconds to show / hide timeline
                if (Time.time - _lastFrameMousePosTime < 2f)
                {
                    ImGui.SetCursorPos(new Vector2(0f, size.y - fullscreenTimelineHeight));
                    DrawTimeLine(fullscreenTimelineHeight);
                }
                else
                {
                    // hide mouse cursor if mouse is not moving
                    ImGui.SetMouseCursor(ImGuiMouseCursor.None);
                }

                // end the full screen window
                ImGui.End();
                ImGui.PopStyleColor();
            }

            // return the rect of the video image
            return new Rect(pos, size);
        }

        /// <summary>
        /// Draw the video timeline
        /// </summary>
        /// <param name="height">height of the time line bar</param>
        /// <param name="width">width of the time line bar</param>
        public void DrawTimeLine(float height = 16f, float width = -1f)
        {
            // get current container
            IFuWindowContainer currentContainer = FuWindow.CurrentDrawingWindow != null ? FuWindow.CurrentDrawingWindow.Container : Fugui.MainContainer;

            int frame = 0;
            if (Player != null)
            {
                frame = (int)Player.frame;
            }

            var drawList = ImGui.GetWindowDrawList();
            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 size = new Vector2(width <= 0 ? ImGui.GetContentRegionAvail().x : width, height * Fugui.CurrentContext.Scale);
            // draw timelline back and filled
            drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(ImGuiCol.FrameBg));
            // draw timeline already player rect with a little offset set as variable
            float playedWidth = (frame / (float)FrameCount) * size.x;
            drawList.AddRectFilled(pos, pos + new Vector2(size.x, size.y), ImGui.GetColorU32(ImGuiCol.FrameBgHovered));
            if (FrameCount > 0)
            {
                drawList.AddRectFilled(pos, pos + new Vector2(playedWidth, size.y), ImGui.GetColorU32(ImGuiCol.CheckMark));
            }

            // draw timeline time stamps
            bool isHoverTimeStamps = false;
            if (_timeStamps != null)
            {
                foreach (var item in _timeStamps)
                {
                    float stampPos = (item.Key / (float)FrameCount) * size.x;
                    float clickPadding = 8f * Fugui.CurrentContext.Scale;
                    float lineSize = 1f * Fugui.CurrentContext.Scale;
                    bool hovered = false;
                    // set tooltip if mouse is hover the timestamp + a little offset
                    if (ImGui.IsMouseHoveringRect(pos + new Vector2(stampPos - clickPadding + lineSize, 0f), pos + new Vector2(stampPos + clickPadding + lineSize, size.y)))
                    {
                        hovered = true;
                        lineSize = 2f * Fugui.CurrentContext.Scale;
                        ImGui.SetTooltip(item.Value);
                        isHoverTimeStamps = true;

                        // set time if mouse is clicked on the timestamp
                        if (currentContainer.Mouse.IsDown(FuMouseButton.Left))
                        {
                            SetFrame(item.Key);
                        }
                    }
                    drawList.AddLine(pos + new Vector2(stampPos + 1f, 2f), pos + new Vector2(stampPos + 1f, size.y - 2f), ImGui.GetColorU32(ImGuiCol.Text, hovered ? 1f : 0.4f), lineSize);
                }
            }

            // draw timeline border
            drawList.AddRect(pos, pos + size, ImGui.GetColorU32(ImGuiCol.Border));
            ImGui.SetCursorScreenPos(pos);
            ImGui.Dummy(size);

            // set frame on mouse click on the timeline rect
            if (ImGui.IsMouseHoveringRect(pos, pos + size))
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

                // we are not hover timestamp
                if (!isHoverTimeStamps)
                {
#if UNITY_EDITOR
                    // display tooltip with frames data
                    ImGui.SetTooltip(frame + " / " + FrameCount);
#endif
                    // set time if mouse is clicked on the timeline
                    if (currentContainer.Mouse.IsDown(FuMouseButton.Left))
                    {
                        float mousePos = ImGui.GetMousePos().x - pos.x;
                        float frameToSet = (mousePos / size.x) * FrameCount;
                        SetFrame((long)frameToSet);
                    }
                }
            }
        }
    }
}
