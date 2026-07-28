using ImGuiNET;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UTexture = UnityEngine.Texture;

namespace Fu
{
    /// <summary>
    /// Represents the Texture Manager type.
    /// </summary>
    internal class TextureManager
    {
        private readonly Dictionary<IntPtr, UTexture> _textures = new Dictionary<IntPtr, UTexture>();
        private readonly Dictionary<UTexture, IntPtr> _textureIds = new Dictionary<UTexture, IntPtr>();
        private readonly Dictionary<Sprite, SpriteInfo> _spriteData = new Dictionary<Sprite, SpriteInfo>();
        private int _nextTextureId = 1;

        private static readonly Dictionary<string, Texture2D> _atlasTexture = new Dictionary<string, Texture2D>();
        private const int FontAtlasCleanupDelayFrames = 4;
        private string _fontAtlasTextureKey;

        #region Nested Types
        /// <summary>
        /// Represents the Pending Font Atlas Cleanup data structure.
        /// </summary>
        private struct PendingFontAtlasCleanup
        {
            #region State
            public string Key;
            public Texture2D Atlas;
            public int EarliestFrame;
            #endregion
        }
        #endregion

        #region State
        private static readonly List<PendingFontAtlasCleanup> _pendingFontAtlasCleanups = new List<PendingFontAtlasCleanup>();
        #endregion

        /// <summary>
        /// Initializes the initialize font atlas workflow.
        /// </summary>
        /// <param name="io">The io value.</param>
        internal unsafe void InitializeFontAtlas(ImGuiIOPtr io)
        {
            FlushPendingFontAtlasCleanups();

            float fontScale = Fugui.CurrentContext.FontScale;
            string fontAtlasTextureKey = GetCurrentFontAtlasTextureKey();
            if (_fontAtlasTextureKey != fontAtlasTextureKey)
            {
                string previousKey = _fontAtlasTextureKey;
                _fontAtlasTextureKey = fontAtlasTextureKey;
                ReleaseFontAtlasTexture(previousKey);
            }

            if (!_atlasTexture.TryGetValue(fontAtlasTextureKey, out Texture2D atlas) || atlas == null)
            {
                if (!ReferenceEquals(atlas, null))
                {
                    UnregisterTextureFromAllManagers(atlas);
                }

                if (!TryRestorePendingFontAtlas(fontAtlasTextureKey, out atlas))
                {
                    FontConfig fontConfig = Fugui.Settings?.FontConfig;
                    if (!FuFontAtlasCache.TryLoadBakedTexture(fontConfig, fontScale, out atlas))
                    {
                        bool useAlpha8 = fontConfig != null && fontConfig.UseAlpha8FontAtlasTexture;
                        atlas = FuFontAtlasCache.CreateTextureFromAtlas(io.Fonts, $"Fugui Font Atlas {fontScale:0.###}", useAlpha8);
                    }

                    if (atlas == null)
                    {
                        Debug.LogError("[FontAtlasCache] Unable to create or load the font atlas texture.");
                        return;
                    }

                    _atlasTexture[fontAtlasTextureKey] = atlas;
                }
            }

            // register atlas texture
            IntPtr texId = RegisterTexture(atlas);
            io.Fonts.SetTexID(texId);
        }

        /// <summary>
        /// Releases this manager's reference to its current shared GPU font atlas.
        /// </summary>
        internal void ClearFontAtlas()
        {
            // The static atlas texture is destroyed only after the last context releases its key.
            string fontAtlasTextureKey = _fontAtlasTextureKey;
            _fontAtlasTextureKey = null;
            ReleaseFontAtlasTexture(fontAtlasTextureKey);
        }

        /// <summary>
        /// Releases a shared GPU atlas when no live texture manager references its cache key.
        /// </summary>
        /// <param name="fontAtlasTextureKey">Atlas cache key to release.</param>
        private static void ReleaseFontAtlasTexture(string fontAtlasTextureKey)
        {
            // Delayed destruction lets already-submitted render commands finish using the texture.
            if (string.IsNullOrEmpty(fontAtlasTextureKey) || !_atlasTexture.ContainsKey(fontAtlasTextureKey))
            {
                return;
            }

            foreach (FuContext context in Fugui.Contexts.Values)
            {
                if (context.TextureManager != null &&
                    context.TextureManager._fontAtlasTextureKey == fontAtlasTextureKey)
                {
                    return;
                }
            }

            Texture2D atlas = _atlasTexture[fontAtlasTextureKey];
            _atlasTexture.Remove(fontAtlasTextureKey);
            ScheduleFontAtlasCleanup(fontAtlasTextureKey, atlas);
        }

