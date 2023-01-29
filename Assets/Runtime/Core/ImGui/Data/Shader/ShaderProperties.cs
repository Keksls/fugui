using System;

namespace Fu.Core.DearImGui
{
	[Serializable]
	public class ShaderProperties
	{
		public string Texture;
		public string Vertices;
		public string BaseVertex;

		public ShaderProperties Clone()
		{
			return (ShaderProperties)MemberwiseClone();
		}
	}
}