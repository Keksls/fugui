using System;
using UnityEngine;

namespace Fu
{
        /// <summary>
        /// Creation settings for a 3D Fugui window.
        /// PanelSize is the world/local size of the 3D panel, Resolution is the render texture and ImGui context size.
        /// </summary>
        [Serializable]
        public struct Fu3DWindowSettings
        {
            #region State
            public Vector2 PanelSize;
            public Vector2Int Resolution;
            public bool ScaleResolutionWithPanel;
            public bool MatchResolutionToPanelAspect;
            public Vector2 ReferencePanelSize;
            public Vector2Int ReferenceResolution;
            public Vector2Int MinResolution;
            public Vector2Int MaxResolution;
            public float ContextScale;
            public float FontScale;
            public float PanelDepth;
            public float PanelCurve;
            public FuContainerScaleConfig ContainerScaleConfig;
            #endregion

            #region Methods
            /// <summary>
            /// Returns the fixed resolution result.
            /// </summary>
            /// <param name="panelSize">The panel Size value.</param>
            /// <param name="resolution">The resolution value.</param>
            /// <param name="contextScale">The context Scale value.</param>
            /// <param name="fontScale">The font Scale value.</param>
            /// <param name="panelDepth">The panel Depth value.</param>
            /// <param name="panelCurve">Horizontal panel curve angle in degrees.</param>
            /// <returns>The result of the operation.</returns>
            public static Fu3DWindowSettings FixedResolution(Vector2 panelSize, Vector2Int resolution, float contextScale = 1f, float fontScale = 1f, float panelDepth = 0.01f, float panelCurve = 0f)
            {
                Fu3DWindowSettings settings = new Fu3DWindowSettings
                {
                    PanelSize = panelSize,
                    Resolution = resolution,
                    ScaleResolutionWithPanel = false,
                    MatchResolutionToPanelAspect = false,
                    ReferencePanelSize = panelSize,
                    ReferenceResolution = resolution,
                    MinResolution = Vector2Int.one,
                    MaxResolution = Vector2Int.zero,
                    ContextScale = contextScale,
                    FontScale = fontScale,
                    PanelDepth = panelDepth,
                    PanelCurve = panelCurve,
                    ContainerScaleConfig = FuContainerScaleConfig.Disabled(contextScale, fontScale)
                };
                settings.Sanitize();
                return settings;
            }

            /// <summary>
            /// Returns the scaled resolution with panel result.
            /// </summary>
            /// <param name="panelSize">The panel Size value.</param>
            /// <param name="referenceResolution">The reference Resolution value.</param>
            /// <param name="referencePanelSize">The reference Panel Size value.</param>
            /// <param name="contextScale">The context Scale value.</param>
            /// <param name="fontScale">The font Scale value.</param>
            /// <param name="minResolution">The min Resolution value.</param>
            /// <param name="maxResolution">The max Resolution value.</param>
            /// <param name="panelDepth">The panel Depth value.</param>
            /// <param name="panelCurve">Horizontal panel curve angle in degrees.</param>
            /// <returns>The result of the operation.</returns>
            public static Fu3DWindowSettings ScaledResolutionWithPanel(Vector2 panelSize, Vector2Int referenceResolution, Vector2 referencePanelSize, float contextScale = 1f, float fontScale = 1f, Vector2Int? minResolution = null, Vector2Int? maxResolution = null, float panelDepth = 0.01f, float panelCurve = 0f)
            {
                Fu3DWindowSettings settings = FixedResolution(panelSize, referenceResolution, contextScale, fontScale, panelDepth, panelCurve);
                settings.ScaleResolutionWithPanel = true;
                settings.MatchResolutionToPanelAspect = false;
                settings.ReferencePanelSize = referencePanelSize;
                settings.ReferenceResolution = referenceResolution;
                settings.MinResolution = minResolution ?? Vector2Int.one;
                settings.MaxResolution = maxResolution ?? Vector2Int.zero;
                settings.Sanitize();
                return settings;
            }

            /// <summary>
            /// Returns the fixed resolution matching panel aspect result.
            /// </summary>
            /// <param name="panelSize">The panel Size value.</param>
            /// <param name="referenceResolution">The reference Resolution value.</param>
            /// <param name="referencePanelSize">The reference Panel Size value.</param>
            /// <param name="contextScale">The context Scale value.</param>
            /// <param name="fontScale">The font Scale value.</param>
            /// <param name="minResolution">The min Resolution value.</param>
            /// <param name="maxResolution">The max Resolution value.</param>
            /// <param name="panelDepth">The panel Depth value.</param>
            /// <param name="panelCurve">Horizontal panel curve angle in degrees.</param>
            /// <returns>The result of the operation.</returns>
            public static Fu3DWindowSettings FixedResolutionMatchingPanelAspect(Vector2 panelSize, Vector2Int referenceResolution, Vector2 referencePanelSize, float contextScale = 1f, float fontScale = 1f, Vector2Int? minResolution = null, Vector2Int? maxResolution = null, float panelDepth = 0.01f, float panelCurve = 0f)
            {
                Fu3DWindowSettings settings = FixedResolution(panelSize, referenceResolution, contextScale, fontScale, panelDepth, panelCurve);
                settings.MatchResolutionToPanelAspect = true;
                settings.ReferencePanelSize = referencePanelSize;
                settings.ReferenceResolution = referenceResolution;
                settings.MinResolution = minResolution ?? Vector2Int.one;
                settings.MaxResolution = maxResolution ?? Vector2Int.zero;
                settings.Sanitize();
                return settings;
            }

