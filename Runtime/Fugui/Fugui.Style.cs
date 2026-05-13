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
    /// Fugui style and font stacks.
    /// </summary>
    public static partial class Fugui
    {
#if !FUDEBUG

        /// <summary>
        /// Push a color style to ImGui color stack
        /// </summary>
        /// <param name="imCol">ImGui color to push</param>
        /// <param name="color">colot value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push(ImGuiCol imCol, Vector4 color)
        {
            ImGuiNative.igPushStyleColor_Vec4(imCol, color);
            NbPushColor++;
        }

        /// <summary>
        /// Push a color style to ImGui color stack
        /// </summary>
        /// <param name="imCol">ImGui color to push</param>
        /// <param name="color">colot value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push(FuColors imCol, Vector4 color)
        {
            if ((int)imCol >= (int)ImGuiCol.COUNT)
            {
                Debug.LogError("You are trying to push a color that is not in ImGuiCol enum, use ImGuiCol instead.");
                return;
            }

            ImGuiNative.igPushStyleColor_Vec4((ImGuiCol)imCol, color);
            NbPushColor++;
        }

        /// <summary>
        /// Push a style var to ImGui style var stack
        /// </summary>
        /// <param name="imVar">ImGUi style var to push</param>
        /// <param name="value">style var value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push(ImGuiStyleVar imVar, Vector2 value)
        {
            ImGuiNative.igPushStyleVar_Vec2(imVar, value * CurrentContext.Scale);
            NbPushStyle++;
        }

        /// <summary>
        /// Push a style var to ImGui style var stack
        /// </summary>
        /// <param name="imVar">ImGUi style var to push</param>
        /// <param name="value">style var value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push(ImGuiStyleVar imVar, float value)
        {
            ImGuiNative.igPushStyleVar_Float(imVar, value * CurrentContext.Scale);
            NbPushStyle++;
        }

        /// <summary>
        /// Pop some colors from ImGui color stack
        /// </summary>
        /// <param name="nb">quantity of color to pop</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PopColor(int nb = 1)
        {
            if (nb > NbPushColor)
            {
                nb = NbPushColor;
            }
            if (NbPushColor > 0)
            {
                ImGuiNative.igPopStyleColor(nb);
                NbPushColor -= nb;
            }
        }

        /// <summary>
        /// Pop some style var from ImGui style stack
        /// </summary>
        /// <param name="nb">quantity of style var to pop</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PopStyle(int nb = 1)
        {
            if (nb > NbPushStyle)
            {
                nb = NbPushStyle;
            }
            if (NbPushStyle > 0)
            {
                ImGuiNative.igPopStyleVar(nb);
                NbPushStyle -= nb;
            }
        }
#endif

        /// <summary>
        /// Push the current font
        /// </summary>
        /// <param name="size">size of the font</param>
        /// <param name="type">type of the font</param>
        public static void PushFont(int size, FontType type = FontType.Regular)
        {
            PushFont(null, size, type);
        }

        /// <summary>
        /// Push the named font.
        /// </summary>
        /// <param name="fontName">name of the font configured in FontConfig</param>
        /// <param name="size">size of the font</param>
        /// <param name="type">type of the font</param>
        public static void PushFont(string fontName, int size, FontType type = FontType.Regular)
        {
            FontSet fontSet = ResolveFontSet(fontName, size);
            if (fontSet == null)
            {
                return;
            }

            ImFontPtr fontPtr = fontSet.GetFont(type);
            if (fontPtr.Equals(default(ImFontPtr)))
            {
                Debug.LogError($"you are trying to push font '{fontSet.Name}' for {fontSet.Size}px as {type}, but this style does not exist.");
                return;
            }

            ImGui.PushFont(fontPtr);
            NbPushFont++;
        }

        /// <summary>
        /// Push the named font.
        /// </summary>
        /// <param name="size">size of the font</param>
        /// <param name="fontName">name of the font configured in FontConfig</param>
        /// <param name="type">type of the font</param>
        public static void PushFont(int size, string fontName, FontType type = FontType.Regular)
        {
            PushFont(fontName, size, type);
        }

        /// <summary>
        /// Push the named font at the current font size.
        /// </summary>
        /// <param name="fontName">name of the font configured in FontConfig</param>
        /// <param name="type">type of the font</param>
        public static void PushFont(string fontName, FontType type)
        {
            PushFont(fontName, GetFontSize(), type);
        }

        /// <summary>
        /// Push the named regular font at the current font size.
        /// </summary>
        /// <param name="fontName">name of the font configured in FontConfig</param>
        public static void PushFont(string fontName)
        {
            PushFont(fontName, GetFontSize(), FontType.Regular);
        }

        /// <summary>
        /// Push the current font type
        /// </summary>
        /// <param name="type">type of the font</param>
        public static void PushFont(FontType type)
        {
            PushFont(GetFontSize(), type);
        }

        /// <summary>
        /// Get the current font size
        /// </summary>
        /// <returns> size of the current font</returns>
        public static int GetFontSize()
        {
            float fontScale = CurrentContext != null ? CurrentContext.FontScale : 1f;
            if (fontScale <= 0f)
            {
                fontScale = 1f;
            }

            return Mathf.RoundToInt(ImGuiNative.igGetFontSize() / fontScale);
        }

        /// <summary>
        /// Pop the current font
        /// </summary>
        public static void PopFont()
        {
            if (NbPushFont > 0)
            {
                ImGui.PopFont();
                NbPushFont--;
            }
        }

        /// <summary>
        /// Pop the n current fonts
        /// </summary>
        /// <param name="nbPop">number of fonts to pop</param>
        public static void PopFont(int nbPop)
        {
            for (int i = 0; i < nbPop; i++)
            {
                PopFont();
            }
        }

        /// <summary>
        /// Push the defaut font to current
        /// </summary>
        public static void PushDefaultFont()
        {
            FontSet fontSet = CurrentContext?.GetFallbackFontSet();
            if (fontSet == null)
            {
                Debug.LogError("you are trying to push the default font but no font is loaded.");
                return;
            }

            ImFontPtr fontPtr = fontSet.GetFont(FontType.Regular);
            if (fontPtr.Equals(default(ImFontPtr)))
            {
                Debug.LogError($"you are trying to push default font '{fontSet.Name}' for {fontSet.Size}px, but its regular style does not exist.");
                return;
            }

            ImGui.PushFont(fontPtr);
            NbPushFont++;
        }

        private static FontSet ResolveFontSet(string fontName, int size)
        {
            if (CurrentContext == null)
            {
                Debug.LogError("you are trying to push a font but there is no current Fugui context.");
                return null;
            }

            string resolvedFontName = CurrentContext.ResolveFontName(fontName);
            if (CurrentContext.TryGetFontSet(resolvedFontName, size, out FontSet fontSet))
            {
                return fontSet;
            }

            Debug.LogError($"you are trying to push font '{resolvedFontName}' for {size}px but it does not exist.");
            return CurrentContext.GetFallbackFontSet();
        }
    }
}
