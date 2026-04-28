#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
#endif
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
#if FUMOBILE
using UnityEngine.Networking;
#endif

namespace Fu
{
    /// <summary>
    /// Locates, writes and loads Fugui baked font atlas textures.
    /// </summary>
    public static class FuFontAtlasCache
    {
        public const string DefaultBakedAtlasFolder = "Fugui/FontAtlases";

        private const string AtlasHashVersion = "FuguiFontAtlasCache/v1";

        /// <summary>
        /// Kept for editor workflows that want to invalidate derived atlas state.
        /// </summary>
        public static void ClearHashCache()
        {
        }

        /// <summary>
        /// Attempts to load a baked atlas texture for the provided FontConfig and font scale.
        /// </summary>
        public static bool TryLoadBakedTexture(FontConfig fontConfig, float fontScale, out Texture2D atlas)
        {
            atlas = null;

            if (fontConfig == null || !fontConfig.UseBakedFontAtlas)
            {
                return false;
            }

            string atlasPath = CombineStreamingPath(Application.streamingAssetsPath, GetAtlasRelativePath(fontConfig, fontScale, Application.streamingAssetsPath));
            byte[] bytes = ReadStreamingAssetBytes(atlasPath, false);
            if (bytes == null || bytes.Length == 0)
            {
                return false;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Point,
                name = $"Fugui Font Atlas {FormatScale(fontScale)}"
            };

            if (!ImageConversion.LoadImage(texture, bytes, false))
            {
                UnityEngine.Object.Destroy(texture);
                Debug.LogWarning($"[FontAtlasCache] Failed to decode baked font atlas: {atlasPath}");
                return false;
            }

            texture.filterMode = FilterMode.Point;
            atlas = texture;
            return true;
        }

        /// <summary>
        /// Returns the baked atlas path relative to StreamingAssets.
        /// </summary>
        public static string GetAtlasRelativePath(FontConfig fontConfig, float fontScale, string streamingAssetsPath)
        {
            string folder = string.IsNullOrWhiteSpace(fontConfig.BakedFontAtlasFolder)
                ? DefaultBakedAtlasFolder
                : fontConfig.BakedFontAtlasFolder;

            return CombineRelativePath(
                folder,
                GetAtlasHash(fontConfig, streamingAssetsPath),
                $"{GetScaleKey(fontScale)}.png");
        }

        /// <summary>
        /// Returns the configured font scale bucket used by runtime atlases.
        /// </summary>
        public static float QuantizeFontScale(FontConfig fontConfig, float fontScale)
        {
            fontScale = Mathf.Max(0.0001f, fontScale);
            if (fontConfig == null || !fontConfig.QuantizeFontScale)
            {
                return fontScale;
            }

            float step = Mathf.Max(0.0001f, fontConfig.FontScaleQuantizationStep);
            return Mathf.Max(0.0001f, Mathf.Round(fontScale / step) * step);
        }

        /// <summary>
        /// Returns the hash folder used for the current FontConfig and source font files.
        /// </summary>
        public static string GetAtlasHash(FontConfig fontConfig, string streamingAssetsPath)
        {
            if (fontConfig == null)
            {
                return "missing-config";
            }

            using (SHA256 sha = SHA256.Create())
            {
                string fontRoot = CombineStreamingPath(streamingAssetsPath, fontConfig.FontsFolder);
                HashText(sha, AtlasHashVersion);
                HashText(sha, NormalizeFolder(fontConfig.FontsFolder));
                HashText(sha, fontConfig.DefaultSize.ToString(CultureInfo.InvariantCulture));

                HashSet<string> hashedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (fontConfig.Fonts != null)
                {
                    foreach (FontSizeConfig font in fontConfig.Fonts)
                    {
                        if (font == null)
                        {
                            continue;
                        }

                        HashText(sha, $"size:{font.Size}");
                        HashSubFonts(sha, fontRoot, "regular", font.SubFonts_Regular, hashedFiles);
                        HashSubFonts(sha, fontRoot, "bold", font.SubFonts_Bold, hashedFiles);
                        HashSubFonts(sha, fontRoot, "italic", font.SubFonts_Italic, hashedFiles);
                    }
                }

                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return ToHex(sha.Hash, 16);
            }
        }

        /// <summary>
        /// Combines StreamingAssets paths while preserving URL-like Android/iOS paths.
        /// </summary>
        public static string CombineStreamingPath(string root, string relative)
        {
            if (string.IsNullOrEmpty(root))
            {
                return NormalizeFolder(relative);
            }

            if (string.IsNullOrEmpty(relative))
            {
                return root;
            }

            if (root.Contains("://"))
            {
                return $"{root.TrimEnd('/')}/{NormalizeFolder(relative).TrimStart('/')}";
            }

            return Path.Combine(root, relative);
        }