            /// <summary>
            /// Runs the sanitize workflow.
            /// </summary>
            public void Sanitize()
            {
                PanelSize = new Vector2(
                    Mathf.Max(0.0001f, Mathf.Abs(PanelSize.x)),
                    Mathf.Max(0.0001f, Mathf.Abs(PanelSize.y)));
                ReferencePanelSize = new Vector2(
                    Mathf.Max(0.0001f, Mathf.Abs(ReferencePanelSize.x > 0f ? ReferencePanelSize.x : PanelSize.x)),
                    Mathf.Max(0.0001f, Mathf.Abs(ReferencePanelSize.y > 0f ? ReferencePanelSize.y : PanelSize.y)));
                Resolution = new Vector2Int(
                    Mathf.Max(1, Resolution.x),
                    Mathf.Max(1, Resolution.y));
                ReferenceResolution = new Vector2Int(
                    Mathf.Max(1, ReferenceResolution.x > 0 ? ReferenceResolution.x : Resolution.x),
                    Mathf.Max(1, ReferenceResolution.y > 0 ? ReferenceResolution.y : Resolution.y));
                MinResolution = new Vector2Int(
                    Mathf.Max(1, MinResolution.x),
                    Mathf.Max(1, MinResolution.y));

                int maxTextureSize = Mathf.Max(1, SystemInfo.maxTextureSize);
                MaxResolution = new Vector2Int(
                    MaxResolution.x > 0 ? MaxResolution.x : maxTextureSize,
                    MaxResolution.y > 0 ? MaxResolution.y : maxTextureSize);
                MaxResolution = new Vector2Int(
                    Mathf.Max(MinResolution.x, MaxResolution.x),
                    Mathf.Max(MinResolution.y, MaxResolution.y));

                ContextScale = Mathf.Max(0.0001f, ContextScale);
                FontScale = Mathf.Max(0.0001f, FontScale);
                PanelDepth = Mathf.Max(0.0001f, PanelDepth);
                PanelCurve = Mathf.Clamp(PanelCurve, 0f, 359.9f);
                if (ScaleResolutionWithPanel)
                {
                    Resolution = ComputeResolution(PanelSize);
                }
                else if (MatchResolutionToPanelAspect)
                {
                    Resolution = ComputeAspectMatchedResolution(PanelSize);
                }
                if (ContainerScaleConfig.BaseScale <= 0f || ContainerScaleConfig.BaseFontScale <= 0f)
                {
                    ContainerScaleConfig = FuContainerScaleConfig.Disabled(ContextScale, FontScale);
                }
                else
                {
                    ContainerScaleConfig.Sanitize();
                }
            }

            /// <summary>
            /// Returns the compute resolution result.
            /// </summary>
            /// <param name="panelSize">The panel Size value.</param>
            /// <returns>The result of the operation.</returns>
            public Vector2Int ComputeResolution(Vector2 panelSize)
            {
                panelSize = new Vector2(
                    Mathf.Max(0.0001f, Mathf.Abs(panelSize.x)),
                    Mathf.Max(0.0001f, Mathf.Abs(panelSize.y)));

                Vector2 referencePanelSize = new Vector2(
                    Mathf.Max(0.0001f, Mathf.Abs(ReferencePanelSize.x)),
                    Mathf.Max(0.0001f, Mathf.Abs(ReferencePanelSize.y)));

                Vector2 scale = new Vector2(
                    panelSize.x / referencePanelSize.x,
                    panelSize.y / referencePanelSize.y);

                Vector2Int resolution = new Vector2Int(
                    Mathf.RoundToInt(ReferenceResolution.x * scale.x),
                    Mathf.RoundToInt(ReferenceResolution.y * scale.y));

                return new Vector2Int(
                    Mathf.Clamp(resolution.x, MinResolution.x, MaxResolution.x),
                    Mathf.Clamp(resolution.y, MinResolution.y, MaxResolution.y));
            }

            /// <summary>
            /// Returns the compute aspect matched resolution result.
            /// </summary>
            /// <param name="panelSize">The panel Size value.</param>
            /// <returns>The result of the operation.</returns>
            public Vector2Int ComputeAspectMatchedResolution(Vector2 panelSize)
            {
                panelSize = new Vector2(
                    Mathf.Max(0.0001f, Mathf.Abs(panelSize.x)),
                    Mathf.Max(0.0001f, Mathf.Abs(panelSize.y)));

                Vector2 referencePanelSize = new Vector2(
                    Mathf.Max(0.0001f, Mathf.Abs(ReferencePanelSize.x)),
                    Mathf.Max(0.0001f, Mathf.Abs(ReferencePanelSize.y)));

                float panelAspect = panelSize.x / panelSize.y;
                float referencePanelAspect = referencePanelSize.x / referencePanelSize.y;
                float referenceResolutionAspect = ReferenceResolution.x / (float)ReferenceResolution.y;
                float targetAspect = referenceResolutionAspect * (panelAspect / referencePanelAspect);
                float referenceArea = Mathf.Max(1f, ReferenceResolution.x * ReferenceResolution.y);
                int width = Mathf.RoundToInt(Mathf.Sqrt(referenceArea * targetAspect));
                int height = Mathf.RoundToInt(width / targetAspect);

                Vector2Int resolution = new Vector2Int(
                    Mathf.Max(1, width),
                    Mathf.Max(1, height));

                return new Vector2Int(
                    Mathf.Clamp(resolution.x, MinResolution.x, MaxResolution.x),
                    Mathf.Clamp(resolution.y, MinResolution.y, MaxResolution.y));
            }
            #endregion
        }
}
