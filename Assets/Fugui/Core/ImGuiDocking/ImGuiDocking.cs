using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace ImGuiNET
{
    public unsafe static class ImGuiDocking
    {
        public static uint DockBuilderAddNode()
        {
            uint node_id = 0;
            ImGuiDockNodeFlags flags = (ImGuiDockNodeFlags)0;
            uint ret = NativeDocking.igDockBuilderAddNode(node_id, flags);
            return ret;
        }

        public static uint DockBuilderAddNode(uint node_id)
        {
            ImGuiDockNodeFlags flags = (ImGuiDockNodeFlags)0;
            uint ret = NativeDocking.igDockBuilderAddNode(node_id, flags);
            return ret;
        }

        public static uint DockBuilderAddNode(uint node_id, ImGuiDockNodeFlags flags)
        {
            uint ret = NativeDocking.igDockBuilderAddNode(node_id, flags);
            return ret;
        }

        public static void DockBuilderCopyDockSpace(uint src_dockspace_id, uint dst_dockspace_id, ref ImVector in_window_remap_pairs)
        {
            fixed (ImVector* native_in_window_remap_pairs = &in_window_remap_pairs)
            {
                NativeDocking.igDockBuilderCopyDockSpace(src_dockspace_id, dst_dockspace_id, native_in_window_remap_pairs);
            }
        }

        public static void DockBuilderCopyNode(uint src_node_id, uint dst_node_id, out ImVector out_node_remap_pairs)
        {
            fixed (ImVector* native_out_node_remap_pairs = &out_node_remap_pairs)
            {
                NativeDocking.igDockBuilderCopyNode(src_node_id, dst_node_id, native_out_node_remap_pairs);
            }
        }

        public static void DockBuilderCopyWindowSettings(string src_name, string dst_name)
        {
            byte* native_src_name;
            int src_name_byteCount = 0;
            if (src_name != null)
            {
                src_name_byteCount = Encoding.UTF8.GetByteCount(src_name);
                if (src_name_byteCount > Util.StackAllocationSizeLimit)
                {
                    native_src_name = Util.Allocate(src_name_byteCount + 1);
                }
                else
                {
                    byte* native_src_name_stackBytes = stackalloc byte[src_name_byteCount + 1];
                    native_src_name = native_src_name_stackBytes;
                }
                int native_src_name_offset = Util.GetUtf8(src_name, native_src_name, src_name_byteCount);
                native_src_name[native_src_name_offset] = 0;
            }
            else { native_src_name = null; }
            byte* native_dst_name;
            int dst_name_byteCount = 0;
            if (dst_name != null)
            {
                dst_name_byteCount = Encoding.UTF8.GetByteCount(dst_name);
                if (dst_name_byteCount > Util.StackAllocationSizeLimit)
                {
                    native_dst_name = Util.Allocate(dst_name_byteCount + 1);
                }
                else
                {
                    byte* native_dst_name_stackBytes = stackalloc byte[dst_name_byteCount + 1];
                    native_dst_name = native_dst_name_stackBytes;
                }
                int native_dst_name_offset = Util.GetUtf8(dst_name, native_dst_name, dst_name_byteCount);
                native_dst_name[native_dst_name_offset] = 0;
            }
            else { native_dst_name = null; }
            NativeDocking.igDockBuilderCopyWindowSettings(native_src_name, native_dst_name);
            if (src_name_byteCount > Util.StackAllocationSizeLimit)
            {
                Util.Free(native_src_name);
            }
            if (dst_name_byteCount > Util.StackAllocationSizeLimit)
            {
                Util.Free(native_dst_name);
            }
        }

        public static void DockBuilderDockWindow(string window_name, uint node_id)
        {
            byte* native_window_name;
            int window_name_byteCount = 0;
            if (window_name != null)
            {
                window_name_byteCount = Encoding.UTF8.GetByteCount(window_name);
                if (window_name_byteCount > Util.StackAllocationSizeLimit)
                {
                    native_window_name = Util.Allocate(window_name_byteCount + 1);
                }
                else
                {
                    byte* native_window_name_stackBytes = stackalloc byte[window_name_byteCount + 1];
                    native_window_name = native_window_name_stackBytes;
                }
                int native_window_name_offset = Util.GetUtf8(window_name, native_window_name, window_name_byteCount);
                native_window_name[native_window_name_offset] = 0;
            }
            else { native_window_name = null; }
            NativeDocking.igDockBuilderDockWindow(native_window_name, node_id);
            if (window_name_byteCount > Util.StackAllocationSizeLimit)
            {
                Util.Free(native_window_name);
            }
        }

        public static void DockBuilderFinish(uint node_id)
        {
            NativeDocking.igDockBuilderFinish(node_id);
        }

        public static void DockBuilderRemoveNode(uint node_id)
        {
            NativeDocking.igDockBuilderRemoveNode(node_id);
        }

        public static void DockBuilderRemoveNodeChildNodes(uint node_id)
        {
            NativeDocking.igDockBuilderRemoveNodeChildNodes(node_id);
        }

        public static void DockBuilderRemoveNodeDockedWindows(uint node_id)
        {
            byte clear_settings_refs = 1;
            NativeDocking.igDockBuilderRemoveNodeDockedWindows(node_id, clear_settings_refs);
        }

        public static void DockBuilderRemoveNodeDockedWindows(uint node_id, bool clear_settings_refs)
        {
            byte native_clear_settings_refs = clear_settings_refs ? (byte)1 : (byte)0;
            NativeDocking.igDockBuilderRemoveNodeDockedWindows(node_id, native_clear_settings_refs);
        }

        public static void DockBuilderSetNodePos(uint node_id, Vector2 pos)
        {
            NativeDocking.igDockBuilderSetNodePos(node_id, pos);
        }

        public static void DockBuilderSetNodeSize(uint node_id, Vector2 size)
        {
            NativeDocking.igDockBuilderSetNodeSize(node_id, size);
        }

        public static uint DockBuilderSplitNode(uint node_id, ImGuiDir split_dir, float size_ratio_for_node_at_dir, out uint out_id_at_dir, out uint out_id_at_opposite_dir)
        {
            fixed (uint* native_out_id_at_dir = &out_id_at_dir)
            {
                fixed (uint* native_out_id_at_opposite_dir = &out_id_at_opposite_dir)
                {
                    uint ret = NativeDocking.igDockBuilderSplitNode(node_id, split_dir, size_ratio_for_node_at_dir, native_out_id_at_dir, native_out_id_at_opposite_dir);
                    return ret;
                }
            }
        }

        public static ImGuiDockNodePtr DockBuilderGetCentralNode(uint node_id)
        {
            ImGuiDockNode* ret = NativeDocking.igDockBuilderGetCentralNode(node_id);
            return new ImGuiDockNodePtr(ret);
        }

        public static ImGuiDockNodePtr DockBuilderGetNode(uint node_id)
        {
            ImGuiDockNode* ret = NativeDocking.igDockBuilderGetNode(node_id);
            return new ImGuiDockNodePtr(ret);
        }
    }

    internal static unsafe class Util
    {
        internal const int StackAllocationSizeLimit = 2048;

        public static string StringFromPtr(byte* ptr)
        {
            int characters = 0;
            while (ptr[characters] != 0)
            {
                characters++;
            }

            return Encoding.UTF8.GetString(ptr, characters);
        }

        internal static bool AreStringsEqual(byte* a, int aLength, byte* b)
        {
            for (int i = 0; i < aLength; i++)
            {
                if (a[i] != b[i]) { return false; }
            }

            if (b[aLength] != 0) { return false; }

            return true;
        }

        internal static byte* Allocate(int byteCount) => (byte*)Marshal.AllocHGlobal(byteCount);
        internal static void Free(byte* ptr) => Marshal.FreeHGlobal((IntPtr)ptr);
        internal static int GetUtf8(string s, byte* utf8Bytes, int utf8ByteCount)
        {
            fixed (char* utf16Ptr = s)
            {
                return Encoding.UTF8.GetBytes(utf16Ptr, s.Length, utf8Bytes, utf8ByteCount);
            }
        }
    }
}