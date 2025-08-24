//#define FUGUI_USE_TEXTUREARRAY
using ImGuiNET;
using System;
using System.Collections.Generic;
#if !FUGUI_USE_TEXTUREARRAY
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
#endif
using UnityEngine;
using UTexture = UnityEngine.Texture;

namespace Fu
{
    public class TextureManager
    {
        private readonly Dictionary<IntPtr, UTexture> _textures = new Dictionary<IntPtr, UTexture>();
        private readonly Dictionary<UTexture, IntPtr> _textureIds = new Dictionary<UTexture, IntPtr>();
        private readonly Dictionary<Sprite, SpriteInfo> _spriteData = new Dictionary<Sprite, SpriteInfo>();
        private ImFontAtlasPtr _fontAtlas;

#if FUGUI_USE_TEXTUREARRAY
        private static Dictionary<float, Texture2DArray> _atlasTexture = new Dictionary<float, Texture2DArray>();
        public unsafe void InitializeFontAtlas(ImGuiIOPtr io)
        {
            // dol not create texture if already exists (context will share). raw textures are very heavy
            if (_atlasTexture.ContainsKey(FuGui.CurrentContext.FontScale))
            {
                return;
            }
            _fontAtlas = io.Fonts;
            _fontAtlas.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);
            // Create a new Texture2DArray with the maximum size and RGBA32 format
            Texture2DArray texture = createTextureArray(pixels, width, height, bytesPerPixel, SystemInfo.maxTextureSize, SystemInfo.maxTextureSize);
            _atlasTexture.Add(FuGui.CurrentContext.FontScale, texture);
        }

        private unsafe Texture2DArray createTextureArray(byte* pixels, int width, int height, int bytesPerPixel, int maxWidth = 4096, int maxHeight = 4096)
        {
            maxWidth = Mathf.Min(maxWidth, width);
            maxHeight = Mathf.Min(maxHeight, height);
            int numTextures = Mathf.CeilToInt(width * height * bytesPerPixel / (float)(maxWidth * maxHeight * bytesPerPixel));
            Texture2DArray textureArray = new Texture2DArray(maxWidth, maxHeight, numTextures, TextureFormat.RGBA32, true);

            for (int i = 0; i < numTextures; i++)
            {
                int subWidth = Mathf.Min(maxWidth, width - (maxWidth * i));
                int subHeight = Mathf.Min(maxHeight, height - (maxHeight * i));
                int lenght = subWidth * subHeight * bytesPerPixel;
                Color32[] colorArray = new Color32[subWidth * subHeight];

                fixed (Color32* colorPointer = colorArray)
                {
                    byte* colorPtr = (byte*)colorPointer;
                    Buffer.MemoryCopy(pixels, colorPtr, (long)lenght, (long)lenght);
                }

                //GCHandle handle = GCHandle.Alloc(colorArray, GCHandleType.Pinned);
                //byte* pixelsDataPtr = (byte*)handle.AddrOfPinnedObject();

                //Debug.Log((pixels == null).ToString() + " " + (pixelsDataPtr == null).ToString());
                //for (int ind = 0; ind < subWidth * subHeight; ind++)
                //    {
                //        Debug.Log((pixels == null).ToString() + " " + ind);
                //        colorArray[ind] = new Color32(*pixels++, *pixels++, *pixels++, *pixels++);
                //        //Debug.Log((*pixelsDataPtr).ToString() + " " + (*pixels).ToString());
                //        //*pixelsDataPtr = *pixels;
                //        //pixelsDataPtr += ind;
                //        //pixels += ind;
                //    }
                //Buffer.MemoryCopy(pixels, pixelsDataPtr, lenght, lenght);

                //Texture2D subTexture = new Texture2D(subWidth, subHeight, TextureFormat.RGBA32, false, false)
                //{
                //    filterMode = FilterMode.Point
                //};

                //                    // create native byte array to store pixels data
                //                    NativeArray<byte> srcData = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(pixels, lenght, Allocator.None);
                //#if ENABLE_UNITY_COLLECTIONS_CHECKS
                //                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref srcData, AtomicSafetyHandle.GetTempMemoryHandle());
                //#endif
                //                    // Invert y while copying the atlas texture.
                //                    NativeArray<byte> dstData = subTexture.GetRawTextureData<byte>();
                //                    int stride = subWidth * bytesPerPixel;
                //                    for (int y = 0; y < subHeight; ++y)
                //                    {
                //                        NativeArray<byte>.Copy(srcData, y * stride, dstData, (subHeight - y - 1) * stride, stride);
                //                    }
                //                    // apply sub texture data
                //                    subTexture.Apply();

                //Texture2D subTexture = new Texture2D(subWidth, subHeight, TextureFormat.RGBA32, false);
                //subTexture.LoadRawTextureData((IntPtr)pixels, lenght);
                //Debug.Log("lenght : " + lenght + " " + subWidth + " " + subHeight + " " + width + " " + height);
                //subTexture.Apply();

                // set color data to texture array
                textureArray.SetPixels32(/*subTexture.GetPixels()*/colorArray, i);

                // increment pixels pointer
                pixels += lenght;
            }

            textureArray.Apply();
            return textureArray;
        }
#else
        private static Dictionary<float, Texture2D> _atlasTexture = new Dictionary<float, Texture2D>();
        public unsafe void InitializeFontAtlas(ImGuiIOPtr io)
        {
            // ignore this font atlas if already exists
            if (_atlasTexture.ContainsKey(Fugui.CurrentContext.FontScale))
            {
                return;
            }

            _fontAtlas = io.Fonts;
            _fontAtlas.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);

