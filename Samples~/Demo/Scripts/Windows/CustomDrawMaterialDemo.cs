#if FU_CUSTOM_MATERIALS_ENABLED
using Fu;
using Fu.Framework;
using System;
using UnityEngine;

/// <summary>
/// Owns the runtime materials and interactive widgets used by the custom draw-material demo.
/// </summary>
internal sealed class CustomDrawMaterialDemo : IDisposable
{
    #region State
    private const string NeonShaderResourcePath = "FuguiCustomMaterials/FuguiDemoNeonPulse";
    private const string GlassShaderResourcePath = "FuguiCustomMaterials/FuguiDemoHolographicGlass";

    private static readonly int GlowProperty = Shader.PropertyToID("_Glow");
    private static readonly int InteractionProperty = Shader.PropertyToID("_Interaction");

    private Material _neonMaterial;
    private Material _glassMaterial;
    private FuDrawMaterial _neonDrawMaterial;
    private FuDrawMaterial _glassDrawMaterial;
    private bool _materialLoadAttempted;
    private float _neonGlow = 1.35f;
    private float _glassGlow = 1.10f;
    private int _neonActivationCount;
    private bool _glassLocked;
    #endregion

    #region Methods
    /// <summary>
    /// Draws the complete interactive custom-material showcase.
    /// </summary>
    /// <param name="enabled">Whether the showcase widgets can be interacted with.</param>
    internal void Draw(bool enabled)
    {
        if (!EnsureMaterials())
        {
            Fugui.Layout.Callout(
                "customDrawMaterialShadersMissing",
                "The custom-material demo shaders could not be loaded from Resources/FuguiCustomMaterials.",
                FuColors.BackgroundDanger);
            return;
        }

        // Keep the sample self-contained: sliders directly drive properties on the caller-owned Unity materials.
        Fugui.Layout.Callout(
            "customDrawMaterialIntro",
            "These buttons are regular DrawList geometry wrapped by PushMaterial(...) / PopMaterial(). Their labels and borders return to Fugui's standard material after the pop.",
            FuColors.BackgroundInfo);

        if (!enabled)
        {
            Fugui.Layout.DisableNextElements();
        }

        Fugui.Layout.Slider("Neon intensity##customDrawMaterialNeonGlow", ref _neonGlow, 0.35f, 2.80f);
        Fugui.Layout.Slider("Glass intensity##customDrawMaterialGlassGlow", ref _glassGlow, 0.35f, 2.80f);

        if (!enabled)
        {
            Fugui.Layout.EnableNextElements();
        }

        _neonMaterial.SetFloat(GlowProperty, _neonGlow);
        _glassMaterial.SetFloat(GlowProperty, _glassGlow);

        DrawMaterialButtons(enabled);
        Fugui.Layout.Dummy(0f, 6f);
        Fugui.Layout.Text($"Neon activations: {_neonActivationCount}    Hologram lock: {(_glassLocked ? "ENGAGED" : "OPEN")}");
    }

    /// <summary>
    /// Releases all runtime materials owned by this showcase.
    /// </summary>
    public void Dispose()
    {
        // FuDrawMaterial does not own Unity materials, so this demo disposes the instances it created itself.
        DestroyRuntimeMaterial(_neonMaterial);
        DestroyRuntimeMaterial(_glassMaterial);
        _neonMaterial = null;
        _glassMaterial = null;
        _neonDrawMaterial = null;
        _glassDrawMaterial = null;
    }

