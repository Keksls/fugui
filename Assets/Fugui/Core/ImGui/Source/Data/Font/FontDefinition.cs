using UnityEngine;

namespace Fugui.Core.DearImGui
{
	[System.Serializable]
	public struct FontDefinition
	{
		[SerializeField]
		private Object _fontAsset;

		[Tooltip("Path relative to Application.streamingAssetsPath.")]
		public string Path;
		public FontConfig Config;

	}
}
