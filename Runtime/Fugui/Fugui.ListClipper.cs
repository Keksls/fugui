// define it to debug whatever Color or Styles are pushed (avoid stack leak metrics)
// it's ressourcefull, si comment it when debug is done. Ensure it's commented before build.
//#define FUDEBUG
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
#endif
using Fu.Framework;
using ImGuiNET;
#if FU_EXTERNALIZATION
using SDL2;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;

namespace Fu
{
    /// <summary>
    /// Fugui ImGui list clipper helpers.
    /// </summary>
    public static partial class Fugui
    {
        private static Dictionary<int, ImGuiListClipperPtr> _clippers = new Dictionary<int, ImGuiListClipperPtr>();

        /// <summary>
        /// Beggin a list clipper. Use it to help drawing only visible items of a list (items need to have fixed height)
        /// </summary>
        /// <param name="count">number of items</param>
        /// <param name="itemHeight">height of an item</param>
        public static unsafe void ListClipperBegin(int count = -1, float itemHeight = -1f)
        {
            if (count <= 0)
                count = 1;
            if (itemHeight <= 0f)
                itemHeight = 1f;

            int ctxId = CurrentContext != null ? CurrentContext.ID : 0;
            if (!_clippers.ContainsKey(ctxId))
            {
                _clippers.Add(ctxId, new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper()));
            }

            var _clipper = _clippers[ctxId];
            ImGuiNative.ImGuiListClipper_Begin(_clipper.NativePtr, count, itemHeight);
        }

        /// <summary>
        /// End a list clipper
        /// </summary>
        public static unsafe void ListClipperEnd()
        {
            int ctxId = CurrentContext != null ? CurrentContext.ID : 0;
            if (!_clippers.ContainsKey(ctxId))
            {
                _clippers.Add(ctxId, new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper()));
            }

            var _clipper = _clippers[ctxId];
            ImGuiNative.ImGuiListClipper_End(_clipper.NativePtr);
        }

        /// <summary>
        /// do one step inside the list clipper. should be called like while(Step())
        /// </summary>
        /// <returns>true if step success</returns>
        public static unsafe bool ListClipperStep()
        {
            int ctxId = CurrentContext != null ? CurrentContext.ID : 0;
            if (!_clippers.ContainsKey(ctxId))
            {
                _clippers.Add(ctxId, new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper()));
            }

            var _clipper = _clippers[ctxId];
            return ImGuiNative.ImGuiListClipper_Step(_clipper.NativePtr) == 1;
        }

        /// <summary>
        /// Get the index of the first list item to draw
        /// </summary>
        /// <returns>index of the item</returns>
        public static unsafe int ListClipperDisplayStart()
        {
            int ctxId = CurrentContext != null ? CurrentContext.ID : 0;
            if (!_clippers.ContainsKey(ctxId))
            {
                _clippers.Add(ctxId, new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper()));
            }

            var _clipper = _clippers[ctxId];
            return _clipper.DisplayStart;
        }

        /// <summary>
        /// Get the index of the last list item to draw
        /// </summary>
        /// <returns>index of the item</returns>
        public static unsafe int ListClipperDisplayEnd()
        {
            int ctxId = CurrentContext != null ? CurrentContext.ID : 0;
            if (!_clippers.ContainsKey(ctxId))
            {
                _clippers.Add(ctxId, new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper()));
            }

            var _clipper = _clippers[ctxId];
            return _clipper.DisplayEnd;
        }
    }
}