    /// <summary>
    /// Draws the responsive row containing both shader-driven buttons.
    /// </summary>
    /// <param name="enabled">Whether button interaction is enabled.</param>
    private void DrawMaterialButtons(bool enabled)
    {
        // Use one row on wide windows and stack the same public API example on narrow windows.
        float scale = Fugui.CurrentContext.Scale;
        float availableWidth = Mathf.Max(1f, Fugui.Layout.GetAvailableWidth());
        float gap = 10f * scale;
        bool drawSideBySide = availableWidth >= 500f * scale;
        float buttonWidth = drawSideBySide
            ? Mathf.Max(1f, (availableWidth - gap) * 0.5f)
            : availableWidth;

        if (DrawMaterialButton(
            "customDrawMaterialNeonButton",
            buttonWidth,
            "NEON OVERDRIVE",
            "cyan / magenta plasma",
            _neonMaterial,
            _neonDrawMaterial,
            enabled,
            false))
        {
            _neonActivationCount++;
        }

        if (drawSideBySide)
        {
            Fugui.Layout.SameLine();
        }

        if (DrawMaterialButton(
            "customDrawMaterialGlassButton",
            buttonWidth,
            _glassLocked ? "HOLOGRAM LOCKED" : "HOLOGRAPHIC GLASS",
            _glassLocked ? "click to release" : "aurora / grid / sweep",
            _glassMaterial,
            _glassDrawMaterial,
            enabled,
            _glassLocked))
        {
            _glassLocked = !_glassLocked;
        }
    }

    /// <summary>
    /// Draws one interactive button whose background uses a caller-selected shader.
    /// </summary>
    /// <param name="id">Stable Fugui interaction identifier.</param>
    /// <param name="width">Button width in screen pixels.</param>
    /// <param name="title">Primary button label.</param>
    /// <param name="subtitle">Secondary button label.</param>
    /// <param name="material">Unity material updated with interaction state.</param>
    /// <param name="drawMaterial">Fugui material configuration pushed around the background geometry.</param>
    /// <param name="enabled">Whether the button can be interacted with.</param>
    /// <param name="latched">Whether the button is currently latched.</param>
    /// <returns>True when the button was clicked during this frame.</returns>
    private bool DrawMaterialButton(
        string id,
        float width,
        string title,
        string subtitle,
        Material material,
        FuDrawMaterial drawMaterial,
        bool enabled,
        bool latched)
    {
        // The invisible item gives custom draw-list geometry normal Fugui hover, active and click behavior.
        float scale = Fugui.CurrentContext.Scale;
        Vector2 position = Fugui.GetCursorScreenPos();
        Vector2 size = new Vector2(width, 104f * scale);
        bool clicked = Fugui.Layout.InvisibleInteraction(id, size, out bool hovered, out bool active, enabled: enabled);
        float interaction = active ? 1f : hovered ? 0.66f : latched ? 0.48f : 0f;
        material.SetFloat(InteractionProperty, interaction);

        FuDrawList drawList = Fugui.GetCurrentWindowDrawList();
        Vector2 max = position + size;
        float rounding = 12f * scale;
        Vector4 tint = new Vector4(1f, 1f, 1f, enabled ? 1f : 0.42f);

        // Only the image background uses the custom shader; the finally block guarantees a balanced public API scope.
        drawList.PushMaterial(drawMaterial, Texture2D.whiteTexture);
        try
        {
            drawList.AddImageRounded(
                Texture2D.whiteTexture,
                position,
                max,
                Vector2.zero,
                Vector2.one,
                tint,
                rounding,
                FuDrawFlags.RoundCornersAll);
        }
        finally
        {
            drawList.PopMaterial();
        }

        DrawStandardButtonChrome(drawList, position, size, title, subtitle, hovered, active, enabled);
        return clicked;
    }

