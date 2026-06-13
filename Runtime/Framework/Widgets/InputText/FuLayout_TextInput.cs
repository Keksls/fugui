using ImGuiNET;
using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Represents the Fu Layout type.
    /// </summary>
    public unsafe partial class FuLayout
    {
        private static readonly ImGuiInputTextCallback _textInputCallback = TextInputCallback;

        #region Methods
        /// <summary>
        /// Displays a single-line text input field
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <param name="style">the Frame Style to use</param>
        /// <param name="width">The width of the input field</param>
        /// <param name="flags">Flag for custom InputText Behaviour</param>
        /// <returns>true if value has just been edited</returns>
        public bool TextInput(string id, ref string text, FuFrameStyle style, float width = 0f, FuInputTextFlags flags = FuInputTextFlags.Default)
        {
            return TextInput(id, "", ref text, 2048, 0f, style, width, flags);
        }

        /// <summary>
        /// Displays a single-line text input field.
        /// </summary>
        public bool TextInput(string id, ref string text, FuFrameStyle style, float width, FuTextInputOptions options)
        {
            return TextInput(id, "", ref text, 2048, 0f, style, width, options);
        }

        /// <summary>
        /// Displays a single-line text input field
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <param name="width">The width of the input field</param>
        /// <param name="flags">Flag for custom InputText Behaviour</param>
        /// <returns>true if value has just been edited</returns>
        public bool TextInput(string id, ref string text, float width = 0f, FuInputTextFlags flags = FuInputTextFlags.Default)
        {
            return TextInput(id, "", ref text, 2048, 0f, FuFrameStyle.Default, width, flags);
        }

        /// <summary>
        /// Displays a single-line text input field.
        /// </summary>
        public bool TextInput(string id, ref string text, float width, FuTextInputOptions options)
        {
            return TextInput(id, "", ref text, 2048, 0f, FuFrameStyle.Default, width, options);
        }

        /// <summary>
        /// Displays a single-line text input field with a hint
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="hint">The hint that will be displayed when the field is empty</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <param name="width">The width of the input field</param>
        /// <param name="flags">Flag for custom InputText Behaviour</param>
        /// <returns>true if value has just been edited</returns>
        public bool TextInput(string id, string hint, ref string text, float width = 0f, FuInputTextFlags flags = FuInputTextFlags.Default)
        {
            return TextInput(id, hint, ref text, 2048, 0f, FuFrameStyle.Default, width, flags);
        }

        /// <summary>
        /// Displays a single-line text input field with a hint.
        /// </summary>
        public bool TextInput(string id, string hint, ref string text, float width, FuTextInputOptions options)
        {
            return TextInput(id, hint, ref text, 2048, 0f, FuFrameStyle.Default, width, options);
        }

        /// <summary>
        /// Displays a single-line text input field with a hint
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="hint">The hint that will be displayed when the field is empty</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <param name="size">the maximum size of the text buffer</param>
        /// <param name="width">The width of the input field</param>
        /// <param name="flags">Flag for custom InputText Behaviour</param>
        /// <returns>true if value has just been edited</returns>
        public bool TextInput(string id, string hint, ref string text, uint size, float width = 0f, FuInputTextFlags flags = FuInputTextFlags.Default)
        {
            return TextInput(id, hint, ref text, size, 0f, FuFrameStyle.Default, width, flags);
        }

        /// <summary>
        /// Displays a single-line text input field with a hint.
        /// </summary>
        public bool TextInput(string id, string hint, ref string text, uint size, float width, FuTextInputOptions options)
        {
            return TextInput(id, hint, ref text, size, 0f, FuFrameStyle.Default, width, options);
        }

        /// <summary>
        /// Displays a multi-line text input field with a hint
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="hint">The hint that will be displayed when the field is empty</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <param name="size">the maximum size of the text buffer</param>
        /// <param name="height">The height of the input field</param>
        /// <param name="width">The width of the input field</param>
        /// <param name="flags">Flag for custom InputText Behaviour</param>
        /// <returns>true if value has just been edited</returns>
        public bool TextInput(string id, string hint, ref string text, uint size, float height, float width = 0f, FuInputTextFlags flags = FuInputTextFlags.Default)
        {
            return TextInput(id, hint, ref text, size, height, FuFrameStyle.Default, width, flags);
        }

        /// <summary>
        /// Displays a multi-line text input field with a hint.
        /// </summary>
        public bool TextInput(string id, string hint, ref string text, uint size, float height, float width, FuTextInputOptions options)
        {
            return TextInput(id, hint, ref text, size, height, FuFrameStyle.Default, width, options);
        }

        /// <summary>
        /// Displays a text input field with a hint
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="hint">The hint that will be displayed when the field is empty</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <param name="size">the maximum size of the text buffer</param>
        /// <param name="height">The height of the input field</param>
        /// <param name="width">The width of the input field</param>
        /// <param name="style">the Frame Style to use</param>
        /// <param name="flags">Flag for custom InputText Behaviour</param>
        /// <returns>true if value has just been edited</returns>
        public virtual bool TextInput(string id, string hint, ref string text, uint size, float height, FuFrameStyle style, float width, FuInputTextFlags flags)
        {
            return TextInput(id, hint, ref text, size, height, style, width, flags, null);
        }

        /// <summary>
        /// Displays a text input field with a hint.
        /// </summary>
        public virtual bool TextInput(string id, string hint, ref string text, uint size, float height, FuFrameStyle style, float width, FuTextInputOptions options)
        {
            FuInputTextFlags flags = options != null ? options.Flags : FuInputTextFlags.Default;
            return TextInput(id, hint, ref text, size, height, style, width, flags, options);
        }

        private bool TextInput(string id, string hint, ref string text, uint size, float height, FuFrameStyle style, float width, FuInputTextFlags flags, FuTextInputOptions options)
        {
            bool edited;
            beginElement(ref id, style);
            if (!_drawElement)
            {
                return false;
            }

            edited = _internalTextInput(id, hint, ref text, size, height, width, flags, options);
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, edited);
            displayToolTip();
            _elementHoverFramedEnabled = true;
            endElement(style);
            return edited;
        }

        /// <summary>
        /// Returns the internal text input result.
        /// </summary>
        /// <param name="id">The id value.</param>
        /// <param name="hint">The hint value.</param>
        /// <param name="text">The text value.</param>
        /// <param name="size">The size value.</param>
        /// <param name="height">The height value.</param>
        /// <param name="width">The width value.</param>
        /// <param name="flags">The flags value.</param>
        /// <returns>The result of the operation.</returns>
        private bool _internalTextInput(string id, string hint, ref string text, uint size, float height, float width, FuInputTextFlags flags)
        {
            return _internalTextInput(id, hint, ref text, size, height, width, flags, null);
        }

        private bool _internalTextInput(string id, string hint, ref string text, uint size, float height, float width, FuInputTextFlags flags, FuTextInputOptions options)
        {
            bool edited;
            if (width == 0)
            {
                width = ImGui.GetContentRegionAvail().x;
            }
            else if (width < 0)
            {
                width = ImGui.GetContentRegionAvail().x + width * Fugui.CurrentContext.Scale;
            }
            else
            {
                width *= Fugui.CurrentContext.Scale;
            }

            ImGui.SetNextItemWidth(width);
            if (LastItemDisabled)
            {
                flags |= FuInputTextFlags.ReadOnly;
            }

            ImGuiInputTextFlags imFlags = BuildInputTextFlags(flags, options);
            TextInputCallbackState callbackState = null;
            GCHandle callbackHandle = default;
            IntPtr callbackUserData = IntPtr.Zero;
            ImGuiInputTextCallback callback = null;

            if (options != null && options.HasNativeCallbacks)
            {
                callbackState = new TextInputCallbackState(id, options);
                callbackHandle = GCHandle.Alloc(callbackState);
                callbackUserData = GCHandle.ToIntPtr(callbackHandle);
                callback = _textInputCallback;
            }

            try
            {
                if (height > 0)
                {
                    edited = ImGui.InputTextMultiline(id, ref text, size, new Vector2(ImGui.GetContentRegionAvail().x, height * Fugui.CurrentContext.Scale), imFlags, callback, callbackUserData);
                }
                else
                {
                    edited = ImGui.InputTextWithHint(id, hint, ref text, size, imFlags, callback, callbackUserData);
                }
            }
            finally
            {
                if (callbackHandle.IsAllocated)
                {
                    callbackHandle.Free();
                }
            }

            if (edited && options != null && options.Submitted != null && (imFlags & ImGuiInputTextFlags.EnterReturnsTrue) != 0)
            {
                options.Submitted(new FuSubmitContext(id, text));
            }

            return edited;
        }

        private static ImGuiInputTextFlags BuildInputTextFlags(FuInputTextFlags flags, FuTextInputOptions options)
        {
            ImGuiInputTextFlags imFlags = (ImGuiInputTextFlags)flags;
            if (options == null)
            {
                return imFlags;
            }

            if (options.CharFilter != null)
            {
                imFlags |= ImGuiInputTextFlags.CallbackCharFilter;
            }
            if (options.Completion != null)
            {
                imFlags |= ImGuiInputTextFlags.CallbackCompletion;
            }
            if (options.History != null)
            {
                imFlags |= ImGuiInputTextFlags.CallbackHistory;
            }
            if (options.Edited != null)
            {
                imFlags |= ImGuiInputTextFlags.CallbackEdit;
            }
            if (options.Submitted != null)
            {
                imFlags |= ImGuiInputTextFlags.EnterReturnsTrue;
            }

            return imFlags;
        }

        private static unsafe int TextInputCallback(ImGuiInputTextCallbackData* nativeData)
        {
            ImGuiInputTextCallbackDataPtr data = new ImGuiInputTextCallbackDataPtr(nativeData);
            if (data.UserData == IntPtr.Zero)
            {
                return 0;
            }

            GCHandle handle = GCHandle.FromIntPtr(data.UserData);
            if (!(handle.Target is TextInputCallbackState state) || state.Options == null)
            {
                return 0;
            }

            string currentText = data.Buf == IntPtr.Zero ? string.Empty : Util.StringFromPtr((byte*)data.Buf.ToPointer());
            FuTextInputContext input = CreateTextInputContext(state.Id, currentText, data);
            FuTextInputOptions options = state.Options;

            if (data.EventFlag == ImGuiInputTextFlags.CallbackCharFilter)
            {
                if (options.CharFilter == null)
                {
                    return 0;
                }

                bool keepCharacter = options.CharFilter(new FuCharFilterContext(input, (char)data.EventChar));
                return keepCharacter ? 0 : 1;
            }

            if (data.EventFlag == ImGuiInputTextFlags.CallbackCompletion)
            {
                FuTextEdit? edit = options.Completion != null
                    ? options.Completion(CreateCompletionContext(input))
                    : null;
                ApplyTextEdit(data, currentText, edit);
                return 0;
            }

            if (data.EventFlag == ImGuiInputTextFlags.CallbackHistory)
            {
                FuTextInputHistoryDirection direction = data.EventKey == ImGuiKey.UpArrow
                    ? FuTextInputHistoryDirection.Previous
                    : FuTextInputHistoryDirection.Next;
                FuTextEdit? edit = options.History != null
                    ? options.History(new FuHistoryContext(input, direction))
                    : null;
                ApplyTextEdit(data, currentText, edit);
                return 0;
            }

            if (data.EventFlag == ImGuiInputTextFlags.CallbackEdit)
            {
                options.Edited?.Invoke(new FuTextEditedContext(input));
            }

            return 0;
        }

        private static FuTextInputContext CreateTextInputContext(string id, string text, ImGuiInputTextCallbackDataPtr data)
        {
            return new FuTextInputContext(
                id,
                text,
                Utf8ByteOffsetToStringIndex(text, data.CursorPos),
                Utf8ByteOffsetToStringIndex(text, data.SelectionStart),
                Utf8ByteOffsetToStringIndex(text, data.SelectionEnd));
        }

        private static FuCompletionContext CreateCompletionContext(FuTextInputContext input)
        {
            int cursor = Mathf.Clamp(input.CursorIndex, 0, input.Text.Length);
            int wordStart = cursor;
            while (wordStart > 0 && IsWordCharacter(input.Text[wordStart - 1]))
            {
                wordStart--;
            }

            string word = cursor > wordStart ? input.Text.Substring(wordStart, cursor - wordStart) : string.Empty;
            return new FuCompletionContext(input, word, wordStart);
        }

        private static bool IsWordCharacter(char character)
        {
            return char.IsLetterOrDigit(character) || character == '_';
        }

        private static void ApplyTextEdit(ImGuiInputTextCallbackDataPtr data, string currentText, FuTextEdit? edit)
        {
            if (!edit.HasValue || edit.Value.Kind == FuTextEditKind.None)
            {
                return;
            }

            FuTextEdit value = edit.Value;
            switch (value.Kind)
            {
                case FuTextEditKind.ReplaceAll:
                    data.DeleteChars(0, data.BufTextLen);
                    if (!string.IsNullOrEmpty(value.Text))
                    {
                        data.InsertChars(0, value.Text);
                    }
                    break;

                case FuTextEditKind.Insert:
                    int cursorByte = Mathf.Clamp(data.CursorPos, 0, data.BufTextLen);
                    if (!string.IsNullOrEmpty(value.Text))
                    {
                        data.InsertChars(cursorByte, value.Text);
                    }
                    break;

                case FuTextEditKind.ReplaceRange:
                    int startIndex = Mathf.Clamp(value.StartIndex, 0, currentText.Length);
                    int endIndex = Mathf.Clamp(value.StartIndex + Mathf.Max(0, value.Length), startIndex, currentText.Length);
                    int startByte = StringIndexToUtf8ByteOffset(currentText, startIndex);
                    int endByte = StringIndexToUtf8ByteOffset(currentText, endIndex);
                    int byteLength = Mathf.Max(0, endByte - startByte);
                    if (byteLength > 0)
                    {
                        data.DeleteChars(startByte, byteLength);
                    }
                    if (!string.IsNullOrEmpty(value.Text))
                    {
                        data.InsertChars(startByte, value.Text);
                    }
                    break;
            }
        }

        private static int StringIndexToUtf8ByteOffset(string text, int stringIndex)
        {
            if (string.IsNullOrEmpty(text) || stringIndex <= 0)
            {
                return 0;
            }

            int clampedIndex = Mathf.Clamp(stringIndex, 0, text.Length);
            int byteOffset = 0;
            for (int i = 0; i < clampedIndex;)
            {
                byteOffset += GetUtf8ByteCount(text, i, out int charCount);
                i += charCount;
            }

            return byteOffset;
        }

        private static int Utf8ByteOffsetToStringIndex(string text, int byteOffset)
        {
            if (string.IsNullOrEmpty(text) || byteOffset <= 0)
            {
                return 0;
            }

            int clampedByteOffset = Mathf.Max(0, byteOffset);
            int currentByteOffset = 0;
            for (int i = 0; i < text.Length;)
            {
                int charByteCount = GetUtf8ByteCount(text, i, out int charCount);
                if (currentByteOffset + charByteCount > clampedByteOffset)
                {
                    return i;
                }

                currentByteOffset += charByteCount;
                i += charCount;
                if (currentByteOffset == clampedByteOffset)
                {
                    return i;
                }
            }

            return text.Length;
        }

        private static int GetUtf8ByteCount(string text, int index, out int charCount)
        {
            int scalar;
            if (char.IsHighSurrogate(text[index]) &&
                index + 1 < text.Length &&
                char.IsLowSurrogate(text[index + 1]))
            {
                scalar = char.ConvertToUtf32(text[index], text[index + 1]);
                charCount = 2;
            }
            else
            {
                scalar = text[index];
                charCount = 1;
            }

            if (scalar <= 0x7F)
            {
                return 1;
            }
            if (scalar <= 0x7FF)
            {
                return 2;
            }
            if (scalar <= 0xFFFF)
            {
                return 3;
            }
            return 4;
        }

        private sealed class TextInputCallbackState
        {
            public readonly string Id;
            public readonly FuTextInputOptions Options;

            public TextInputCallbackState(string id, FuTextInputOptions options)
            {
                Id = id;
                Options = options;
            }
        }
        #endregion
    }
}