        internal static string CombineRelativePath(params string[] parts)
        {
            StringBuilder builder = new StringBuilder();

            foreach (string part in parts)
            {
                if (string.IsNullOrWhiteSpace(part))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append('/');
                }

                builder.Append(NormalizeFolder(part).Trim('/'));
            }

            return builder.ToString();
        }

        internal static unsafe Texture2D CreateTextureFromAtlas(ImFontAtlasPtr fontAtlas, string textureName)
        {
            return CreateTextureFromAtlas(fontAtlas, textureName, false);
        }

        internal static unsafe Texture2D CreateTextureFromAtlas(ImFontAtlasPtr fontAtlas, string textureName, bool useAlpha8)
        {
            if (useAlpha8)
            {
                fontAtlas.GetTexDataAsAlpha8(out byte* alphaPixels, out int alphaWidth, out int alphaHeight, out int alphaBytesPerPixel);
                return CreateTextureFromPixels(alphaPixels, alphaWidth, alphaHeight, alphaBytesPerPixel, textureName, TextureFormat.Alpha8);
            }

            fontAtlas.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);
            return CreateTextureFromPixels(pixels, width, height, bytesPerPixel, textureName, TextureFormat.RGBA32);
        }

        internal static unsafe Texture2D CreateTextureFromPixels(byte* pixels, int width, int height, int bytesPerPixel, string textureName)
        {
            return CreateTextureFromPixels(pixels, width, height, bytesPerPixel, textureName, TextureFormat.RGBA32);
        }

        internal static unsafe Texture2D CreateTextureFromPixels(byte* pixels, int width, int height, int bytesPerPixel, string textureName, TextureFormat textureFormat)
        {
            if (pixels == null || width <= 0 || height <= 0 || bytesPerPixel <= 0)
            {
                return null;
            }

            if (width > SystemInfo.maxTextureSize || height > SystemInfo.maxTextureSize)
            {
                Debug.LogError("The font atlas you are trying to create is too big and exceeds the Unity max texture size.\nConsider reducing the font size, the number of font sizes or the number of icons.");
            }

            width = Mathf.Min(width, SystemInfo.maxTextureSize);
            height = Mathf.Min(height, SystemInfo.maxTextureSize);

            Texture2D atlas = new Texture2D(width, height, textureFormat, false, false)
            {
                filterMode = FilterMode.Point,
                name = textureName
            };

            NativeArray<byte> srcData = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(pixels, width * height * bytesPerPixel, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref srcData, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
            NativeArray<byte> dstData = atlas.GetRawTextureData<byte>();
            int stride = width * bytesPerPixel;
            for (int y = 0; y < height; ++y)
            {
                NativeArray<byte>.Copy(srcData, y * stride, dstData, (height - y - 1) * stride, stride);
            }

            atlas.Apply();
            return atlas;
        }

        internal static byte[] ReadStreamingAssetBytes(string path, bool logErrors)
        {
#if FUMOBILE
            using (UnityWebRequest request = UnityWebRequest.Get(path))
            {
                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    if (logErrors)
                    {
                        Debug.LogError($"[FontAtlasCache] Failed to load streaming asset: {path} - {request.error}");
                    }

                    return null;
                }

                return request.downloadHandler.data;
            }
#else
            if (!File.Exists(path))
            {
                if (logErrors)
                {
                    Debug.LogError($"[FontAtlasCache] Streaming asset not found: {path}");
                }

                return null;
            }

            return File.ReadAllBytes(path);
#endif
        }

        internal static string GetScaleKey(float fontScale)
        {
            int scale = Mathf.RoundToInt(Mathf.Max(0.0001f, fontScale) * 1000f);
            return $"scale_{scale.ToString(CultureInfo.InvariantCulture)}";
        }

        private static string FormatScale(float fontScale)
        {
            return fontScale.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static void HashSubFonts(SHA256 sha, string fontRoot, string label, SubFontConfig[] subFonts, HashSet<string> hashedFiles)
        {
            HashText(sha, label);

            if (subFonts == null)
            {
                return;
            }

            foreach (SubFontConfig subFont in subFonts)
            {
                if (subFont == null)
                {
                    continue;
                }

                HashText(sha, subFont.FileName ?? string.Empty);
                HashText(sha, subFont.StartGlyph.ToString(CultureInfo.InvariantCulture));
                HashText(sha, subFont.EndGlyph.ToString(CultureInfo.InvariantCulture));
                HashText(sha, subFont.SizeOffset.ToString(CultureInfo.InvariantCulture));
                HashText(sha, subFont.GlyphOffset.x.ToString(CultureInfo.InvariantCulture));
                HashText(sha, subFont.GlyphOffset.y.ToString(CultureInfo.InvariantCulture));

                if (subFont.CustomGlyphRanges != null)
                {
                    for (int i = 0; i < subFont.CustomGlyphRanges.Length; i++)
                    {
                        HashText(sha, subFont.CustomGlyphRanges[i].ToString(CultureInfo.InvariantCulture));
                    }
                }

                if (!string.IsNullOrEmpty(subFont.FileName) && hashedFiles.Add(subFont.FileName))
                {
                    string fontPath = CombineStreamingPath(fontRoot, subFont.FileName);
                    byte[] fontBytes = ReadStreamingAssetBytes(fontPath, false);
                    if (fontBytes == null || fontBytes.Length == 0)
                    {
                        HashText(sha, $"missing:{subFont.FileName}");
                    }
                    else
                    {
                        HashText(sha, $"file:{subFont.FileName}:{fontBytes.Length}");
                        sha.TransformBlock(fontBytes, 0, fontBytes.Length, null, 0);
                    }
                }
            }
        }

        private static void HashText(SHA256 sha, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
            if (bytes.Length > 0)
            {
                sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
            }

            sha.TransformBlock(new byte[] { 0 }, 0, 1, null, 0);
        }

        private static string NormalizeFolder(string folder)
        {
            return (folder ?? string.Empty).Replace('\\', '/').Trim();
        }

        private static string ToHex(byte[] bytes, int maxBytes)
        {
            StringBuilder builder = new StringBuilder(maxBytes * 2);
            int count = Mathf.Min(bytes.Length, maxBytes);

            for (int i = 0; i < count; i++)
            {
                builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }
    }

    /// <summary>
    /// Builds and shares native ImGui font atlases between contexts that use the same FontConfig and font scale.
    /// </summary>
    internal static unsafe class FuSharedFontAtlasCache
    {
        private static readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();

        internal sealed class Entry
        {
            public string Key;
            public float FontScale;
            public ImFontAtlasPtr Atlas;
            public Dictionary<int, FontSet> Fonts = new Dictionary<int, FontSet>();
            public FontSet DefaultFont;
            public List<byte[]> FontBuffers = new List<byte[]>();
            public int RefCount;
        }

        internal static bool IsEnabled(FontConfig fontConfig)
        {
            return fontConfig != null && fontConfig.UseSharedFontAtlas;
        }

        internal static Entry GetOrCreate(FontConfig fontConfig, float fontScale, string streamingAssetsPath)
        {
            if (!IsEnabled(fontConfig))
            {
                return null;
            }

            fontScale = FuFontAtlasCache.QuantizeFontScale(fontConfig, fontScale);
            string key = GetKey(fontConfig, fontScale, streamingAssetsPath);
            if (_entries.TryGetValue(key, out Entry existing) && existing != null)
            {
                existing.RefCount++;
                return existing;
            }

            Entry entry = BuildEntry(fontConfig, fontScale, streamingAssetsPath, key);
            if (entry == null)
            {
                return null;
            }

            entry.RefCount = 1;
            _entries[key] = entry;
            return entry;
        }

        internal static void Release(Entry entry)
        {
            if (entry == null)
            {
                return;
            }

            entry.RefCount = Mathf.Max(0, entry.RefCount - 1);
        }

        internal static void Prewarm(FontConfig fontConfig, float fontScale, string streamingAssetsPath)
        {
            Entry entry = GetOrCreate(fontConfig, fontScale, streamingAssetsPath);
            Release(entry);
        }

        private static Entry BuildEntry(FontConfig fontConfig, float fontScale, string streamingAssetsPath, string key)
        {
            IntPtr previousContext = ImGuiNative.igGetCurrentContext();
            IntPtr buildContext = IntPtr.Zero;
            ImFontAtlasPtr atlas = new ImFontAtlasPtr(ImGuiNative.ImFontAtlas_ImFontAtlas());

            try
            {
                buildContext = ImGui.CreateContext(atlas);
                ImGuiNative.igSetCurrentContext(buildContext);

                Entry entry = new Entry
                {
                    Key = key,
                    FontScale = fontScale,
                    Atlas = atlas
                };

                ImGuiIOPtr io = ImGui.GetIO();
                FuFontLoader.LoadFonts(
                    io,
                    fontConfig,
                    fontScale,
                    streamingAssetsPath,
                    entry.Fonts,
                    out entry.DefaultFont,
                    entry.FontBuffers);

                if (!io.Fonts.Build())
                {
                    Debug.LogError($"[FontAtlasCache] Failed to build shared font atlas for scale {fontScale:0.###}.");
                    atlas.Destroy();
                    return null;
                }

                return entry;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                atlas.Destroy();
                return null;
            }
            finally
            {
                if (buildContext != IntPtr.Zero)
                {
                    ImGuiNative.igSetCurrentContext(buildContext);
                    ImGui.DestroyContext(buildContext);
                }

                ImGuiNative.igSetCurrentContext(previousContext);
            }
        }

        private static string GetKey(FontConfig fontConfig, float fontScale, string streamingAssetsPath)
        {
            return $"{FuFontAtlasCache.GetAtlasHash(fontConfig, streamingAssetsPath)}:{FuFontAtlasCache.GetScaleKey(fontScale)}";
        }
    }
}
