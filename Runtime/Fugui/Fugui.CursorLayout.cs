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
    /// Fugui cursor layout helpers.
    /// </summary>
    public static partial class Fugui
    {
        /// <summary>
        /// Gets the current cursor position in screen coordinates.
        /// </summary>
        public static Vector2 GetCursorScreenPos()
        {
            return ImGui.GetCursorScreenPos();
        }

        /// <summary>
        /// Sets the current cursor position in screen coordinates.
        /// </summary>
        public static void SetCursorScreenPos(Vector2 pos)
        {
            ImGui.SetCursorScreenPos(pos);
        }

        /// <summary>
        /// Gets the current cursor position in local window coordinates.
        /// </summary>
        public static Vector2 GetCursorPos()
        {
            return ImGui.GetCursorPos();
        }

        /// <summary>
        /// Sets the current cursor position in local window coordinates.
        /// </summary>
        public static void SetCursorPos(Vector2 pos)
        {
            ImGui.SetCursorPos(pos);
        }

        /// <summary>
        /// Gets the remaining content region available in the current window.
        /// </summary>
        public static Vector2 GetContentRegionAvail()
        {
            return ImGui.GetContentRegionAvail();
        }

        /// <summary>
        /// Reserve raw screen-space size in the current Fugui frame.
        /// </summary>
        public static void Dummy(Vector2 size)
        {
            ImGuiNative.igDummy(size);
        }

        /// <summary>
        /// Continue drawing on the same line.
        /// </summary>
        public static void SameLine()
        {
            ImGui.SameLine();
        }

        /// <summary>
        /// Draw an invisible interaction zone.
        /// </summary>
        public static bool InvisibleButton(string id, Vector2 size)
        {
            return ImGui.InvisibleButton(id, size);
        }

        /// <summary>
        /// Return whether the last submitted item is hovered.
        /// </summary>
        public static bool IsItemHovered()
        {
            return ImGui.IsItemHovered();
        }

        /// <summary>
        /// Return whether the last submitted item is active.
        /// </summary>
        public static bool IsItemActive()
        {
            return ImGui.IsItemActive();
        }

        /// <summary>
        /// Return whether the last submitted item was clicked with the given button.
        /// </summary>
        public static bool IsItemClicked(FuMouseButton mouseButton)
        {
            if (mouseButton == FuMouseButton.None)
            {
                return false;
            }
            return ImGui.IsItemClicked((ImGuiMouseButton)mouseButton);
        }

        /// <summary>
        /// Return whether the mouse is hovering the given screen-space rectangle.
        /// </summary>
        public static bool IsMouseHoveringRect(Vector2 min, Vector2 max)
        {
            return ImGui.IsMouseHoveringRect(min, max);
        }

        /// <summary>
        /// Scroll the current child/window to the current item position.
        /// </summary>
        public static void SetScrollHereY(float centerYRatio = 0.5f)
        {
            ImGui.SetScrollHereY(centerYRatio);
        }

        /// <summary>
        /// Move the current drawing X position of strenght
        /// </summary>
        /// <param name="strenght">quantity of pixels to move on X from here</param>
        /// <param name="negValueUseMaxRect">if strenght is negative use max rect width to calculate the position</param>
        public static void MoveX(float strenght, bool negValueUseMaxRect = false)
        {
            if(strenght == 0) return;
            if (strenght < 0)
            {
                MoveXUnscaled(strenght * Scale, negValueUseMaxRect);
                return;
            }
            ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().x + strenght * Scale, ImGui.GetCursorScreenPos().y));
        }

        /// <summary>
        /// Move the current drawing Y position of strenght
        /// </summary>
        /// <param name="strenght">quantity of pixels to move on Y from here</param>
        /// <param name="negValueUseMaxRect">if strenght is negative use max rect width to calculate the position</param>
        public static void MoveY(float strenght, bool negValueUseMaxRect = false)
        {
            if (strenght == 0) return;
            if (strenght < 0)
            {
                MoveYUnscaled(strenght * Scale, negValueUseMaxRect);
                return;
            }
            ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().x, ImGui.GetCursorScreenPos().y + strenght * Scale));
        }

        /// <summary>
        /// Move the current drawing X position of strenght
        /// </summary>
        /// <param name="strenght">quantity of pixels to move on X from here</param>
        /// <param name="negValueUseMaxRect">if strenght is negative use max rect width to calculate the position</param>
        public static void MoveXUnscaled(float strenght, bool negValueUseMaxRect = false)
        {
            if (strenght == 0) return;
            if (strenght < 0 && negValueUseMaxRect)
            {
                strenght = ImGui.GetContentRegionAvail().x + strenght;
            }
            ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().x + strenght, ImGui.GetCursorScreenPos().y));
        }

        /// <summary>
        /// Move the current drawing Y position of strenght
        /// </summary>
        /// <param name="strenght">quantity of pixels to move on Y from here</param>
        /// <param name="negValueUseMaxRect">if strenght is negative, use max rect available + strenght</param>
        public static void MoveYUnscaled(float strenght, bool negValueUseMaxRect = false)
        {
            if (strenght == 0) return;
            if (strenght < 0 && negValueUseMaxRect)
            {
                strenght = ImGui.GetContentRegionAvail().y + strenght;
            }
            ImGui.SetCursorScreenPos(new Vector2(ImGui.GetCursorScreenPos().x, ImGui.GetCursorScreenPos().y + strenght));
        }

        /// <summary>
        /// Move the current drawing X position to new local pos
        /// </summary>
        /// <param name="x">Local X position</param>
        public static void SetLocalX(float x)
        {
            ImGui.SetCursorPosX(x * Scale);
        }

        /// <summary>
        /// Move the current drawing Y position to new local pos
        /// </summary>
        /// <param name="y">Local Y position</param>
        public static void SetLocalY(float y)
        {
            ImGui.SetCursorPosY(y * Scale);
        }

        /// <summary>
        /// Align next element Horizontaly
        /// </summary>
        /// <param name="nextElementWidth">width of the next element</param>
        /// <param name="alignement">alignement type</param>
        public static void HorizontalAlignNextElement(float nextElementWidth, FuElementAlignement alignement)
        {
            switch (alignement)
            {
                case FuElementAlignement.Center:
                    float pad = ImGui.GetContentRegionAvail().x / 2f - nextElementWidth / 2f;
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + pad);
                    break;

                case FuElementAlignement.Right:
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().x - nextElementWidth);
                    break;
            }
        }
    }
}
