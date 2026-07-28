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
#if UNITY_ANDROID && !UNITY_EDITOR
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

        private const string AtlasHashVersion = "FuguiFontAtlasCache/v3";

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

            try
            {
                // Ownership transfers to the caller only after decoding succeeds.
                if (!ImageConversion.LoadImage(texture, bytes, false))
                {
                    DestroyOwnedTexture(texture);
                    Debug.LogWarning($"[FontAtlasCache] Failed to decode baked font atlas: {atlasPath}");
                    return false;
                }
            }
            catch
            {
                DestroyOwnedTexture(texture);
                throw;
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
                HashFontConfig(sha, fontConfig, streamingAssetsPath, true);
                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return ToHex(sha.Hash, 16);
            }
        }

        internal static string GetAtlasCacheKey(FontConfig fontConfig, float fontScale, string streamingAssetsPath)
        {
            return $"{GetAtlasHash(fontConfig, streamingAssetsPath)}:{GetScaleKey(fontScale)}";
        }

        internal static string GetConfigSignature(FontConfig fontConfig)
        {
            if (fontConfig == null)
            {
                return "missing-config";
            }

            using (SHA256 sha = SHA256.Create())
            {
                HashText(sha, fontConfig.UseBakedFontAtlas ? "baked:on" : "baked:off");
                HashText(sha, NormalizeFolder(fontConfig.BakedFontAtlasFolder));
                HashText(sha, fontConfig.UseAlpha8FontAtlasTexture ? "alpha8:on" : "alpha8:off");
                HashFontConfig(sha, fontConfig, null, false);
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

            try
            {
                // The created texture remains locally owned until every source row has been uploaded successfully.
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
            catch
            {
                DestroyOwnedTexture(atlas);
                throw;
            }
        }

        /// <summary>
        /// Destroys one font-atlas texture that has not been transferred to a caller or texture manager.
        /// </summary>
        /// <param name="texture">Locally owned font-atlas texture.</param>
        private static void DestroyOwnedTexture(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            // Atlas baking and previews can run outside play mode.
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(texture);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        internal static byte[] ReadStreamingAssetBytes(string path, bool logErrors)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
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

        private static void HashFontConfig(SHA256 sha, FontConfig fontConfig, string streamingAssetsPath, bool hashFontFiles)
        {
            string fontRoot = hashFontFiles ? CombineStreamingPath(streamingAssetsPath, fontConfig.FontsFolder) : null;
            HashSet<string> hashedFiles = hashFontFiles ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) : null;

            HashText(sha, AtlasHashVersion);
            HashText(sha, NormalizeFolder(fontConfig.FontsFolder));
            HashText(sha, fontConfig.DefaultSize.ToString(CultureInfo.InvariantCulture));
            HashText(sha, fontConfig.DefaultFontName);

            if (fontConfig.Fonts == null)
            {
                return;
            }

            foreach (FontSizeConfig font in fontConfig.Fonts)
            {
                if (font == null)
                {
                    continue;
                }

                HashText(sha, $"name:{font.Name}");
                HashText(sha, $"size:{font.Size}");
                HashSubFonts(sha, fontRoot, "regular", font.SubFonts_Regular, hashedFiles);
                HashSubFonts(sha, fontRoot, "bold", font.SubFonts_Bold, hashedFiles);
                HashSubFonts(sha, fontRoot, "italic", font.SubFonts_Italic, hashedFiles);
            }
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

                if (hashedFiles != null && !string.IsNullOrEmpty(subFont.FileName) && hashedFiles.Add(subFont.FileName))
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
            public Dictionary<string, Dictionary<int, FontSet>> Fonts = new Dictionary<string, Dictionary<int, FontSet>>();
            public FontSet DefaultFont;
            public int RefCount;
        }

        /// <summary>
        /// Returns whether native font-atlas sharing is enabled for a configuration.
        /// </summary>
        /// <param name="fontConfig">Font configuration to inspect.</param>
        /// <returns>True when contexts should share native atlases.</returns>
        internal static bool IsEnabled(FontConfig fontConfig)
        {
            // A missing configuration cannot participate in the shared cache.
            return fontConfig != null && fontConfig.UseSharedFontAtlas;
        }

        /// <summary>
        /// Acquires a reference to a completed shared atlas, building it when necessary.
        /// </summary>
        /// <param name="fontConfig">Font configuration to load.</param>
        /// <param name="fontScale">Requested font scale.</param>
        /// <param name="streamingAssetsPath">Root path used to resolve font files.</param>
        /// <returns>Acquired cache entry, or null when sharing or atlas construction is unavailable.</returns>
        internal static Entry GetOrCreate(FontConfig fontConfig, float fontScale, string streamingAssetsPath)
        {
            // Only completed entries enter the cache, so callers never borrow a half-built atlas.
            if (!IsEnabled(fontConfig))
            {
                return null;
            }

            fontScale = FuFontAtlasCache.QuantizeFontScale(fontConfig, fontScale);
            string key = GetKey(fontConfig, fontScale, streamingAssetsPath);
            if (_entries.TryGetValue(key, out Entry existing) &&
                existing != null &&
                existing.Atlas.NativePtr != null)
            {
                existing.RefCount++;
                return existing;
            }

            _entries.Remove(key);
            Entry entry = BuildEntry(fontConfig, fontScale, streamingAssetsPath, key);
            if (entry == null)
            {
                return null;
            }

            entry.RefCount = 1;
            _entries[key] = entry;
            return entry;
        }

        /// <summary>
        /// Releases one context reference to a shared native atlas.
        /// </summary>
        /// <param name="entry">Previously acquired cache entry.</param>
        internal static void Release(Entry entry)
        {
            // Atlases remain cached until session shutdown to avoid repeated native rebuilds.
            if (entry == null)
            {
                return;
            }

            entry.RefCount = Mathf.Max(0, entry.RefCount - 1);
        }

        /// <summary>
        /// Builds one shared atlas ahead of use without retaining a context reference.
        /// </summary>
        /// <param name="fontConfig">Font configuration to load.</param>
        /// <param name="fontScale">Requested font scale.</param>
        /// <param name="streamingAssetsPath">Root path used to resolve font files.</param>
        internal static void Prewarm(FontConfig fontConfig, float fontScale, string streamingAssetsPath)
        {
            // The cache owns the built atlas; prewarming does not represent a live context borrower.
            Entry entry = GetOrCreate(fontConfig, fontScale, streamingAssetsPath);
            Release(entry);
        }

        /// <summary>
        /// Destroys every native atlas owned by the current Fugui session.
        /// </summary>
        internal static void Shutdown()
        {
            // Contexts borrow these atlases; session shutdown runs only after all contexts have been destroyed.
            Exception firstException = null;
            foreach (Entry entry in _entries.Values)
            {
                if (entry == null)
                {
                    continue;
                }

                if (entry.RefCount != 0)
                {
                    Debug.LogWarning($"[FontAtlasCache] Destroying atlas '{entry.Key}' with {entry.RefCount} outstanding reference(s).");
                }

                try
                {
                    if (entry.Atlas.NativePtr != null)
                    {
                        entry.Atlas.Destroy();
                    }
                }
                catch (Exception exception)
                {
                    // Continue so one failed native destruction cannot retain later atlas allocations.
                    firstException ??= exception;
                }
                finally
                {
                    entry.Atlas = default;
                    entry.Fonts.Clear();
                    entry.DefaultFont = null;
                    entry.RefCount = 0;
                }
            }

            _entries.Clear();
            if (firstException != null)
            {
                throw new InvalidOperationException("One or more shared native font atlases failed to destroy.", firstException);
            }
        }

        /// <summary>
        /// Builds one independently owned native atlas for a shared cache entry.
        /// </summary>
        /// <param name="fontConfig">Font configuration to load.</param>
        /// <param name="fontScale">Quantized font scale.</param>
        /// <param name="streamingAssetsPath">Root path used to resolve fonts.</param>
        /// <param name="key">Stable cache key for this atlas.</param>
        /// <returns>Completed cache entry, or null when atlas build failed.</returns>
        private static Entry BuildEntry(FontConfig fontConfig, float fontScale, string streamingAssetsPath, string key)
        {
            // A temporary context performs the build but never owns the externally allocated atlas.
            IntPtr previousContext = ImGuiNative.igGetCurrentContext();
            IntPtr buildContext = IntPtr.Zero;
            ImFontAtlasPtr atlas = new ImFontAtlasPtr(ImGuiNative.ImFontAtlas_ImFontAtlas());
            Entry result = null;

            try
            {
                if (atlas.NativePtr == null)
                {
                    Debug.LogError("[FontAtlasCache] Unable to allocate a native shared font atlas.");
                    return null;
                }

                buildContext = ImGui.CreateContext(atlas);
                if (buildContext == IntPtr.Zero)
                {
                    Debug.LogError("[FontAtlasCache] Unable to create the temporary ImGui font-build context.");
                    return null;
                }

                ImGuiNative.igSetCurrentContext(buildContext);

                Entry entry = new Entry
                {
                    Key = key,
                    FontScale = fontScale,
                    Atlas = atlas
                };

                ImGuiIOPtr io = ImGui.GetIO();
                using (FuFontLoadResources fontResources = FuFontLoader.LoadFonts(
                    io,
                    fontConfig,
                    fontScale,
                    streamingAssetsPath,
                    entry.Fonts,
                    out entry.DefaultFont))
                {
                    // The shared atlas takes permanent ownership of font data; glyph ranges are build-scoped.
                    if (!io.Fonts.Build())
                    {
                        Debug.LogError($"[FontAtlasCache] Failed to build shared font atlas for scale {fontScale:0.###}.");
                        return null;
                    }
                }

                result = entry;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                if (buildContext != IntPtr.Zero)
                {
                    ImGuiNative.igSetCurrentContext(buildContext);
                    ImGui.DestroyContext(buildContext);
                }

                ImGuiNative.igSetCurrentContext(previousContext);
                if (result == null && atlas.NativePtr != null)
                {
                    atlas.Destroy();
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the stable shared-atlas cache key for a configuration and scale.
        /// </summary>
        /// <param name="fontConfig">Font configuration.</param>
        /// <param name="fontScale">Quantized font scale.</param>
        /// <param name="streamingAssetsPath">Root font path.</param>
        /// <returns>Atlas cache key.</returns>
        private static string GetKey(FontConfig fontConfig, float fontScale, string streamingAssetsPath)
        {
            // Shared native and baked Unity atlases use the same content-derived identity.
            return FuFontAtlasCache.GetAtlasCacheKey(fontConfig, fontScale, streamingAssetsPath);
        }
    }
}
