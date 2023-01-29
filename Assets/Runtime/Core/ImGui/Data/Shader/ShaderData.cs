using System;
using UnityEngine;

namespace Fu.Core.DearImGui
{
	[Serializable]
	public class ShaderData
	{
		public Shader Mesh;
		public Shader Procedural;

		public ShaderData Clone()
		{
			return (ShaderData)MemberwiseClone();
		}
	}
}
