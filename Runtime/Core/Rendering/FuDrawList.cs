using ImGuiNET;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace Fu
{
    public unsafe partial struct FuDrawList
    {
        internal ImDrawList* NativePtr { get; }

        private static IntPtr ResolveTextureId(Texture texture)
        {
            if (texture == null)
            {
                return IntPtr.Zero;
            }

            TextureManager textureManager = FuWindow.CurrentDrawingWindow != null
                ? FuWindow.CurrentDrawingWindow.Container?.Context?.TextureManager
                : Fugui.CurrentContext?.TextureManager;

            if (textureManager == null)
            {
                return IntPtr.Zero;
            }

            return textureManager.GetTextureId(texture);
        }
        internal FuDrawList(ImDrawList* nativePtr) => NativePtr = nativePtr;
        internal FuDrawList(ImDrawListPtr nativePtr) => NativePtr = nativePtr.NativePtr;
        internal FuDrawList(IntPtr nativePtr) => NativePtr = (ImDrawList*)nativePtr;
        public bool IsValid => NativePtr != null;
        internal ImPtrVector<ImDrawCmdPtr> CmdBuffer => new ImPtrVector<ImDrawCmdPtr>(NativePtr->CmdBuffer, Unsafe.SizeOf<ImDrawCmd>());
        internal ImVector<ushort> IdxBuffer => new ImVector<ushort>(NativePtr->IdxBuffer);
        internal ImPtrVector<ImDrawVertPtr> VtxBuffer => new ImPtrVector<ImDrawVertPtr>(NativePtr->VtxBuffer, Unsafe.SizeOf<ImDrawVert>());
        internal ref ImDrawListFlags Flags => ref Unsafe.AsRef<ImDrawListFlags>(&NativePtr->Flags);
        internal ref uint _VtxCurrentIdx => ref Unsafe.AsRef<uint>(&NativePtr->_VtxCurrentIdx);
        internal ref IntPtr _Data => ref Unsafe.AsRef<IntPtr>(&NativePtr->_Data);
        internal ImDrawVertPtr _VtxWritePtr => new ImDrawVertPtr(NativePtr->_VtxWritePtr);
        internal IntPtr _IdxWritePtr { get => (IntPtr)NativePtr->_IdxWritePtr; set => NativePtr->_IdxWritePtr = (ushort*)value; }
        internal ImVector<Vector2> _Path => new ImVector<Vector2>(NativePtr->_Path);
        internal ref ImDrawCmdHeader _CmdHeader => ref Unsafe.AsRef<ImDrawCmdHeader>(&NativePtr->_CmdHeader);
        internal ref ImDrawListSplitter _Splitter => ref Unsafe.AsRef<ImDrawListSplitter>(&NativePtr->_Splitter);
        internal ImVector<Vector4> _ClipRectStack => new ImVector<Vector4>(NativePtr->_ClipRectStack);
        internal ImVector<IntPtr> _TextureIdStack => new ImVector<IntPtr>(NativePtr->_TextureIdStack);
        internal ImVector<byte> _CallbacksDataBuf => new ImVector<byte>(NativePtr->_CallbacksDataBuf);
        internal ref float _FringeScale => ref Unsafe.AsRef<float>(&NativePtr->_FringeScale);
        internal NullTerminatedString _OwnerName => new NullTerminatedString(NativePtr->_OwnerName);
        public int _CalcCircleAutoSegmentCount(float radius)
        {
            int ret = ImGuiNative.ImDrawList__CalcCircleAutoSegmentCount((ImDrawList*)(NativePtr), radius);
            return ret;
        }
        public void _ClearFreeMemory()
        {
            ImGuiNative.ImDrawList__ClearFreeMemory((ImDrawList*)(NativePtr));
        }
        public void _OnChangedClipRect()
        {
            ImGuiNative.ImDrawList__OnChangedClipRect((ImDrawList*)(NativePtr));
        }
        public void _OnChangedTextureID()
        {
            ImGuiNative.ImDrawList__OnChangedTextureID((ImDrawList*)(NativePtr));
        }
        public void _OnChangedVtxOffset()
        {
            ImGuiNative.ImDrawList__OnChangedVtxOffset((ImDrawList*)(NativePtr));
        }
        public void _PathArcToFastEx(Vector2 center, float radius, int a_min_sample, int a_max_sample, int a_step)
        {
            ImGuiNative.ImDrawList__PathArcToFastEx((ImDrawList*)(NativePtr), center, radius, a_min_sample, a_max_sample, a_step);
        }
        public void _PathArcToN(Vector2 center, float radius, float a_min, float a_max, int num_segments)
        {
            ImGuiNative.ImDrawList__PathArcToN((ImDrawList*)(NativePtr), center, radius, a_min, a_max, num_segments);
        }
        public void _PopUnusedDrawCmd()
        {
            ImGuiNative.ImDrawList__PopUnusedDrawCmd((ImDrawList*)(NativePtr));
        }
        public void _ResetForNewFrame()
        {
            ImGuiNative.ImDrawList__ResetForNewFrame((ImDrawList*)(NativePtr));
        }
        public void _SetTextureID(IntPtr texture_id)
        {
            ImGuiNative.ImDrawList__SetTextureID((ImDrawList*)(NativePtr), texture_id);
        }
        public void _TryMergeDrawCmds()
        {
            ImGuiNative.ImDrawList__TryMergeDrawCmds((ImDrawList*)(NativePtr));
        }
        public void AddBezierCubic(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, uint col, float thickness)
        {
            int num_segments = 0;
            ImGuiNative.ImDrawList_AddBezierCubic((ImDrawList*)(NativePtr), p1, p2, p3, p4, col, thickness, num_segments);
        }
        public void AddBezierCubic(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, uint col, float thickness, int num_segments)
        {
            ImGuiNative.ImDrawList_AddBezierCubic((ImDrawList*)(NativePtr), p1, p2, p3, p4, col, thickness, num_segments);
        }
        public void AddBezierQuadratic(Vector2 p1, Vector2 p2, Vector2 p3, uint col, float thickness)
        {
            int num_segments = 0;
            ImGuiNative.ImDrawList_AddBezierQuadratic((ImDrawList*)(NativePtr), p1, p2, p3, col, thickness, num_segments);
        }
        public void AddBezierQuadratic(Vector2 p1, Vector2 p2, Vector2 p3, uint col, float thickness, int num_segments)
        {
            ImGuiNative.ImDrawList_AddBezierQuadratic((ImDrawList*)(NativePtr), p1, p2, p3, col, thickness, num_segments);
        }
        public void AddCallback(IntPtr callback, IntPtr userdata)
        {
            void* native_userdata = (void*)userdata.ToPointer();
            uint userdata_size = 0;
            ImGuiNative.ImDrawList_AddCallback((ImDrawList*)(NativePtr), callback, native_userdata, userdata_size);
        }
        public void AddCallback(IntPtr callback, IntPtr userdata, uint userdata_size)
        {
            void* native_userdata = (void*)userdata.ToPointer();
            ImGuiNative.ImDrawList_AddCallback((ImDrawList*)(NativePtr), callback, native_userdata, userdata_size);
        }
        public void AddCircle(Vector2 center, float radius, uint col)
        {
            int num_segments = 0;
            float thickness = 1.0f;
            ImGuiNative.ImDrawList_AddCircle((ImDrawList*)(NativePtr), center, radius, col, num_segments, thickness);
        }
        public void AddCircle(Vector2 center, float radius, uint col, int num_segments)
        {
            float thickness = 1.0f;
            ImGuiNative.ImDrawList_AddCircle((ImDrawList*)(NativePtr), center, radius, col, num_segments, thickness);
        }
        public void AddCircle(Vector2 center, float radius, uint col, int num_segments, float thickness)
        {
            ImGuiNative.ImDrawList_AddCircle((ImDrawList*)(NativePtr), center, radius, col, num_segments, thickness);
        }
        public void AddCircleFilled(Vector2 center, float radius, uint col)
        {
            int num_segments = 0;
            ImGuiNative.ImDrawList_AddCircleFilled((ImDrawList*)(NativePtr), center, radius, col, num_segments);
        }
        public void AddCircleFilled(Vector2 center, float radius, uint col, int num_segments)
        {
            ImGuiNative.ImDrawList_AddCircleFilled((ImDrawList*)(NativePtr), center, radius, col, num_segments);
        }
        public void AddConcavePolyFilled(ref Vector2 points, int num_points, uint col)
        {
            fixed (Vector2* native_points = &points)
            {
                ImGuiNative.ImDrawList_AddConcavePolyFilled((ImDrawList*)(NativePtr), native_points, num_points, col);
            }
        }
        public void AddConvexPolyFilled(ref Vector2 points, int num_points, uint col)
        {
            fixed (Vector2* native_points = &points)
            {
                ImGuiNative.ImDrawList_AddConvexPolyFilled((ImDrawList*)(NativePtr), native_points, num_points, col);
            }
        }
        public void AddDrawCmd()
        {
            ImGuiNative.ImDrawList_AddDrawCmd((ImDrawList*)(NativePtr));
        }
        public void AddEllipse(Vector2 center, Vector2 radius, uint col)
        {
            float rot = 0.0f;
            int num_segments = 0;
            float thickness = 1.0f;
            ImGuiNative.ImDrawList_AddEllipse((ImDrawList*)(NativePtr), center, radius, col, rot, num_segments, thickness);
        }
        public void AddEllipse(Vector2 center, Vector2 radius, uint col, float rot)
        {
            int num_segments = 0;
            float thickness = 1.0f;
            ImGuiNative.ImDrawList_AddEllipse((ImDrawList*)(NativePtr), center, radius, col, rot, num_segments, thickness);
        }
        public void AddEllipse(Vector2 center, Vector2 radius, uint col, float rot, int num_segments)
        {
            float thickness = 1.0f;
            ImGuiNative.ImDrawList_AddEllipse((ImDrawList*)(NativePtr), center, radius, col, rot, num_segments, thickness);
        }
        public void AddEllipse(Vector2 center, Vector2 radius, uint col, float rot, int num_segments, float thickness)
        {
            ImGuiNative.ImDrawList_AddEllipse((ImDrawList*)(NativePtr), center, radius, col, rot, num_segments, thickness);
        }
        public void AddEllipseFilled(Vector2 center, Vector2 radius, uint col)
        {
            float rot = 0.0f;
            int num_segments = 0;
            ImGuiNative.ImDrawList_AddEllipseFilled((ImDrawList*)(NativePtr), center, radius, col, rot, num_segments);
        }
        public void AddEllipseFilled(Vector2 center, Vector2 radius, uint col, float rot)
        {
            int num_segments = 0;
            ImGuiNative.ImDrawList_AddEllipseFilled((ImDrawList*)(NativePtr), center, radius, col, rot, num_segments);
        }
        public void AddEllipseFilled(Vector2 center, Vector2 radius, uint col, float rot, int num_segments)
        {
            ImGuiNative.ImDrawList_AddEllipseFilled((ImDrawList*)(NativePtr), center, radius, col, rot, num_segments);
        }
        internal void AddImage(IntPtr user_texture_id, Vector2 p_min, Vector2 p_max)
        {
            Vector2 uv_min = new Vector2();
            Vector2 uv_max = new Vector2(1, 1);
            uint col = 4294967295;
            ImGuiNative.ImDrawList_AddImage((ImDrawList*)(NativePtr), user_texture_id, p_min, p_max, uv_min, uv_max, col);
        }
        internal void AddImage(IntPtr user_texture_id, Vector2 p_min, Vector2 p_max, Vector2 uv_min)
        {
            Vector2 uv_max = new Vector2(1, 1);
            uint col = 4294967295;
            ImGuiNative.ImDrawList_AddImage((ImDrawList*)(NativePtr), user_texture_id, p_min, p_max, uv_min, uv_max, col);
        }
        internal void AddImage(IntPtr user_texture_id, Vector2 p_min, Vector2 p_max, Vector2 uv_min, Vector2 uv_max)
        {
            uint col = 4294967295;
            ImGuiNative.ImDrawList_AddImage((ImDrawList*)(NativePtr), user_texture_id, p_min, p_max, uv_min, uv_max, col);
        }
        internal void AddImage(IntPtr user_texture_id, Vector2 p_min, Vector2 p_max, Vector2 uv_min, Vector2 uv_max, uint col)
        {
            ImGuiNative.ImDrawList_AddImage((ImDrawList*)(NativePtr), user_texture_id, p_min, p_max, uv_min, uv_max, col);
        }
        internal void AddImageQuad(IntPtr user_texture_id, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            Vector2 uv1 = new Vector2();
            Vector2 uv2 = new Vector2(1, 0);
            Vector2 uv3 = new Vector2(1, 1);
            Vector2 uv4 = new Vector2(0, 1);
            uint col = 4294967295;
            ImGuiNative.ImDrawList_AddImageQuad((ImDrawList*)(NativePtr), user_texture_id, p1, p2, p3, p4, uv1, uv2, uv3, uv4, col);
        }
        internal void AddImageQuad(IntPtr user_texture_id, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Vector2 uv1)
        {
            Vector2 uv2 = new Vector2(1, 0);
            Vector2 uv3 = new Vector2(1, 1);
            Vector2 uv4 = new Vector2(0, 1);
            uint col = 4294967295;
            ImGuiNative.ImDrawList_AddImageQuad((ImDrawList*)(NativePtr), user_texture_id, p1, p2, p3, p4, uv1, uv2, uv3, uv4, col);
        }
        internal void AddImageQuad(IntPtr user_texture_id, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Vector2 uv1, Vector2 uv2)
        {
            Vector2 uv3 = new Vector2(1, 1);
            Vector2 uv4 = new Vector2(0, 1);
            uint col = 4294967295;
            ImGuiNative.ImDrawList_AddImageQuad((ImDrawList*)(NativePtr), user_texture_id, p1, p2, p3, p4, uv1, uv2, uv3, uv4, col);
        }
        internal void AddImageQuad(IntPtr user_texture_id, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Vector2 uv1, Vector2 uv2, Vector2 uv3)
        {
            Vector2 uv4 = new Vector2(0, 1);
            uint col = 4294967295;
            ImGuiNative.ImDrawList_AddImageQuad((ImDrawList*)(NativePtr), user_texture_id, p1, p2, p3, p4, uv1, uv2, uv3, uv4, col);
        }
        internal void AddImageQuad(IntPtr user_texture_id, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
        {
            uint col = 4294967295;
            ImGuiNative.ImDrawList_AddImageQuad((ImDrawList*)(NativePtr), user_texture_id, p1, p2, p3, p4, uv1, uv2, uv3, uv4, col);
        }
        internal void AddImageQuad(IntPtr user_texture_id, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4, uint col)
        {
            ImGuiNative.ImDrawList_AddImageQuad((ImDrawList*)(NativePtr), user_texture_id, p1, p2, p3, p4, uv1, uv2, uv3, uv4, col);
        }
        internal void AddImageRounded(IntPtr user_texture_id, Vector2 p_min, Vector2 p_max, Vector2 uv_min, Vector2 uv_max, uint col, float rounding)
        {
            ImDrawFlags flags = (ImDrawFlags)0;
            ImGuiNative.ImDrawList_AddImageRounded((ImDrawList*)(NativePtr), user_texture_id, p_min, p_max, uv_min, uv_max, col, rounding, flags);
        }
        internal void AddImageRoundedNative(IntPtr user_texture_id, Vector2 p_min, Vector2 p_max, Vector2 uv_min, Vector2 uv_max, uint col, float rounding, ImDrawFlags flags)
        {
            ImGuiNative.ImDrawList_AddImageRounded((ImDrawList*)(NativePtr), user_texture_id, p_min, p_max, uv_min, uv_max, col, rounding, flags);
        }
        public void AddImage(Texture texture, Vector2 p_min, Vector2 p_max)
        {
            AddImage(ResolveTextureId(texture), p_min, p_max);
        }

        public void AddImage(Texture texture, Vector2 p_min, Vector2 p_max, Vector2 uv_min)
        {
            AddImage(ResolveTextureId(texture), p_min, p_max, uv_min);
        }

        public void AddImage(Texture texture, Vector2 p_min, Vector2 p_max, Vector2 uv_min, Vector2 uv_max)
        {
            AddImage(ResolveTextureId(texture), p_min, p_max, uv_min, uv_max);
        }

        public void AddImage(Texture texture, Vector2 p_min, Vector2 p_max, Vector2 uv_min, Vector2 uv_max, uint col)
        {
            AddImage(ResolveTextureId(texture), p_min, p_max, uv_min, uv_max, col);
        }

        public void AddImage(Texture texture, Vector2 p_min, Vector2 p_max, Vector2 uv_min, Vector2 uv_max, Vector4 color)
        {
            AddImage(texture, p_min, p_max, uv_min, uv_max, ImGui.GetColorU32(color));
        }

        public void AddImage(Texture texture, Vector2 p_min, Vector2 p_max, Vector4 color)
        {
            AddImage(texture, p_min, p_max, Vector2.zero, Vector2.one, ImGui.GetColorU32(color));
        }

        public void AddImageQuad(Texture texture, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            AddImageQuad(ResolveTextureId(texture), p1, p2, p3, p4);
        }

        public void AddImageQuad(Texture texture, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Vector2 uv1)
        {
            AddImageQuad(ResolveTextureId(texture), p1, p2, p3, p4, uv1);
        }

        public void AddImageQuad(Texture texture, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Vector2 uv1, Vector2 uv2)
        {
            AddImageQuad(ResolveTextureId(texture), p1, p2, p3, p4, uv1, uv2);
        }

        public void AddImageQuad(Texture texture, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Vector2 uv1, Vector2 uv2, Vector2 uv3)
        {
            AddImageQuad(ResolveTextureId(texture), p1, p2, p3, p4, uv1, uv2, uv3);
        }

        public void AddImageQuad(Texture texture, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
        {
            AddImageQuad(ResolveTextureId(texture), p1, p2, p3, p4, uv1, uv2, uv3, uv4);
        }

        public void AddImageQuad(Texture texture, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4, uint col)
        {
            AddImageQuad(ResolveTextureId(texture), p1, p2, p3, p4, uv1, uv2, uv3, uv4, col);
        }

        public void AddImageQuad(Texture texture, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4, Vector4 color)
        {
            AddImageQuad(texture, p1, p2, p3, p4, uv1, uv2, uv3, uv4, ImGui.GetColorU32(color));
        }

        public void AddImageRounded(Texture texture, Vector2 p_min, Vector2 p_max, Vector2 uv_min, Vector2 uv_max, uint col, float rounding)
        {
            AddImageRounded(ResolveTextureId(texture), p_min, p_max, uv_min, uv_max, col, rounding);
        }

        public void AddImageRounded(Texture texture, Vector2 p_min, Vector2 p_max, Vector2 uv_min, Vector2 uv_max, Vector4 color, float rounding)
        {
            AddImageRounded(texture, p_min, p_max, uv_min, uv_max, ImGui.GetColorU32(color), rounding);
        }

        public void AddImageRounded(Texture texture, Vector2 p_min, Vector2 p_max, Vector2 uv_min, Vector2 uv_max, uint col, float rounding, FuDrawFlags flags)
        {
            AddImageRoundedNative(ResolveTextureId(texture), p_min, p_max, uv_min, uv_max, col, rounding, (ImDrawFlags)flags);
        }

        public void AddImageRounded(Texture texture, Vector2 p_min, Vector2 p_max, Vector2 uv_min, Vector2 uv_max, Vector4 color, float rounding, FuDrawFlags flags)
        {
            AddImageRounded(texture, p_min, p_max, uv_min, uv_max, ImGui.GetColorU32(color), rounding, flags);
        }
        public void AddLine(Vector2 p1, Vector2 p2, uint col)
        {
            float thickness = 1.0f;
            ImGuiNative.ImDrawList_AddLine((ImDrawList*)(NativePtr), p1, p2, col, thickness);
        }
        public void AddLine(Vector2 p1, Vector2 p2, uint col, float thickness)
        {
            ImGuiNative.ImDrawList_AddLine((ImDrawList*)(NativePtr), p1, p2, col, thickness);
        }
        public void AddNgon(Vector2 center, float radius, uint col, int num_segments)
        {
            float thickness = 1.0f;
            ImGuiNative.ImDrawList_AddNgon((ImDrawList*)(NativePtr), center, radius, col, num_segments, thickness);
        }
        public void AddNgon(Vector2 center, float radius, uint col, int num_segments, float thickness)
        {
            ImGuiNative.ImDrawList_AddNgon((ImDrawList*)(NativePtr), center, radius, col, num_segments, thickness);
        }
        public void AddNgonFilled(Vector2 center, float radius, uint col, int num_segments)
        {
            ImGuiNative.ImDrawList_AddNgonFilled((ImDrawList*)(NativePtr), center, radius, col, num_segments);
        }
        internal void AddPolylineNative(ref Vector2 points, int num_points, uint col, ImDrawFlags flags, float thickness)
        {
            fixed (Vector2* native_points = &points)
            {
                ImGuiNative.ImDrawList_AddPolyline((ImDrawList*)(NativePtr), native_points, num_points, col, flags, thickness);
            }
        }
        public void AddQuad(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, uint col)
        {
            float thickness = 1.0f;
            ImGuiNative.ImDrawList_AddQuad((ImDrawList*)(NativePtr), p1, p2, p3, p4, col, thickness);
        }
        public void AddQuad(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, uint col, float thickness)
        {
            ImGuiNative.ImDrawList_AddQuad((ImDrawList*)(NativePtr), p1, p2, p3, p4, col, thickness);
        }
        public void AddQuadFilled(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, uint col)
        {
            ImGuiNative.ImDrawList_AddQuadFilled((ImDrawList*)(NativePtr), p1, p2, p3, p4, col);
        }
        public void AddRect(Vector2 p_min, Vector2 p_max, uint col)
        {
            float rounding = 0.0f;
            ImDrawFlags flags = (ImDrawFlags)0;
            float thickness = 1.0f;
            ImGuiNative.ImDrawList_AddRect((ImDrawList*)(NativePtr), p_min, p_max, col, rounding, flags, thickness);
        }
        public void AddRect(Vector2 p_min, Vector2 p_max, uint col, float rounding)
        {
            ImDrawFlags flags = (ImDrawFlags)0;
            float thickness = 1.0f;
            ImGuiNative.ImDrawList_AddRect((ImDrawList*)(NativePtr), p_min, p_max, col, rounding, flags, thickness);
        }
        internal void AddRectNative(Vector2 p_min, Vector2 p_max, uint col, float rounding, ImDrawFlags flags)
        {
            float thickness = 1.0f;
            ImGuiNative.ImDrawList_AddRect((ImDrawList*)(NativePtr), p_min, p_max, col, rounding, flags, thickness);
        }
        internal void AddRectNative(Vector2 p_min, Vector2 p_max, uint col, float rounding, ImDrawFlags flags, float thickness)
        {
            ImGuiNative.ImDrawList_AddRect((ImDrawList*)(NativePtr), p_min, p_max, col, rounding, flags, thickness);
        }
        public void AddRectFilled(Vector2 p_min, Vector2 p_max, uint col)
        {
            float rounding = 0.0f;
            ImDrawFlags flags = (ImDrawFlags)0;
            ImGuiNative.ImDrawList_AddRectFilled((ImDrawList*)(NativePtr), p_min, p_max, col, rounding, flags);
        }
        public void AddRectFilled(Vector2 p_min, Vector2 p_max, uint col, float rounding)
        {
            ImDrawFlags flags = (ImDrawFlags)0;
            ImGuiNative.ImDrawList_AddRectFilled((ImDrawList*)(NativePtr), p_min, p_max, col, rounding, flags);
        }
        internal void AddRectFilledNative(Vector2 p_min, Vector2 p_max, uint col, float rounding, ImDrawFlags flags)
        {
            ImGuiNative.ImDrawList_AddRectFilled((ImDrawList*)(NativePtr), p_min, p_max, col, rounding, flags);
        }
        public void AddRectFilledMultiColor(Vector2 p_min, Vector2 p_max, uint col_upr_left, uint col_upr_right, uint col_bot_right, uint col_bot_left)
        {
            ImGuiNative.ImDrawList_AddRectFilledMultiColor((ImDrawList*)(NativePtr), p_min, p_max, col_upr_left, col_upr_right, col_bot_right, col_bot_left);
        }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        public void AddText(Vector2 pos, uint col, ReadOnlySpan<char> text_begin)
        {
            byte* native_text_begin;
            int text_begin_byteCount = 0;
            text_begin_byteCount = Encoding.UTF8.GetByteCount(text_begin);
            if (text_begin_byteCount > Util.StackAllocationSizeLimit)
            {
                native_text_begin = Util.Allocate(text_begin_byteCount + 1);
            }
            else
            {
                byte* native_text_begin_stackBytes = stackalloc byte[text_begin_byteCount + 1];
                native_text_begin = native_text_begin_stackBytes;
            }
            int native_text_begin_offset = Util.GetUtf8(text_begin, native_text_begin, text_begin_byteCount);
            native_text_begin[native_text_begin_offset] = 0;
            ImGuiNative.ImDrawList_AddText_Vec2((ImDrawList*)(NativePtr), pos, col, native_text_begin, native_text_begin + text_begin_byteCount);
            if (text_begin_byteCount > Util.StackAllocationSizeLimit)
            {
                Util.Free(native_text_begin);
            }
        }
#endif
        public void AddText(Vector2 pos, uint col, string text_begin)
        {
            byte* native_text_begin;
            int text_begin_byteCount = 0;
            text_begin_byteCount = Encoding.UTF8.GetByteCount(text_begin);
            if (text_begin_byteCount > Util.StackAllocationSizeLimit)
            {
                native_text_begin = Util.Allocate(text_begin_byteCount + 1);
            }
            else
            {
                byte* native_text_begin_stackBytes = stackalloc byte[text_begin_byteCount + 1];
                native_text_begin = native_text_begin_stackBytes;
            }
            int native_text_begin_offset = Util.GetUtf8(text_begin, native_text_begin, text_begin_byteCount);
            native_text_begin[native_text_begin_offset] = 0;
            ImGuiNative.ImDrawList_AddText_Vec2((ImDrawList*)(NativePtr), pos, col, native_text_begin, native_text_begin + text_begin_byteCount);
            if (text_begin_byteCount > Util.StackAllocationSizeLimit)
            {
                Util.Free(native_text_begin);
            }
        }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        internal void AddText(ImFontPtr font, float font_size, Vector2 pos, uint col, ReadOnlySpan<char> text_begin)
        {
            ImFont* native_font = font.NativePtr;
            byte* native_text_begin;
            int text_begin_byteCount = 0;
            text_begin_byteCount = Encoding.UTF8.GetByteCount(text_begin);
            if (text_begin_byteCount > Util.StackAllocationSizeLimit)
            {
                native_text_begin = Util.Allocate(text_begin_byteCount + 1);
            }
            else
            {
                byte* native_text_begin_stackBytes = stackalloc byte[text_begin_byteCount + 1];
                native_text_begin = native_text_begin_stackBytes;
            }
            int native_text_begin_offset = Util.GetUtf8(text_begin, native_text_begin, text_begin_byteCount);
            native_text_begin[native_text_begin_offset] = 0;
            float wrap_width = 0.0f;
            Vector4* cpu_fine_clip_rect = null;
            ImGuiNative.ImDrawList_AddText_FontPtr((ImDrawList*)(NativePtr), native_font, font_size, pos, col, native_text_begin, native_text_begin + text_begin_byteCount, wrap_width, cpu_fine_clip_rect);
            if (text_begin_byteCount > Util.StackAllocationSizeLimit)
            {
                Util.Free(native_text_begin);
            }
        }
#endif
        internal void AddText(ImFontPtr font, float font_size, Vector2 pos, uint col, string text_begin)
        {
            ImFont* native_font = font.NativePtr;
            byte* native_text_begin;
            int text_begin_byteCount = 0;
            text_begin_byteCount = Encoding.UTF8.GetByteCount(text_begin);
            if (text_begin_byteCount > Util.StackAllocationSizeLimit)
            {
                native_text_begin = Util.Allocate(text_begin_byteCount + 1);
            }
            else
            {
                byte* native_text_begin_stackBytes = stackalloc byte[text_begin_byteCount + 1];
                native_text_begin = native_text_begin_stackBytes;
            }
            int native_text_begin_offset = Util.GetUtf8(text_begin, native_text_begin, text_begin_byteCount);
            native_text_begin[native_text_begin_offset] = 0;
            float wrap_width = 0.0f;
            Vector4* cpu_fine_clip_rect = null;
            ImGuiNative.ImDrawList_AddText_FontPtr((ImDrawList*)(NativePtr), native_font, font_size, pos, col, native_text_begin, native_text_begin + text_begin_byteCount, wrap_width, cpu_fine_clip_rect);
            if (text_begin_byteCount > Util.StackAllocationSizeLimit)
            {
                Util.Free(native_text_begin);
            }
        }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        internal void AddText(ImFontPtr font, float font_size, Vector2 pos, uint col, ReadOnlySpan<char> text_begin, float wrap_width)
        {
            ImFont* native_font = font.NativePtr;
            byte* native_text_begin;
            int text_begin_byteCount = 0;
            text_begin_byteCount = Encoding.UTF8.GetByteCount(text_begin);
            if (text_begin_byteCount > Util.StackAllocationSizeLimit)
            {
                native_text_begin = Util.Allocate(text_begin_byteCount + 1);
            }
            else
            {
                byte* native_text_begin_stackBytes = stackalloc byte[text_begin_byteCount + 1];
                native_text_begin = native_text_begin_stackBytes;
            }
            int native_text_begin_offset = Util.GetUtf8(text_begin, native_text_begin, text_begin_byteCount);
            native_text_begin[native_text_begin_offset] = 0;
            Vector4* cpu_fine_clip_rect = null;
            ImGuiNative.ImDrawList_AddText_FontPtr((ImDrawList*)(NativePtr), native_font, font_size, pos, col, native_text_begin, native_text_begin + text_begin_byteCount, wrap_width, cpu_fine_clip_rect);
            if (text_begin_byteCount > Util.StackAllocationSizeLimit)
            {
                Util.Free(native_text_begin);
            }
        }
#endif
        internal void AddText(ImFontPtr font, float font_size, Vector2 pos, uint col, string text_begin, float wrap_width)
        {
            ImFont* native_font = font.NativePtr;
            byte* native_text_begin;
            int text_begin_byteCount = 0;
            text_begin_byteCount = Encoding.UTF8.GetByteCount(text_begin);
            if (text_begin_byteCount > Util.StackAllocationSizeLimit)
            {
                native_text_begin = Util.Allocate(text_begin_byteCount + 1);
            }
            else
            {
                byte* native_text_begin_stackBytes = stackalloc byte[text_begin_byteCount + 1];
                native_text_begin = native_text_begin_stackBytes;
            }
            int native_text_begin_offset = Util.GetUtf8(text_begin, native_text_begin, text_begin_byteCount);
            native_text_begin[native_text_begin_offset] = 0;
            Vector4* cpu_fine_clip_rect = null;
            ImGuiNative.ImDrawList_AddText_FontPtr((ImDrawList*)(NativePtr), native_font, font_size, pos, col, native_text_begin, native_text_begin + text_begin_byteCount, wrap_width, cpu_fine_clip_rect);
            if (text_begin_byteCount > Util.StackAllocationSizeLimit)
            {
                Util.Free(native_text_begin);
            }
        }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        internal void AddText(ImFontPtr font, float font_size, Vector2 pos, uint col, ReadOnlySpan<char> text_begin, float wrap_width, ref Vector4 cpu_fine_clip_rect)
        {
            ImFont* native_font = font.NativePtr;
            byte* native_text_begin;
            int text_begin_byteCount = 0;
            text_begin_byteCount = Encoding.UTF8.GetByteCount(text_begin);
            if (text_begin_byteCount > Util.StackAllocationSizeLimit)
            {
                native_text_begin = Util.Allocate(text_begin_byteCount + 1);
            }
            else
            {
                byte* native_text_begin_stackBytes = stackalloc byte[text_begin_byteCount + 1];
                native_text_begin = native_text_begin_stackBytes;
            }
            int native_text_begin_offset = Util.GetUtf8(text_begin, native_text_begin, text_begin_byteCount);
            native_text_begin[native_text_begin_offset] = 0;
            fixed (Vector4* native_cpu_fine_clip_rect = &cpu_fine_clip_rect)
            {
                ImGuiNative.ImDrawList_AddText_FontPtr((ImDrawList*)(NativePtr), native_font, font_size, pos, col, native_text_begin, native_text_begin + text_begin_byteCount, wrap_width, native_cpu_fine_clip_rect);
                if (text_begin_byteCount > Util.StackAllocationSizeLimit)
                {
                    Util.Free(native_text_begin);
                }
            }
        }
#endif
        internal void AddText(ImFontPtr font, float font_size, Vector2 pos, uint col, string text_begin, float wrap_width, ref Vector4 cpu_fine_clip_rect)
        {
            ImFont* native_font = font.NativePtr;
            byte* native_text_begin;
            int text_begin_byteCount = 0;
            text_begin_byteCount = Encoding.UTF8.GetByteCount(text_begin);
            if (text_begin_byteCount > Util.StackAllocationSizeLimit)
            {
                native_text_begin = Util.Allocate(text_begin_byteCount + 1);
            }
            else
            {
                byte* native_text_begin_stackBytes = stackalloc byte[text_begin_byteCount + 1];
                native_text_begin = native_text_begin_stackBytes;
            }
            int native_text_begin_offset = Util.GetUtf8(text_begin, native_text_begin, text_begin_byteCount);
            native_text_begin[native_text_begin_offset] = 0;
            fixed (Vector4* native_cpu_fine_clip_rect = &cpu_fine_clip_rect)
            {
                ImGuiNative.ImDrawList_AddText_FontPtr((ImDrawList*)(NativePtr), native_font, font_size, pos, col, native_text_begin, native_text_begin + text_begin_byteCount, wrap_width, native_cpu_fine_clip_rect);
                if (text_begin_byteCount > Util.StackAllocationSizeLimit)
                {
                    Util.Free(native_text_begin);
                }
            }
        }
        public void AddTriangle(Vector2 p1, Vector2 p2, Vector2 p3, uint col)
        {
            float thickness = 1.0f;
            ImGuiNative.ImDrawList_AddTriangle((ImDrawList*)(NativePtr), p1, p2, p3, col, thickness);
        }
        public void AddTriangle(Vector2 p1, Vector2 p2, Vector2 p3, uint col, float thickness)
        {
            ImGuiNative.ImDrawList_AddTriangle((ImDrawList*)(NativePtr), p1, p2, p3, col, thickness);
        }
        public void AddTriangleFilled(Vector2 p1, Vector2 p2, Vector2 p3, uint col)
        {
            ImGuiNative.ImDrawList_AddTriangleFilled((ImDrawList*)(NativePtr), p1, p2, p3, col);
        }
        public void ChannelsMerge()
        {
            ImGuiNative.ImDrawList_ChannelsMerge((ImDrawList*)(NativePtr));
        }
        public void ChannelsSetCurrent(int n)
        {
            ImGuiNative.ImDrawList_ChannelsSetCurrent((ImDrawList*)(NativePtr), n);
        }
        public void ChannelsSplit(int count)
        {
            ImGuiNative.ImDrawList_ChannelsSplit((ImDrawList*)(NativePtr), count);
        }
        public FuDrawList CloneOutput()
        {
            ImDrawList* ret = ImGuiNative.ImDrawList_CloneOutput((ImDrawList*)(NativePtr));
            return new FuDrawList(ret);
        }
        public void Destroy()
        {
            ImGuiNative.ImDrawList_destroy((ImDrawList*)(NativePtr));
        }
        public Vector2 GetClipRectMax()
        {
            Vector2 __retval;
            ImGuiNative.ImDrawList_GetClipRectMax(&__retval, (ImDrawList*)(NativePtr));
            return __retval;
        }
        public Vector2 GetClipRectMin()
        {
            Vector2 __retval;
            ImGuiNative.ImDrawList_GetClipRectMin(&__retval, (ImDrawList*)(NativePtr));
            return __retval;
        }
        public void PathArcTo(Vector2 center, float radius, float a_min, float a_max)
        {
            int num_segments = 0;
            ImGuiNative.ImDrawList_PathArcTo((ImDrawList*)(NativePtr), center, radius, a_min, a_max, num_segments);
        }
        public void PathArcTo(Vector2 center, float radius, float a_min, float a_max, int num_segments)
        {
            ImGuiNative.ImDrawList_PathArcTo((ImDrawList*)(NativePtr), center, radius, a_min, a_max, num_segments);
        }
        public void PathArcToFast(Vector2 center, float radius, int a_min_of_12, int a_max_of_12)
        {
            ImGuiNative.ImDrawList_PathArcToFast((ImDrawList*)(NativePtr), center, radius, a_min_of_12, a_max_of_12);
        }
        public void PathBezierCubicCurveTo(Vector2 p2, Vector2 p3, Vector2 p4)
        {
            int num_segments = 0;
            ImGuiNative.ImDrawList_PathBezierCubicCurveTo((ImDrawList*)(NativePtr), p2, p3, p4, num_segments);
        }
        public void PathBezierCubicCurveTo(Vector2 p2, Vector2 p3, Vector2 p4, int num_segments)
        {
            ImGuiNative.ImDrawList_PathBezierCubicCurveTo((ImDrawList*)(NativePtr), p2, p3, p4, num_segments);
        }
        public void PathBezierQuadraticCurveTo(Vector2 p2, Vector2 p3)
        {
            int num_segments = 0;
            ImGuiNative.ImDrawList_PathBezierQuadraticCurveTo((ImDrawList*)(NativePtr), p2, p3, num_segments);
        }
        public void PathBezierQuadraticCurveTo(Vector2 p2, Vector2 p3, int num_segments)
        {
            ImGuiNative.ImDrawList_PathBezierQuadraticCurveTo((ImDrawList*)(NativePtr), p2, p3, num_segments);
        }
        public void PathClear()
        {
            ImGuiNative.ImDrawList_PathClear((ImDrawList*)(NativePtr));
        }
        public void PathEllipticalArcTo(Vector2 center, Vector2 radius, float rot, float a_min, float a_max)
        {
            int num_segments = 0;
            ImGuiNative.ImDrawList_PathEllipticalArcTo((ImDrawList*)(NativePtr), center, radius, rot, a_min, a_max, num_segments);
        }
        public void PathEllipticalArcTo(Vector2 center, Vector2 radius, float rot, float a_min, float a_max, int num_segments)
        {
            ImGuiNative.ImDrawList_PathEllipticalArcTo((ImDrawList*)(NativePtr), center, radius, rot, a_min, a_max, num_segments);
        }
        public void PathFillConcave(uint col)
        {
            ImGuiNative.ImDrawList_PathFillConcave((ImDrawList*)(NativePtr), col);
        }
        public void PathFillConvex(uint col)
        {
            ImGuiNative.ImDrawList_PathFillConvex((ImDrawList*)(NativePtr), col);
        }
        public void PathLineTo(Vector2 pos)
        {
            ImGuiNative.ImDrawList_PathLineTo((ImDrawList*)(NativePtr), pos);
        }
        public void PathLineToMergeDuplicate(Vector2 pos)
        {
            ImGuiNative.ImDrawList_PathLineToMergeDuplicate((ImDrawList*)(NativePtr), pos);
        }
        public void PathRect(Vector2 rect_min, Vector2 rect_max)
        {
            float rounding = 0.0f;
            ImDrawFlags flags = (ImDrawFlags)0;
            ImGuiNative.ImDrawList_PathRect((ImDrawList*)(NativePtr), rect_min, rect_max, rounding, flags);
        }
        public void PathRect(Vector2 rect_min, Vector2 rect_max, float rounding)
        {
            ImDrawFlags flags = (ImDrawFlags)0;
            ImGuiNative.ImDrawList_PathRect((ImDrawList*)(NativePtr), rect_min, rect_max, rounding, flags);
        }
        internal void PathRectNative(Vector2 rect_min, Vector2 rect_max, float rounding, ImDrawFlags flags)
        {
            ImGuiNative.ImDrawList_PathRect((ImDrawList*)(NativePtr), rect_min, rect_max, rounding, flags);
        }
        public void PathStroke(uint col)
        {
            ImDrawFlags flags = (ImDrawFlags)0;
            float thickness = 1.0f;
            ImGuiNative.ImDrawList_PathStroke((ImDrawList*)(NativePtr), col, flags, thickness);
        }
        internal void PathStrokeNative(uint col, ImDrawFlags flags)
        {
            float thickness = 1.0f;
            ImGuiNative.ImDrawList_PathStroke((ImDrawList*)(NativePtr), col, flags, thickness);
        }
        internal void PathStrokeNative(uint col, ImDrawFlags flags, float thickness)
        {
            ImGuiNative.ImDrawList_PathStroke((ImDrawList*)(NativePtr), col, flags, thickness);
        }
        public void PopClipRect()
        {
            ImGuiNative.ImDrawList_PopClipRect((ImDrawList*)(NativePtr));
        }
        public void PopTextureID()
        {
            ImGuiNative.ImDrawList_PopTextureID((ImDrawList*)(NativePtr));
        }
        public void PrimQuadUV(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Vector2 uv_a, Vector2 uv_b, Vector2 uv_c, Vector2 uv_d, uint col)
        {
            ImGuiNative.ImDrawList_PrimQuadUV((ImDrawList*)(NativePtr), a, b, c, d, uv_a, uv_b, uv_c, uv_d, col);
        }
        public void PrimRect(Vector2 a, Vector2 b, uint col)
        {
            ImGuiNative.ImDrawList_PrimRect((ImDrawList*)(NativePtr), a, b, col);
        }
        public void PrimRectUV(Vector2 a, Vector2 b, Vector2 uv_a, Vector2 uv_b, uint col)
        {
            ImGuiNative.ImDrawList_PrimRectUV((ImDrawList*)(NativePtr), a, b, uv_a, uv_b, col);
        }
        public void PrimReserve(int idx_count, int vtx_count)
        {
            ImGuiNative.ImDrawList_PrimReserve((ImDrawList*)(NativePtr), idx_count, vtx_count);
        }
        public void PrimUnreserve(int idx_count, int vtx_count)
        {
            ImGuiNative.ImDrawList_PrimUnreserve((ImDrawList*)(NativePtr), idx_count, vtx_count);
        }
        public void PrimVtx(Vector2 pos, Vector2 uv, uint col)
        {
            ImGuiNative.ImDrawList_PrimVtx((ImDrawList*)(NativePtr), pos, uv, col);
        }
        public void PrimWriteIdx(ushort idx)
        {
            ImGuiNative.ImDrawList_PrimWriteIdx((ImDrawList*)(NativePtr), idx);
        }
        public void PrimWriteVtx(Vector2 pos, Vector2 uv, uint col)
        {
            ImGuiNative.ImDrawList_PrimWriteVtx((ImDrawList*)(NativePtr), pos, uv, col);
        }
        internal void AddImageRounded(IntPtr user_texture_id, Vector2 p_min, Vector2 p_max, Vector2 uv_min, Vector2 uv_max, uint col, float rounding, FuDrawFlags flags)
        {
            AddImageRoundedNative(user_texture_id, p_min, p_max, uv_min, uv_max, col, rounding, (ImDrawFlags)flags);
        }

        public void AddPolyline(ref Vector2 points, int num_points, uint col, FuDrawFlags flags, float thickness)
        {
            AddPolylineNative(ref points, num_points, col, (ImDrawFlags)flags, thickness);
        }

        public void AddRect(Vector2 p_min, Vector2 p_max, uint col, float rounding, FuDrawFlags flags)
        {
            AddRectNative(p_min, p_max, col, rounding, (ImDrawFlags)flags);
        }

        public void AddRect(Vector2 p_min, Vector2 p_max, uint col, float rounding, FuDrawFlags flags, float thickness)
        {
            AddRectNative(p_min, p_max, col, rounding, (ImDrawFlags)flags, thickness);
        }

        public void AddRectFilled(Vector2 p_min, Vector2 p_max, uint col, float rounding, FuDrawFlags flags)
        {
            AddRectFilledNative(p_min, p_max, col, rounding, (ImDrawFlags)flags);
        }

        public void AddRectFilled(Vector2 p_min, Vector2 p_max, Vector4 color, float rounding = 0f, FuDrawFlags flags = FuDrawFlags.None)
        {
            AddRectFilledNative(p_min, p_max, ImGui.GetColorU32(color), rounding, (ImDrawFlags)flags);
        }

        public void AddRect(Vector2 p_min, Vector2 p_max, Vector4 color, float rounding = 0f, FuDrawFlags flags = FuDrawFlags.None, float thickness = 1f)
        {
            AddRectNative(p_min, p_max, ImGui.GetColorU32(color), rounding, (ImDrawFlags)flags, thickness);
        }

        public void AddLine(Vector2 p1, Vector2 p2, Vector4 color, float thickness = 1f)
        {
            AddLine(p1, p2, ImGui.GetColorU32(color), thickness);
        }

        public void AddText(Vector2 pos, Vector4 color, string text_begin)
        {
            AddText(pos, ImGui.GetColorU32(color), text_begin);
        }

        public void AddCircleFilled(Vector2 center, float radius, Vector4 color, int num_segments = 0)
        {
            AddCircleFilled(center, radius, ImGui.GetColorU32(color), num_segments);
        }

        public void PathRect(Vector2 rect_min, Vector2 rect_max, float rounding, FuDrawFlags flags)
        {
            PathRectNative(rect_min, rect_max, rounding, (ImDrawFlags)flags);
        }

        public void PathStroke(uint col, FuDrawFlags flags)
        {
            PathStrokeNative(col, (ImDrawFlags)flags);
        }

        public void PathStroke(uint col, FuDrawFlags flags, float thickness)
        {
            PathStrokeNative(col, (ImDrawFlags)flags, thickness);
        }
        public void PushClipRect(Vector2 clip_rect_min, Vector2 clip_rect_max)
        {
            byte intersect_with_current_clip_rect = 0;
            ImGuiNative.ImDrawList_PushClipRect((ImDrawList*)(NativePtr), clip_rect_min, clip_rect_max, intersect_with_current_clip_rect);
        }
        public void PushClipRect(Vector2 clip_rect_min, Vector2 clip_rect_max, bool intersect_with_current_clip_rect)
        {
            byte native_intersect_with_current_clip_rect = intersect_with_current_clip_rect ? (byte)1 : (byte)0;
            ImGuiNative.ImDrawList_PushClipRect((ImDrawList*)(NativePtr), clip_rect_min, clip_rect_max, native_intersect_with_current_clip_rect);
        }
        public void PushClipRectFullScreen()
        {
            ImGuiNative.ImDrawList_PushClipRectFullScreen((ImDrawList*)(NativePtr));
        }
        public void PushTexture(Texture texture)
        {
            PushTextureID(ResolveTextureId(texture));
        }

        internal void PushTextureID(IntPtr texture_id)
        {
            ImGuiNative.ImDrawList_PushTextureID((ImDrawList*)(NativePtr), texture_id);
        }
    }
}