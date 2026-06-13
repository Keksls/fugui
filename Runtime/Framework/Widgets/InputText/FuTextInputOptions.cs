using System;

namespace Fu.Framework
{
    /// <summary>
    /// High-level Fugui options for text inputs. ImGui callbacks remain an internal implementation detail.
    /// </summary>
    public sealed class FuTextInputOptions
    {
        /// <summary>
        /// Input flags applied to the widget.
        /// </summary>
        public FuInputTextFlags Flags = FuInputTextFlags.Default;

        /// <summary>
        /// Optional character filter. Return true to keep the character, false to reject it.
        /// </summary>
        public Func<FuCharFilterContext, bool> CharFilter;

        /// <summary>
        /// Optional completion callback, usually triggered by Tab.
        /// </summary>
        public Func<FuCompletionContext, FuTextEdit?> Completion;

        /// <summary>
        /// Optional history callback, usually triggered by Up/Down.
        /// </summary>
        public Func<FuHistoryContext, FuTextEdit?> History;

        /// <summary>
        /// Optional edit callback called when ImGui reports an input edit.
        /// </summary>
        public Action<FuTextEditedContext> Edited;

        /// <summary>
        /// Optional submit callback. Fugui automatically adds EnterReturnsTrue when this is set.
        /// </summary>
        public Action<FuSubmitContext> Submitted;

        internal bool HasNativeCallbacks
        {
            get
            {
                return CharFilter != null || Completion != null || History != null || Edited != null;
            }
        }
    }

    /// <summary>
    /// Describes a text edit requested by a Fugui text-input callback.
    /// </summary>
    public readonly struct FuTextEdit
    {
        internal readonly FuTextEditKind Kind;
        internal readonly int StartIndex;
        internal readonly int Length;

        /// <summary>
        /// Text inserted by this edit.
        /// </summary>
        public readonly string Text;

        private FuTextEdit(FuTextEditKind kind, int startIndex, int length, string text)
        {
            Kind = kind;
            StartIndex = startIndex;
            Length = length;
            Text = text ?? string.Empty;
        }

        /// <summary>
        /// No edit.
        /// </summary>
        public static FuTextEdit None
        {
            get { return new FuTextEdit(FuTextEditKind.None, 0, 0, string.Empty); }
        }

        /// <summary>
        /// Replace the whole text.
        /// </summary>
        public static FuTextEdit ReplaceAll(string text)
        {
            return new FuTextEdit(FuTextEditKind.ReplaceAll, 0, 0, text);
        }

        /// <summary>
        /// Insert text at the current cursor position.
        /// </summary>
        public static FuTextEdit Insert(string text)
        {
            return new FuTextEdit(FuTextEditKind.Insert, 0, 0, text);
        }

        /// <summary>
        /// Replace a C# string range.
        /// </summary>
        public static FuTextEdit ReplaceRange(int startIndex, int length, string text)
        {
            return new FuTextEdit(FuTextEditKind.ReplaceRange, startIndex, length, text);
        }

        /// <summary>
        /// Delete a C# string range.
        /// </summary>
        public static FuTextEdit DeleteRange(int startIndex, int length)
        {
            return ReplaceRange(startIndex, length, string.Empty);
        }
    }

    internal enum FuTextEditKind
    {
        None,
        ReplaceAll,
        Insert,
        ReplaceRange
    }

    /// <summary>
    /// Direction requested by a text-input history callback.
    /// </summary>
    public enum FuTextInputHistoryDirection
    {
        Previous,
        Next
    }

    /// <summary>
    /// Common text-input callback context.
    /// </summary>
    public readonly struct FuTextInputContext
    {
        public readonly string Id;
        public readonly string Text;
        public readonly int CursorIndex;
        public readonly int SelectionStartIndex;
        public readonly int SelectionEndIndex;

        internal FuTextInputContext(string id, string text, int cursorIndex, int selectionStartIndex, int selectionEndIndex)
        {
            Id = id;
            Text = text ?? string.Empty;
            CursorIndex = cursorIndex;
            SelectionStartIndex = selectionStartIndex;
            SelectionEndIndex = selectionEndIndex;
        }
    }

    /// <summary>
    /// Character-filter callback context.
    /// </summary>
    public readonly struct FuCharFilterContext
    {
        public readonly FuTextInputContext Input;
        public readonly char Character;

        internal FuCharFilterContext(FuTextInputContext input, char character)
        {
            Input = input;
            Character = character;
        }
    }

    /// <summary>
    /// Completion callback context.
    /// </summary>
    public readonly struct FuCompletionContext
    {
        public readonly FuTextInputContext Input;
        public readonly string CurrentWord;
        public readonly int CurrentWordStartIndex;

        internal FuCompletionContext(FuTextInputContext input, string currentWord, int currentWordStartIndex)
        {
            Input = input;
            CurrentWord = currentWord ?? string.Empty;
            CurrentWordStartIndex = currentWordStartIndex;
        }
    }

    /// <summary>
    /// History callback context.
    /// </summary>
    public readonly struct FuHistoryContext
    {
        public readonly FuTextInputContext Input;
        public readonly FuTextInputHistoryDirection Direction;

        internal FuHistoryContext(FuTextInputContext input, FuTextInputHistoryDirection direction)
        {
            Input = input;
            Direction = direction;
        }
    }

    /// <summary>
    /// Edit callback context.
    /// </summary>
    public readonly struct FuTextEditedContext
    {
        public readonly FuTextInputContext Input;

        internal FuTextEditedContext(FuTextInputContext input)
        {
            Input = input;
        }
    }

    /// <summary>
    /// Submit callback context.
    /// </summary>
    public readonly struct FuSubmitContext
    {
        public readonly string Id;
        public readonly string Text;

        internal FuSubmitContext(string id, string text)
        {
            Id = id;
            Text = text ?? string.Empty;
        }
    }
}