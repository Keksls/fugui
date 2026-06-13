// define it to debug whatever Color or Styles are pushed (avoid stack leak metrics)
// it's ressourcefull, si comment it when debug is done. Ensure it's commented before build.
//#define FUDEBUG
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
#endif
using Fu.Framework;
using ImGuiNET;
#if FU_EXTERNALIZATION
#endif
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Fugui cursor layout helpers.
    /// </summary>
    public static partial class Fugui
    {
        private enum FuCursorPositionMode
        {
            Local,
            Screen
        }

        private readonly struct FuCursorPositionScope
        {
            public readonly Vector2 Position;
            public readonly FuCursorPositionMode Mode;

            public FuCursorPositionScope(Vector2 position, FuCursorPositionMode mode)
            {
                Position = position;
                Mode = mode;
            }
        }

        private static readonly Stack<FuCursorPositionScope> _cursorPositionStack = new Stack<FuCursorPositionScope>();

        internal static void ClearCursorPositionStack(string reason)
        {
            if (_cursorPositionStack.Count == 0)
            {
                return;
            }

            Debug.LogWarning($"Fugui cursor position stack had {_cursorPositionStack.Count} unbalanced PushPos/PushScreenPos call(s); clearing it {reason}.");
            _cursorPositionStack.Clear();
        }


        /// <summary>
        /// Pushes the current cursor position and moves it to a local Fugui position.
        /// </summary>
        /// <param name="position">Local position in Fugui units.</param>
        public static void PushPos(Vector2 position)
        {
            _cursorPositionStack.Push(new FuCursorPositionScope(ImGui.GetCursorPos(), FuCursorPositionMode.Local));
            ImGuiNative.igSetCursorPos(position * Scale);
        }

        /// <summary>
        /// Pushes the current cursor position and moves it to a local Fugui position.
        /// </summary>
        /// <param name="x">Local X position in Fugui units.</param>
        /// <param name="y">Local Y position in Fugui units.</param>
        public static void PushPos(float x, float y)
        {
            PushPos(new Vector2(x, y));
        }

        /// <summary>
        /// Pushes the current cursor position and moves it to an absolute screen position.
        /// </summary>
        /// <param name="position">Absolute screen position in pixels.</param>
        public static void PushScreenPos(Vector2 position)
        {
            _cursorPositionStack.Push(new FuCursorPositionScope(ImGui.GetCursorScreenPos(), FuCursorPositionMode.Screen));
            ImGuiNative.igSetCursorScreenPos(position);
        }

        /// <summary>
        /// Pushes the current cursor position and moves it to an absolute screen position.
        /// </summary>
        /// <param name="x">Absolute screen X position in pixels.</param>
        /// <param name="y">Absolute screen Y position in pixels.</param>
        public static void PushScreenPos(float x, float y)
        {
            PushScreenPos(new Vector2(x, y));
        }

        /// <summary>
        /// Restores the cursor position saved by PushPos or PushScreenPos.
        /// </summary>
        public static void PopPos()
        {
            if (_cursorPositionStack.Count == 0)
            {
                Debug.LogWarning("Fugui.PopPos called with an empty position stack.");
                return;
            }

            FuCursorPositionScope scope = _cursorPositionStack.Pop();
            if (scope.Mode == FuCursorPositionMode.Screen)
            {
                ImGui.SetCursorScreenPos(scope.Position);
                return;
            }

            ImGui.SetCursorPos(scope.Position);
        }

        /// <summary>
        /// Restores the cursor position saved by PushScreenPos.
        /// </summary>
        public static void PopScreenPos()
        {
            PopPos();
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
