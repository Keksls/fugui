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
#if FU_CUSTOM_MATERIALS_ENABLED
        private Dictionary<IntPtr, FuCustomDrawBinding> _customDrawBindings;
        private Dictionary<FuCustomDrawBindingKey, IntPtr> _customDrawBindingIds;
        private int _nextCustomDrawBindingId = -1;
#endif

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
#if FU_CUSTOM_MATERIALS_ENABLED
            _customDrawBindings?.Clear();
            _customDrawBindingIds?.Clear();
            _customDrawBindings = null;
            _customDrawBindingIds = null;
            _nextCustomDrawBindingId = -1;
#endif

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
#if FU_CUSTOM_MATERIALS_ENABLED
            if (id.ToInt64() < 0 &&
                _customDrawBindings != null &&
                _customDrawBindings.TryGetValue(id, out FuCustomDrawBinding customBinding))
            {
                texture = customBinding.Texture;
                return texture != null;
            }
#endif
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

#if FU_CUSTOM_MATERIALS_ENABLED
        /// <summary>
        /// Returns a stable custom binding identifier while preserving the texture of an existing draw resource.
        /// </summary>
        /// <param name="drawMaterial">Custom material configuration.</param>
        /// <param name="sourceTextureId">Existing draw resource whose texture must be preserved.</param>
        /// <returns>Negative identifier understood by Fugui render backends.</returns>
        internal IntPtr GetCustomDrawBindingId(FuDrawMaterial drawMaterial, IntPtr sourceTextureId)
        {
            // Color-only draw commands use white when no sampleable texture is currently active.
            if (!TryGetTexture(sourceTextureId, out UTexture texture) || texture == null)
            {
                texture = Texture2D.whiteTexture;
            }

            return GetCustomDrawBindingId(drawMaterial, texture);
        }

        /// <summary>
        /// Returns a stable custom binding identifier for an explicit texture.
        /// </summary>
        /// <param name="drawMaterial">Custom material configuration.</param>
        /// <param name="texture">Texture sampled by the custom draw command.</param>
        /// <returns>Negative identifier understood by Fugui render backends.</returns>
        internal IntPtr GetCustomDrawBindingId(FuDrawMaterial drawMaterial, UTexture texture)
        {
            // Material configuration is mandatory while a texture can safely fall back to white.
            if (drawMaterial == null)
            {
                throw new ArgumentNullException(nameof(drawMaterial));
            }

            if (texture == null)
            {
                texture = Texture2D.whiteTexture;
            }

            FuCustomDrawBindingKey key = new FuCustomDrawBindingKey(drawMaterial, texture);
            if (_customDrawBindingIds != null &&
                _customDrawBindingIds.TryGetValue(key, out IntPtr existingId))
            {
                // Reuse the identifier to avoid allocations and unnecessary ImGui command splits.
                return existingId;
            }

            // Allocate registries only when the opt-in feature is used by a context for the first time.
            _customDrawBindings ??= new Dictionary<IntPtr, FuCustomDrawBinding>();
            _customDrawBindingIds ??= new Dictionary<FuCustomDrawBindingKey, IntPtr>();
            IntPtr bindingId = AllocateCustomDrawBindingId();
            _customDrawBindings.Add(bindingId, new FuCustomDrawBinding(drawMaterial, texture));
            _customDrawBindingIds.Add(key, bindingId);
            return bindingId;
        }

        /// <summary>
        /// Attempts to resolve a negative draw resource identifier as a custom material binding.
        /// </summary>
        /// <param name="id">Draw resource identifier to resolve.</param>
        /// <param name="binding">Resolved custom draw binding.</param>
        /// <returns>True when the identifier references a registered custom binding.</returns>
        internal bool TryGetCustomDrawBinding(IntPtr id, out FuCustomDrawBinding binding)
        {
            // Positive identifiers are reserved for the regular Fugui texture registry.
            if (id.ToInt64() >= 0)
            {
                binding = default;
                return false;
            }

            if (_customDrawBindings != null)
            {
                return _customDrawBindings.TryGetValue(id, out binding);
            }

            binding = default;
            return false;
        }

        /// <summary>
        /// Allocates an unused negative identifier without colliding with reserved Fugui commands.
        /// </summary>
        /// <returns>New custom draw binding identifier.</returns>
        private IntPtr AllocateCustomDrawBindingId()
        {
            // Descend through negative ids while leaving special Fugui commands untouched.
            while (_nextCustomDrawBindingId > int.MinValue)
            {
                IntPtr candidate = new IntPtr(_nextCustomDrawBindingId--);
                if (candidate != Fugui.BackdropTextureID &&
                    (_customDrawBindings == null || !_customDrawBindings.ContainsKey(candidate)))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException("Fugui exhausted the custom draw material identifier range.");
        }
#endif

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