    /// <summary>
    /// Draws standard Fugui border and text primitives over a shader-driven background.
    /// </summary>
    /// <param name="drawList">Draw list receiving the standard primitives.</param>
    /// <param name="position">Top-left button position.</param>
    /// <param name="size">Button size.</param>
    /// <param name="title">Primary button label.</param>
    /// <param name="subtitle">Secondary button label.</param>
    /// <param name="hovered">Whether the invisible interaction is hovered.</param>
    /// <param name="active">Whether the invisible interaction is active.</param>
    /// <param name="enabled">Whether the button is enabled.</param>
    private static void DrawStandardButtonChrome(
        FuDrawList drawList,
        Vector2 position,
        Vector2 size,
        string title,
        string subtitle,
        bool hovered,
        bool active,
        bool enabled)
    {
        // Drawing after PopMaterial demonstrates that a material scope affects only the commands it encloses.
        float scale = Fugui.CurrentContext.Scale;
        Vector2 max = position + size;
        float rounding = 12f * scale;
        float borderAlpha = active ? 1f : hovered ? 0.82f : 0.52f;
        uint borderColor = Fugui.GetColorU32(new Color(0.84f, 0.98f, 1f, enabled ? borderAlpha : 0.22f));
        uint titleColor = Fugui.GetColorU32(new Color(1f, 1f, 1f, enabled ? 0.98f : 0.48f));
        uint subtitleColor = Fugui.GetColorU32(new Color(0.78f, 0.93f, 1f, enabled ? 0.82f : 0.38f));

        drawList.AddRect(
            position,
            max,
            borderColor,
            rounding,
            FuDrawFlags.RoundCornersAll,
            Mathf.Max(1f, (active ? 2.4f : 1.4f) * scale));
        DrawCenteredText(drawList, title, position + new Vector2(0f, 28f * scale), size.x, titleColor);
        DrawCenteredText(drawList, subtitle, position + new Vector2(0f, 60f * scale), size.x, subtitleColor);
    }

    /// <summary>
    /// Draws one text label horizontally centered inside the supplied width.
    /// </summary>
    /// <param name="drawList">Draw list receiving the text.</param>
    /// <param name="text">Text to draw.</param>
    /// <param name="position">Top-left position of the centering row.</param>
    /// <param name="width">Available row width.</param>
    /// <param name="color">Packed Fugui text color.</param>
    private static void DrawCenteredText(FuDrawList drawList, string text, Vector2 position, float width, uint color)
    {
        // Clamp the offset so long labels still start within the custom button.
        float offset = Mathf.Max(12f * Fugui.CurrentContext.Scale, (width - Fugui.CalcTextSize(text).x) * 0.5f);
        drawList.AddText(position + new Vector2(offset, 0f), color, text);
    }

    /// <summary>
    /// Lazily creates the caller-owned materials used by the showcase.
    /// </summary>
    /// <returns>True when both demo shaders and material configurations are available.</returns>
    private bool EnsureMaterials()
    {
        if (_neonDrawMaterial != null && _glassDrawMaterial != null)
        {
            return true;
        }

        if (_materialLoadAttempted)
        {
            return false;
        }

        // Loading once keeps the steady-state demo path allocation-free.
        _materialLoadAttempted = true;
        Shader neonShader = Resources.Load<Shader>(NeonShaderResourcePath);
        Shader glassShader = Resources.Load<Shader>(GlassShaderResourcePath);
        if (neonShader == null || glassShader == null)
        {
            return false;
        }

        _neonMaterial = CreateRuntimeMaterial(neonShader, "Fugui Demo - Neon Pulse");
        _glassMaterial = CreateRuntimeMaterial(glassShader, "Fugui Demo - Holographic Glass");
        _neonDrawMaterial = new FuDrawMaterial(_neonMaterial);
        _glassDrawMaterial = new FuDrawMaterial(_glassMaterial);
        return true;
    }

    /// <summary>
    /// Creates a hidden runtime material from a demo shader.
    /// </summary>
    /// <param name="shader">Shader used by the runtime material.</param>
    /// <param name="name">Diagnostic material name.</param>
    /// <returns>The newly created caller-owned material.</returns>
    private static Material CreateRuntimeMaterial(Shader shader, string name)
    {
        // HideAndDontSave prevents sample-only runtime instances from polluting the scene or project.
        return new Material(shader)
        {
            name = name,
            hideFlags = HideFlags.HideAndDontSave,
        };
    }

    /// <summary>
    /// Destroys a runtime material using the mode appropriate for the current Unity state.
    /// </summary>
    /// <param name="material">Runtime material to destroy.</param>
    private static void DestroyRuntimeMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        // Unity requires delayed destruction in play mode and immediate destruction while editing.
        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(material);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(material);
        }
    }
    #endregion
}
#endif
