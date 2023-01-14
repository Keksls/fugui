﻿using ImGuiNET;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UTexture = UnityEngine.Texture;

namespace Fugui.Core.DearImGui.Texture
{
    public class TextureManager
    {
        private Texture2D _atlasTexture;
        private readonly Dictionary<IntPtr, UTexture> _textures = new Dictionary<IntPtr, UTexture>();
        private readonly Dictionary<UTexture, IntPtr> _textureIds = new Dictionary<UTexture, IntPtr>();
        private readonly Dictionary<Sprite, SpriteInfo> _spriteData = new Dictionary<Sprite, SpriteInfo>();
        private ImFontAtlasPtr _fontAtlas;

        public unsafe void InitializeFontAtlas(ImGuiIOPtr io)
        {
            _fontAtlas = io.Fonts;
            _fontAtlas.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);

            _atlasTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Point
            };

            // TODO: Remove collections and make native array manually.
            NativeArray<byte> srcData = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(pixels, width * height * bytesPerPixel, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref srcData, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
            // Invert y while copying the atlas texture.
            NativeArray<byte> dstData = _atlasTexture.GetRawTextureData<byte>();
            int stride = width * bytesPerPixel;
            for (int y = 0; y < height; ++y)
            {
                NativeArray<byte>.Copy(srcData, y * stride, dstData, (height - y - 1) * stride, stride);
            }

            _atlasTexture.Apply();
        }

        public unsafe void Shutdown()
        {
            _textures.Clear();
            _textureIds.Clear();
            _spriteData.Clear();

            if (_atlasTexture != null)
            {
                UnityEngine.Object.Destroy(_atlasTexture);
                _atlasTexture = null;
            }
            ImGui.GetIO().Fonts.Clear(); // Previous FontDefault reference no longer valid.
            ImGui.GetIO().NativePtr->FontDefault = default; // NULL uses Fonts[0].
        }

        public void PrepareFrame(ImGuiIOPtr io)
        {
            IntPtr id = RegisterTexture(_atlasTexture);
            io.Fonts.SetTexID(id);
        }

        public bool TryGetTexture(IntPtr id, out UTexture texture)
        {
            return _textures.TryGetValue(id, out texture);
        }

        public IntPtr GetTextureId(UTexture texture)
        {
            return _textureIds.TryGetValue(texture, out IntPtr id) ? id : RegisterTexture(texture);
        }

        internal SpriteInfo GetSpriteInfo(Sprite sprite)
        {
            if (!_spriteData.TryGetValue(sprite, out SpriteInfo spriteInfo))
            {
                _spriteData[sprite] = spriteInfo = new SpriteInfo
                {
                    Texture = sprite.texture,
                    Size = sprite.rect.size,
                    UV0 = sprite.uv[0],
                    UV1 = sprite.uv[1],
                };
            }

            return spriteInfo;
        }

        private IntPtr RegisterTexture(UTexture texture)
        {
            IntPtr id = texture.GetNativeTexturePtr();
            _textures[id] = texture;
            _textureIds[texture] = id;
            return id;
        }
    }
}