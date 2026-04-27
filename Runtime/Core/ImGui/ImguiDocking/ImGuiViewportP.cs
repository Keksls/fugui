using UnityEngine;

namespace ImGuiNET
{
    /// <summary>
    /// Represents the Im Gui Viewport P data structure.
    /// </summary>
    public unsafe partial struct ImGuiViewportP
    {
        #region State
        public ImGuiViewport _ImGuiViewport;
        public int Idx;
        public int LastFrameActive;
        public int LastFrontMostStampCount;
        public uint LastNameHash;
        public Vector2 LastPos;
        public float Alpha;
        public float LastAlpha;
        public short PlatformMonitor;
        public byte PlatformWindowCreated;
        public ImGuiWindow* Window;
        public fixed int DrawListsLastFrame[2];
        public ImDrawList* DrawLists_0;
        public ImDrawList* DrawLists_1;
        public ImDrawData DrawDataP;
        public ImDrawDataBuilder DrawDataBuilder;
        public Vector2 LastPlatformPos;
        public Vector2 LastPlatformSize;
        public Vector2 LastRendererSize;
        public Vector2 WorkOffsetMin;
        public Vector2 WorkOffsetMax;
        public Vector2 CurrWorkOffsetMin;
        public Vector2 CurrWorkOffsetMax;
        #endregion
    }
}