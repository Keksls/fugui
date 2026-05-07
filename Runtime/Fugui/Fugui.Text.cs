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
    /// Fugui text measurement and string helpers.
    /// </summary>
    public static partial class Fugui
    {
        /// <summary>
        /// Get text size according to it's wrapping behaviour
        /// </summary>
        /// <param name="text">text to get size of</param>
        /// <returns>Size of the text</returns>
        public static Vector2 CalcTextSize(string text)
        {
            return CalcTextSize(text, FuTextWrapping.None);
        }

        /// <summary>
        /// Get text size according to it's wrapping behaviour
        /// </summary>
        /// <param name="text">text to get size of</param>
        /// <param name="wrapping">however the text need to be wrapped</param>
        /// <returns>Size of the text</returns>
        public static Vector2 CalcTextSize(string text, FuTextWrapping wrapping)
        {
            return CalcTextSize(text, wrapping, Vector2.zero);
        }

        /// <summary>
        /// Get text size according to it's wrapping behaviour
        /// </summary>
        /// <param name="text">text to get size of</param>
        /// <param name="wrapping">however the text need to be wrapped</param>
        /// <param name="maxSize">maximum size (for clipping or wrapping). Keep Vector2.zero to use maximum available region</param>
        /// <returns>Size of the text</returns>
        public static Vector2 CalcTextSize(string text, FuTextWrapping wrapping, Vector2 maxSize)
        {
            if ((text.Length == 1 || Fugui.GetUntagedText(text).Length == 1) && Fugui.IsDuoToneChar(text[0]))
            {
                // get secondaty char
                char secondary = (char)(((ushort)text[0]) + 1);
                // get both char sized
                Vector2 primarySize = ImGui.CalcTextSize(text[0].ToString());
                Vector2 secondarySize = ImGui.CalcTextSize(secondary.ToString());
                // get full icon size
                return new Vector2(Mathf.Max(primarySize.x, secondarySize.x), Mathf.Max(primarySize.y, secondarySize.y));
            }

            Vector2 textSize;
            switch (wrapping)
            {
                default:
                case FuTextWrapping.None:
                    textSize = ImGui.CalcTextSize(text, true);
                    break;

                case FuTextWrapping.Clip:
                    textSize = ImGui.CalcTextSize(text, true);
                    textSize.x = Mathf.Min(textSize.x, maxSize.x == 0f ? ImGui.GetContentRegionAvail().x : maxSize.x);
                    break;

                case FuTextWrapping.Wrap:
                    textSize = ImGui.CalcTextSize(text, true, maxSize.x == -1f ? ImGui.GetContentRegionAvail().x : maxSize.x);
                    if (maxSize.y > 0f && textSize.y > maxSize.y)
                    {
                        textSize.y = maxSize.y;
                    }
                    break;
            }
            return textSize;
        }

        public static float GetTextLineHeight()
        {
            return ImGui.GetTextLineHeight();
        }

        public static float GetTextLineHeightWithSpacing()
        {
            return ImGui.GetTextLineHeightWithSpacing();
        }

        public static float GetFrameHeight()
        {
            return ImGui.GetFrameHeight();
        }

        static Dictionary<string, string> _niceStrings = new Dictionary<string, string>();

        /// <summary>
        /// Returns the add spaces before uppercase result.
        /// </summary>
        /// <param name="input">The input value.</param>
        /// <returns>The result of the operation.</returns>
        public static string AddSpacesBeforeUppercase(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            if (!_niceStrings.TryGetValue(input, out string niceString))
            {
                // Use a regular expression to add spaces before uppercase letters, but ignore the first letter of the string and avoid adding a space if it is preceded by whitespace
                niceString = AddSpacesBeforeUppercaseDirect(input);
                _niceStrings.Add(input, niceString);
            }
            return niceString;
        }

        /// <summary>
        /// Adds spaces before uppercase letters in the input string. return the value directly without saving it
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>The input string with spaces added before uppercase letters.</returns>
        public static string AddSpacesBeforeUppercaseDirect(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            return Regex.Replace(input, @"(?<=[a-z])(?=[A-Z])", " ");
        }

        private static Dictionary<string, string> _untagedStrings = new Dictionary<string, string>();

        /// <summary>
        /// Get a text without tag "##xxxxxx"
        /// </summary>
        /// <param name="input">taged text</param>
        /// <returns>untaged text</returns>
        public static string GetUntagedText(string input)
        {
            if (!_untagedStrings.TryGetValue(input, out string untagedString))
            {
                int tagIndex = input.IndexOf("##", StringComparison.Ordinal);
                untagedString = tagIndex >= 0 ? input.Substring(0, tagIndex) : input;
                _untagedStrings.Add(input, untagedString);
            }
            return untagedString;
        }

        /// <summary>
        /// Check if input string contains only alphanumeric characters and spaces.
        /// Spaces are allowed only if they are followed by an alphanumeric character
        /// </summary>
        /// <param name="input">The input string</param>
        /// <returns>True if input contains only alphanumeric characters and spaces, false otherwise</returns>
        public static bool IsAlphaNumericWithSpaces(string input)
        {
            return Regex.IsMatch(input, @"^[a-zA-Z0-9]+(\s[a-zA-Z0-9]+)*$");
        }

        /// <summary>
        /// Replaces spaces followed by an alphanumeric character with the same alphanumeric character capitalized
        /// </summary>
        /// <param name="input">The input string</param>
        /// <returns>The modified string</returns>
        public static string RemoveSpaceAndCapitalize(string input)
        {
            return Regex.Replace(input, @"\s([a-zA-Z0-9])", x => x.Groups[1].Value.ToUpper());
        }
    }
}
