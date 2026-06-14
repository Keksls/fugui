using ImGuiNET;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Fugui color lookup and color stack.
    /// </summary>
    public static partial class Fugui
    {
        private const int ImGuiColorCount = (int)ImGuiCol.COUNT;
        private const int DefaultColorStackCapacity = 64;

        private static FuColorStackEntry[] _colorStack = new FuColorStackEntry[DefaultColorStackCapacity];
        private static int[] _colorStackHeads = new int[0];
        private static int _colorStackCount;

        /// <summary>
        /// Returns the current color, including any Fugui color push currently on the stack.
        /// </summary>
        /// <param name="color">Fugui color enum value.</param>
        /// <returns>Resolved color value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 GetColor(FuColors color)
        {
            return GetColor((int)color);
        }

        /// <summary>
        /// Returns the current color, including any Fugui color push currently on the stack.
        /// </summary>
        /// <param name="color">Raw color index in the current theme color array.</param>
        /// <returns>Resolved color value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 GetColor(int color)
        {
            if (TryGetPushedColor(color, out Vector4 pushedColor))
            {
                return pushedColor;
            }

            return GetThemeColor(color);
        }

        /// <summary>
        /// Returns the current color, including any Fugui color push currently on the stack.
        /// </summary>
        /// <param name="color">Fugui color enum or registered theme extension enum value.</param>
        /// <returns>Resolved color value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 GetColor(Enum color)
        {
            return GetColor(ResolveColorIndex(color));
        }

        /// <summary>
        /// Returns the current color with an alpha multiplier, including any Fugui color push currently on the stack.
        /// </summary>
        /// <param name="color">Fugui color enum value.</param>
        /// <param name="alphaMult">Alpha multiplier.</param>
        /// <returns>Resolved color value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 GetColor(FuColors color, float alphaMult)
        {
            return GetColor((int)color, alphaMult);
        }

        /// <summary>
        /// Returns the current color with an alpha multiplier, including any Fugui color push currently on the stack.
        /// </summary>
        /// <param name="color">Raw color index in the current theme color array.</param>
        /// <param name="alphaMult">Alpha multiplier.</param>
        /// <returns>Resolved color value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 GetColor(int color, float alphaMult)
        {
            Vector4 colorV4 = GetColor(color);
            colorV4.w *= alphaMult;
            return colorV4;
        }

        /// <summary>
        /// Returns the current color with an alpha multiplier, including any Fugui color push currently on the stack.
        /// </summary>
        /// <param name="color">Fugui color enum or registered theme extension enum value.</param>
        /// <param name="alphaMult">Alpha multiplier.</param>
        /// <returns>Resolved color value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 GetColor(Enum color, float alphaMult)
        {
            return GetColor(ResolveColorIndex(color), alphaMult);
        }

        /// <summary>
        /// Returns the current color as U32, including any Fugui color push currently on the stack.
        /// </summary>
        /// <param name="color">Fugui color enum value.</param>
        /// <returns>Resolved color as U32.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetColorU32(FuColors color)
        {
            return GetColorU32((int)color);
        }

        /// <summary>
        /// Returns the current color as U32, including any Fugui color push currently on the stack.
        /// </summary>
        /// <param name="color">Raw color index in the current theme color array.</param>
        /// <returns>Resolved color as U32.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetColorU32(int color)
        {
            return ImGui.GetColorU32(GetColor(color));
        }

        /// <summary>
        /// Returns the current color as U32, including any Fugui color push currently on the stack.
        /// </summary>
        /// <param name="color">Fugui color enum or registered theme extension enum value.</param>
        /// <returns>Resolved color as U32.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetColorU32(Enum color)
        {
            return GetColorU32(ResolveColorIndex(color));
        }

        /// <summary>
        /// Returns the current color as U32 with an alpha multiplier, including any Fugui color push currently on the stack.
        /// </summary>
        /// <param name="color">Fugui color enum value.</param>
        /// <param name="alphaMult">Alpha multiplier.</param>
        /// <returns>Resolved color as U32.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetColorU32(FuColors color, float alphaMult)
        {
            return GetColorU32((int)color, alphaMult);
        }

        /// <summary>
        /// Returns the current color as U32 with an alpha multiplier, including any Fugui color push currently on the stack.
        /// </summary>
        /// <param name="color">Raw color index in the current theme color array.</param>
        /// <param name="alphaMult">Alpha multiplier.</param>
        /// <returns>Resolved color as U32.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetColorU32(int color, float alphaMult)
        {
            return ImGui.GetColorU32(GetColor(color, alphaMult));
        }

        /// <summary>
        /// Returns the current color as U32 with an alpha multiplier, including any Fugui color push currently on the stack.
        /// </summary>
        /// <param name="color">Fugui color enum or registered theme extension enum value.</param>
        /// <param name="alphaMult">Alpha multiplier.</param>
        /// <returns>Resolved color as U32.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetColorU32(Enum color, float alphaMult)
        {
            return GetColorU32(ResolveColorIndex(color), alphaMult);
        }

#if !FUDEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Push(ImGuiCol imCol, Vector4 color)
        {
            PushColorValue((int)imCol, color);
        }

        /// <summary>
        /// Push a color style to Fugui and ImGui when this color exists in ImGui.
        /// </summary>
        /// <param name="imCol">Fugui color to push.</param>
        /// <param name="color">Color value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push(FuColors imCol, Vector4 color)
        {
            PushColorValue((int)imCol, color);
        }

        /// <summary>
        /// Push a color style to Fugui and ImGui when this color exists in ImGui.
        /// </summary>
        /// <param name="colorIndex">Raw color index in the current theme color array.</param>
        /// <param name="color">Color value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push(int colorIndex, Vector4 color)
        {
            PushColorValue(colorIndex, color);
        }

        /// <summary>
        /// Push a color style to Fugui and ImGui when this color exists in ImGui.
        /// </summary>
        /// <param name="color">Fugui color enum or registered theme extension enum value.</param>
        /// <param name="value">Color value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push(Enum color, Vector4 value)
        {
            PushColorValue(ResolveColorIndex(color), value);
        }

        /// <summary>
        /// Pop some colors from the Fugui and ImGui color stacks.
        /// </summary>
        /// <param name="nb">Quantity of colors to pop.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PopColor(int nb = 1)
        {
            if (nb > NbPushColor)
            {
                nb = NbPushColor;
            }

            for (int i = 0; i < nb; i++)
            {
                PopColorValue(out _);
            }
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector4 GetThemeColor(FuColors color)
        {
            return GetThemeColor((int)color);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector4 GetThemeColor(int color)
        {
            FuTheme theme = Themes != null ? Themes.CurrentTheme : null;
            Vector4[] colors = theme != null ? theme.Colors : null;
            if (colors != null && (uint)color < (uint)colors.Length)
            {
                return colors[color];
            }

            return Vector4.zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryGetPushedColor(int color, out Vector4 pushedColor)
        {
            if ((uint)color < (uint)_colorStackHeads.Length)
            {
                int stackIndex = _colorStackHeads[color];
                if (stackIndex >= 0)
                {
                    pushedColor = _colorStack[stackIndex].Color;
                    return true;
                }
            }

            pushedColor = Vector4.zero;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool PushColorValue(int colorIndex, Vector4 color)
        {
            if (!IsValidColorIndex(colorIndex))
            {
                Debug.LogError($"you are trying to push an invalid Fugui color index '{colorIndex}'.");
                return false;
            }

            EnsureColorHeadCapacity(colorIndex);
            EnsureColorStackCapacity();

            bool pushedImGui = colorIndex < ImGuiColorCount;
            int stackIndex = _colorStackCount++;
            _colorStack[stackIndex] = new FuColorStackEntry
            {
                ColorIndex = colorIndex,
                Color = color,
                PreviousHead = _colorStackHeads[colorIndex],
                PushedImGui = pushedImGui
            };
            _colorStackHeads[colorIndex] = stackIndex;

            if (pushedImGui)
            {
                ImGuiNative.igPushStyleColor_Vec4((ImGuiCol)colorIndex, color);
            }

            NbPushColor++;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool PopColorValue(out FuColorStackEntry poppedColor)
        {
            poppedColor = default;
            if (NbPushColor <= 0 || _colorStackCount <= 0)
            {
                return false;
            }

            int stackIndex = --_colorStackCount;
            poppedColor = _colorStack[stackIndex];
            _colorStack[stackIndex] = default;
            if ((uint)poppedColor.ColorIndex < (uint)_colorStackHeads.Length)
            {
                _colorStackHeads[poppedColor.ColorIndex] = poppedColor.PreviousHead;
            }

            if (poppedColor.PushedImGui)
            {
                ImGuiNative.igPopStyleColor(1);
            }

            NbPushColor--;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidColorIndex(int colorIndex)
        {
            return colorIndex >= 0 && colorIndex < GetThemeColorCount();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetThemeColorCount()
        {
            FuTheme theme = Themes != null ? Themes.CurrentTheme : null;
            Vector4[] colors = theme != null ? theme.Colors : null;
            if (colors != null)
            {
                return colors.Length;
            }

            return (int)FuColors.COUNT + FuTheme.ThemeExtensionCount;
        }

        private static int ResolveColorIndex(Enum color)
        {
            if (color == null)
            {
                return -1;
            }

            if (color is FuColors fuColor)
            {
                return (int)fuColor;
            }

            Enum themeExtension = FuTheme.ThemeExtension;
            if (themeExtension != null && color.GetType() == themeExtension.GetType())
            {
                return (int)FuColors.COUNT + Convert.ToInt32(color);
            }

            return Convert.ToInt32(color);
        }

        private static void EnsureColorHeadCapacity(int colorIndex)
        {
            if ((uint)colorIndex < (uint)_colorStackHeads.Length)
            {
                return;
            }

            int oldLength = _colorStackHeads.Length;
            int newLength = Math.Max(colorIndex + 1, Math.Max(GetThemeColorCount(), (int)FuColors.COUNT));
            int[] heads = new int[newLength];
            for (int i = 0; i < oldLength; i++)
            {
                heads[i] = _colorStackHeads[i];
            }
            for (int i = oldLength; i < newLength; i++)
            {
                heads[i] = -1;
            }
            _colorStackHeads = heads;
        }

        private static void EnsureColorStackCapacity()
        {
            if (_colorStackCount < _colorStack.Length)
            {
                return;
            }

            Array.Resize(ref _colorStack, _colorStack.Length * 2);
        }

        private struct FuColorStackEntry
        {
            public int ColorIndex;
            public Vector4 Color;
            public int PreviousHead;
            public bool PushedImGui;
        }
    }
}