            if (width > SystemInfo.maxTextureSize || height > SystemInfo.maxTextureSize)
            {
                Debug.LogError("The font atlas you are trying to created is too big and exced the unity max texture size.\nconsidere reducing the size of the font, the number of different font sizes or the quantity of icons.");
            }
            if (width > SystemInfo.maxTextureSize)
                width = SystemInfo.maxTextureSize;
            if (height > SystemInfo.maxTextureSize)
                height = SystemInfo.maxTextureSize;

            Texture2D atlas = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Point
            };

            // TODO: Remove collections and make native array manually.
            NativeArray<byte> srcData = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(pixels, width * height * bytesPerPixel, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref srcData, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
            // Invert y while copying the atlas texture.
            NativeArray<byte> dstData = atlas.GetRawTextureData<byte>();
            int stride = width * bytesPerPixel;
            for (int y = 0; y < height; ++y)
            {
                NativeArray<byte>.Copy(srcData, y * stride, dstData, (height - y - 1) * stride, stride);
            }
            atlas.Apply();

            _atlasTexture.Add(Fugui.CurrentContext.FontScale, atlas);

            // register atlas texture
            IntPtr texId = RegisterTexture(atlas);
            io.Fonts.SetTexID(texId);
        }

        /// <summary>
        /// Clear font atlas from font manager
        /// </summary>
        /// <param name="oldScale">scale to remove from texture manager</param>
        public void ClearFontAtlas(float oldScale)
        {
            if (_atlasTexture.ContainsKey(oldScale))
            {
                return;
            }
            IntPtr textureID = _textureIds[_atlasTexture[oldScale]];
            _textures.Remove(textureID);
            _textureIds.Remove(_atlasTexture[oldScale]);
            UnityEngine.Object.Destroy(_atlasTexture[oldScale]);
            _atlasTexture.Remove(oldScale);

        }
#endif
        public unsafe void Shutdown()
        {
            _textures.Clear();
            _textureIds.Clear();
            _spriteData.Clear();

            float scale = Fugui.CurrentContext.FontScale;
            if (_atlasTexture != null && _atlasTexture.ContainsKey(Fugui.CurrentContext.FontScale))
            {
                // check whatever no remaning context need the font atlas texture
                bool destroyFontAtlas = true;
                foreach (FuContext context in Fugui.Contexts.Values)
                {
                    if (context.FontScale == scale)
                    {
                        destroyFontAtlas = false;
                        break;
                    }
                }
                if (destroyFontAtlas)
                {
                    UnityEngine.Object.Destroy(_atlasTexture[scale]);
                    _atlasTexture[scale] = null;
                    _atlasTexture.Remove(scale);
                }
            }
            ImGui.GetIO().Fonts.Clear(); // Previous FontDefault reference no longer valid.
            ImGui.GetIO().NativePtr->FontDefault = default; // NULL uses Fonts[0].
        }

        public void PrepareFrame(ImGuiIOPtr io)
        {
            IntPtr id = GetTextureId(_atlasTexture[Fugui.CurrentContext.FontScale]);
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
            if (_textureIds.ContainsKey(texture))
            {
                return GetTextureId(texture);
            }
            else
            {
                IntPtr id = new IntPtr(_textures.Count + 1);
                _textures.Add(id, texture);
                _textureIds.Add(texture, id);
                return id;
            }
        }
    }

    internal sealed class SpriteInfo
    {
        public UTexture Texture;
        public Vector2 Size;
        public Vector2 UV0;
        public Vector2 UV1;
    }
}