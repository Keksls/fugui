using System;
using UnityEngine;

namespace Fu
{
    	/// <summary>
    	/// Represents the Cursor Shape data structure.
    	/// </summary>
    	[Serializable]
    	public struct CursorShape
    	{
    		#region State
    		public Texture2D Texture;
    		public Vector2 Hotspot;
    		#endregion
    	}
}