using ImGuiNET;
using UnityEngine;

namespace Fu
{
	// TODO: Implement animated cursor.
	/// <summary>
	/// Represents the Cursor Shapes Asset type.
	/// </summary>
	[CreateAssetMenu(menuName = "Fugui/Cursor Shapes")]
	public sealed class CursorShapesAsset : ScriptableObject
	{
		#region State
		[Tooltip("Default.")]
		public CursorShape Arrow;

		[Tooltip("When hovering over InputText, etc.")]
		public CursorShape TextInput;

		[Tooltip("(Unused by ImGui functions)")]
		public CursorShape ResizeAll;

		[Tooltip("When hovering over an horizontal border")]
		public CursorShape ResizeNS;

		[Tooltip("When hovering over a vertical border or a column")]
		public CursorShape ResizeEW;

		[Tooltip("When hovering over the bottom-left corner of a window")]
		public CursorShape ResizeNESW;

		[Tooltip("When hovering over the bottom-right corner of a window")]
		public CursorShape ResizeNWSE;

		[Tooltip("(Unused by ImGui functions. Use for e.g. hyperlinks)")]
		public CursorShape Hand;

		[Tooltip("When hovering something with disabled interaction. Usually a crossed circle.")]
		public CursorShape NotAllowed;

		public ref CursorShape this[FuMouseCursor cursor]
		{
			get
			{
				switch (cursor)
				{
					case FuMouseCursor.Arrow: return ref Arrow;
					case FuMouseCursor.TextInput: return ref TextInput;
					case FuMouseCursor.ResizeAll: return ref ResizeAll;
					case FuMouseCursor.ResizeEW: return ref ResizeEW;
					case FuMouseCursor.ResizeNS: return ref ResizeNS;
					case FuMouseCursor.ResizeNESW: return ref ResizeNESW;
					case FuMouseCursor.ResizeNWSE: return ref ResizeNWSE;
					case FuMouseCursor.Hand: return ref Hand;
					case FuMouseCursor.NotAllowed: return ref NotAllowed;
					default: return ref Arrow;
				}
			}
		}

		internal ref CursorShape this[ImGuiMouseCursor cursor]
		{
			get
			{
				return ref this[cursor.ToFugui()];
			}
		}
		#endregion
	}
}
