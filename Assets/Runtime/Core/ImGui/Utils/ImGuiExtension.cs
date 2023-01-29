﻿using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Fu.Core.DearImGui
{
	internal static unsafe class ImGuiExtension
	{
		private static readonly HashSet<IntPtr> _managedAllocations = new HashSet<IntPtr>();

		internal static void SetBackendPlatformName(this ImGuiIOPtr io, string name)
		{
			if (io.NativePtr->BackendPlatformName != (byte*)0)
			{
				if (_managedAllocations.Contains((IntPtr)io.NativePtr->BackendPlatformName))
				{
					Marshal.FreeHGlobal(new IntPtr(io.NativePtr->BackendPlatformName));
				}
				io.NativePtr->BackendPlatformName = (byte*)0;
			}
			if (name != null)
			{
				int byteCount = Encoding.UTF8.GetByteCount(name);
				byte* nativeName = (byte*)Marshal.AllocHGlobal(byteCount + 1);
				int offset = Utils.GetUtf8(name, nativeName, byteCount);

				nativeName[offset] = 0;

				io.NativePtr->BackendPlatformName = nativeName;
				_managedAllocations.Add((IntPtr)nativeName);
			}
		}

		public static void SetBackendRendererName(this ImGuiIOPtr io, string name)
		{
			if (io.NativePtr->BackendRendererName != (byte*)0)
			{
				if (_managedAllocations.Contains((IntPtr)io.NativePtr->BackendRendererName))
				{
					Marshal.FreeHGlobal((IntPtr)io.NativePtr->BackendRendererName);
					io.NativePtr->BackendRendererName = (byte*)0;
				}
			}
			if (name != null)
			{
				int byteCount = Encoding.UTF8.GetByteCount(name);
				byte* nativeName = (byte*)Marshal.AllocHGlobal(byteCount + 1);
				int offset = Utils.GetUtf8(name, nativeName, byteCount);

				nativeName[offset] = 0;

				io.NativePtr->BackendRendererName = nativeName;
				_managedAllocations.Add((IntPtr)nativeName);
			}
		}
	}
}