        /// <summary>
        /// Runs the schedule font atlas cleanup workflow.
        /// </summary>
        /// <param name="fontAtlasTextureKey">Content key that can reclaim the atlas before destruction.</param>
        /// <param name="atlas">The atlas value.</param>
        private static void ScheduleFontAtlasCleanup(string fontAtlasTextureKey, Texture2D atlas)
        {
            if (ReferenceEquals(atlas, null) || atlas == null)
            {
                return;
            }

            int earliestFrame = Time.frameCount + FontAtlasCleanupDelayFrames;
            for (int i = 0; i < _pendingFontAtlasCleanups.Count; i++)
            {
                PendingFontAtlasCleanup pending = _pendingFontAtlasCleanups[i];
                if (ReferenceEquals(pending.Atlas, atlas))
                {
                    pending.Key = fontAtlasTextureKey;
                    pending.EarliestFrame = Mathf.Max(pending.EarliestFrame, earliestFrame);
                    _pendingFontAtlasCleanups[i] = pending;
                    return;
                }
            }

            _pendingFontAtlasCleanups.Add(new PendingFontAtlasCleanup
            {
                Key = fontAtlasTextureKey,
                Atlas = atlas,
                EarliestFrame = earliestFrame
            });
        }

        /// <summary>
        /// Reclaims a delayed atlas when its content key becomes active again before destruction.
        /// </summary>
        /// <param name="fontAtlasTextureKey">Atlas cache key requested by a context.</param>
        /// <param name="atlas">Reclaimed Unity atlas texture.</param>
        /// <returns>True when a pending texture was restored.</returns>
        private static bool TryRestorePendingFontAtlas(string fontAtlasTextureKey, out Texture2D atlas)
        {
            // Scale oscillation should reuse the submitted GPU texture instead of reallocating it.
            atlas = null;
            for (int i = _pendingFontAtlasCleanups.Count - 1; i >= 0; i--)
            {
                PendingFontAtlasCleanup pending = _pendingFontAtlasCleanups[i];
                if (!string.Equals(pending.Key, fontAtlasTextureKey, StringComparison.Ordinal))
                {
                    continue;
                }

                _pendingFontAtlasCleanups.RemoveAt(i);
                if (pending.Atlas == null)
                {
                    continue;
                }

                atlas = pending.Atlas;
                _atlasTexture[fontAtlasTextureKey] = atlas;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Runs the flush pending font atlas cleanups workflow.
        /// </summary>
        private static void FlushPendingFontAtlasCleanups()
        {
            for (int i = _pendingFontAtlasCleanups.Count - 1; i >= 0; i--)
            {
                PendingFontAtlasCleanup pending = _pendingFontAtlasCleanups[i];
                if (ReferenceEquals(pending.Atlas, null) || pending.Atlas == null)
                {
                    _pendingFontAtlasCleanups.RemoveAt(i);
                    continue;
                }

                if (Time.frameCount < pending.EarliestFrame)
                {
                    continue;
                }

                if (_atlasTexture.ContainsValue(pending.Atlas))
                {
                    _pendingFontAtlasCleanups.RemoveAt(i);
                    continue;
                }

                UnregisterTextureFromAllManagers(pending.Atlas);
                DestroyOwnedTexture(pending.Atlas);
                _pendingFontAtlasCleanups.RemoveAt(i);
            }
        }
        /// <summary>
        /// Immediately destroys all shared atlas textures owned by the current Fugui session.
        /// </summary>
        internal static void ShutdownSharedResources()
        {
            // Session shutdown cannot depend on a future frame to flush delayed GPU cleanup.
            HashSet<Texture2D> atlases = new HashSet<Texture2D>();
            foreach (Texture2D atlas in _atlasTexture.Values)
            {
                if (!ReferenceEquals(atlas, null))
                {
                    atlases.Add(atlas);
                }
            }

            for (int i = 0; i < _pendingFontAtlasCleanups.Count; i++)
            {
                Texture2D atlas = _pendingFontAtlasCleanups[i].Atlas;
                if (!ReferenceEquals(atlas, null))
                {
                    atlases.Add(atlas);
                }
            }

            _atlasTexture.Clear();
            _pendingFontAtlasCleanups.Clear();

            foreach (Texture2D atlas in atlases)
            {
                UnregisterTextureFromAllManagers(atlas);
                DestroyOwnedTexture(atlas);
            }
        }

        /// <summary>
        /// Runs the shutdown workflow.
        /// </summary>
        public unsafe void Shutdown()
        {
            _textures.Clear();
            _textureIds.Clear();
            _spriteData.Clear();

            string fontAtlasTextureKey = _fontAtlasTextureKey;
            _fontAtlasTextureKey = null;
            ReleaseFontAtlasTexture(fontAtlasTextureKey);

            if (Fugui.CurrentContext == null || !Fugui.CurrentContext.UsesSharedFontAtlas)
            {
                ImGui.GetIO().Fonts.Clear(); // Previous FontDefault reference no longer valid.
            }

            ImGui.GetIO().NativePtr->FontDefault = default; // NULL uses Fonts[0].
        }

        /// <summary>
        /// Runs the prepare frame workflow.
        /// </summary>
        /// <param name="io">The io value.</param>
        internal void PrepareFrame(ImGuiIOPtr io)
        {
            // Delayed atlas destruction advances even when the current font scale remains stable.
            FlushPendingFontAtlasCleanups();
            string fontAtlasTextureKey = GetCurrentFontAtlasTextureKey();
            if (_fontAtlasTextureKey != fontAtlasTextureKey ||
                !_atlasTexture.TryGetValue(fontAtlasTextureKey, out Texture2D atlas) ||
                atlas == null)
            {
                InitializeFontAtlas(io);
            }

            if (!_atlasTexture.TryGetValue(fontAtlasTextureKey, out atlas) || atlas == null)
            {
                return;
            }

            IntPtr id = GetTextureId(atlas);
            io.Fonts.SetTexID(id);
        }

        /// <summary>
        /// Attempts to get texture.
        /// </summary>
        /// <param name="id">The id value.</param>
        /// <param name="texture">The texture value.</param>
        /// <returns>The result of the operation.</returns>
        public bool TryGetTexture(IntPtr id, out UTexture texture)
        {
            if (!_textures.TryGetValue(id, out texture))
            {
                return false;
            }

            if (texture != null)
            {
                return true;
            }

            _textures.Remove(id);
            if (!ReferenceEquals(texture, null))
            {
                _textureIds.Remove(texture);
            }
            return false;
        }

        /// <summary>
        /// Gets the texture id.
        /// </summary>
        /// <param name="texture">The texture value.</param>
        /// <returns>The result of the operation.</returns>
        public IntPtr GetTextureId(UTexture texture)
        {
            if (texture == null)
            {
                return IntPtr.Zero;
            }

            if (_textureIds.TryGetValue(texture, out IntPtr id))
            {
                if (_textures.TryGetValue(id, out UTexture registeredTexture) && registeredTexture == texture)
                {
                    return id;
                }

                _textureIds.Remove(texture);
                _textures.Remove(id);
            }

            return RegisterTexture(texture);
        }

        /// <summary>
        /// Gets the font atlas texture id.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public IntPtr GetFontAtlasTextureId()
        {
            return GetTextureId(_atlasTexture[GetCurrentFontAtlasTextureKey()]);
        }

        /// <summary>
        /// Returns whether a texture is one of Fugui's shared font atlas textures.
        /// </summary>
        /// <param name="texture">Texture to test.</param>
        /// <returns>True when the texture is a registered font atlas.</returns>
        internal bool IsFontAtlasTexture(UTexture texture)
        {
            if (ReferenceEquals(texture, null) || texture == null)
            {
                return false;
            }

            foreach (UTexture atlas in _atlasTexture.Values)
            {
                if (ReferenceEquals(atlas, texture))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the sprite info.
        /// </summary>
        /// <param name="sprite">The sprite value.</param>
        /// <returns>The result of the operation.</returns>
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

        /// <summary>
        /// Returns the register texture result.
        /// </summary>
        /// <param name="texture">The texture value.</param>
        /// <returns>The result of the operation.</returns>
        private IntPtr RegisterTexture(UTexture texture)
        {
            if (texture == null)
            {
                return IntPtr.Zero;
            }

            if (_textureIds.TryGetValue(texture, out IntPtr textureId))
            {
                return textureId;
            }

            IntPtr id;
            do
            {
                id = new IntPtr(_nextTextureId++);
            }
            while (_textures.ContainsKey(id));

            _textures.Add(id, texture);
            _textureIds.Add(texture, id);
            return id;
        }

        private static string GetCurrentFontAtlasTextureKey()
        {
            if (!string.IsNullOrEmpty(Fugui.CurrentContext?.FontAtlasCacheKey))
            {
                return Fugui.CurrentContext.FontAtlasCacheKey;
            }

            float fontScale = Fugui.CurrentContext != null ? Fugui.CurrentContext.FontScale : 1f;
            return FuFontAtlasCache.GetAtlasCacheKey(Fugui.Settings?.FontConfig, fontScale, Application.streamingAssetsPath);
        }

        /// <summary>
        /// Runs the unregister texture from all managers workflow.
        /// </summary>
        /// <param name="texture">The texture value.</param>
        private static void UnregisterTextureFromAllManagers(UTexture texture)
        {
            if (ReferenceEquals(texture, null))
            {
                return;
            }

            foreach (FuContext context in Fugui.Contexts.Values)
            {
                context.TextureManager?.UnregisterTexture(texture);
            }
        }

        /// <summary>
        /// Runs the unregister texture workflow.
        /// </summary>
        /// <param name="texture">The texture value.</param>
        private void UnregisterTexture(UTexture texture)
        {
            if (ReferenceEquals(texture, null))
            {
                return;
            }

            if (_textureIds.TryGetValue(texture, out IntPtr textureId))
            {
                _textures.Remove(textureId);
                _textureIds.Remove(texture);
            }
        }

        /// <summary>
        /// Destroys a runtime-created Unity texture with the API appropriate for the current mode.
        /// </summary>
        /// <param name="texture">Texture owned by Fugui.</param>
        private static void DestroyOwnedTexture(UTexture texture)
        {
            if (ReferenceEquals(texture, null) || texture == null)
            {
                return;
            }

            // Editor previews require immediate destruction because no play-mode frame will flush Destroy.
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(texture);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
