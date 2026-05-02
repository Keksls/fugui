using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fu
{
    /// <summary>
    /// Defines how a container context scale should be computed from the container size.
    /// </summary>
    [Serializable]
    public struct FuContainerScaleConfig
    {
        #region State
        public const float DefaultReferenceDpi = 96f;

        public bool Enabled;
        public Vector2Int ReferenceResolution;
        [Range(0f, 1f)]
        public float MatchWidthOrHeight;
        public float MinScale;
        public float MaxScale;
        public float BaseScale;
        public float BaseFontScale;
        public bool ScaleFont;
        public bool UseDpiScale;
        public float ReferenceDpi;
        #endregion

        #region Methods
        /// <summary>
        /// Returns the disabled result.
        /// </summary>
        /// <param name="baseScale">The base Scale value.</param>
        /// <param name="baseFontScale">The base Font Scale value.</param>
        /// <returns>The result of the operation.</returns>
        public static FuContainerScaleConfig Disabled(float baseScale, float baseFontScale)
        {
            return new FuContainerScaleConfig
            {
                Enabled = false,
                ReferenceResolution = new Vector2Int(1920, 1080),
                MatchWidthOrHeight = 0.5f,
                MinScale = 0.25f,
                MaxScale = 4f,
                BaseScale = Mathf.Max(0.0001f, baseScale),
                BaseFontScale = Mathf.Max(0.0001f, baseFontScale),
                ScaleFont = true,
                UseDpiScale = false,
                ReferenceDpi = DefaultReferenceDpi
            };
        }

        /// <summary>
        /// Returns the reference result.
        /// </summary>
        /// <param name="referenceResolution">The reference Resolution value.</param>
        /// <param name="matchWidthOrHeight">The match Width Or Height value.</param>
        /// <param name="minScale">The min Scale value.</param>
        /// <param name="maxScale">The max Scale value.</param>
        /// <param name="baseScale">The base Scale value.</param>
        /// <param name="baseFontScale">The base Font Scale value.</param>
        /// <param name="scaleFont">The scale Font value.</param>
        /// <param name="useDpiScale">Use display DPI as a lower bound for the automatic scale.</param>
        /// <param name="referenceDpi">DPI that maps to scale 1.</param>
        /// <returns>The result of the operation.</returns>
        public static FuContainerScaleConfig Reference(Vector2Int referenceResolution, float matchWidthOrHeight, float minScale, float maxScale, float baseScale, float baseFontScale, bool scaleFont = true, bool useDpiScale = true, float referenceDpi = DefaultReferenceDpi)
        {
            FuContainerScaleConfig config = Disabled(baseScale, baseFontScale);
            config.Enabled = true;
            config.ReferenceResolution = referenceResolution;
            config.MatchWidthOrHeight = matchWidthOrHeight;
            config.MinScale = minScale;
            config.MaxScale = maxScale;
            config.ScaleFont = scaleFont;
            config.UseDpiScale = useDpiScale;
            config.ReferenceDpi = referenceDpi;
            config.Sanitize();
            return config;
        }

        /// <summary>
        /// Runs the sanitize workflow.
        /// </summary>
        public void Sanitize()
        {
            ReferenceResolution = new Vector2Int(
                Mathf.Max(1, ReferenceResolution.x),
                Mathf.Max(1, ReferenceResolution.y));

            MatchWidthOrHeight = Mathf.Clamp01(MatchWidthOrHeight);
            MinScale = Mathf.Max(0.0001f, MinScale);
            MaxScale = Mathf.Max(MinScale, MaxScale);
            BaseScale = Mathf.Max(0.0001f, BaseScale);
            BaseFontScale = Mathf.Max(0.0001f, BaseFontScale);
            ReferenceDpi = Mathf.Max(1f, ReferenceDpi > 0f ? ReferenceDpi : DefaultReferenceDpi);
        }

        /// <summary>
        /// Returns the compute scale result.
        /// </summary>
        /// <param name="containerSize">The container Size value.</param>
        /// <returns>The result of the operation.</returns>
        public float ComputeScale(Vector2Int containerSize)
        {
            Sanitize();

            float widthScale = Mathf.Max(1, containerSize.x) / (float)ReferenceResolution.x;
            float heightScale = Mathf.Max(1, containerSize.y) / (float)ReferenceResolution.y;
            float logWidthScale = Mathf.Log(widthScale, 2f);
            float logHeightScale = Mathf.Log(heightScale, 2f);
            float scale = Mathf.Pow(2f, Mathf.Lerp(logWidthScale, logHeightScale, MatchWidthOrHeight));
            if (UseDpiScale)
            {
                scale = Mathf.Max(scale, ComputeDpiScale(ReferenceDpi));
            }
            return Mathf.Max(0.0001f, scale);
        }

        /// <summary>
        /// Computes the display-density scale used as a readability floor.
        /// </summary>
        /// <param name="referenceDpi">DPI that maps to scale 1.</param>
        /// <returns>Display DPI scale, never below 1.</returns>
        public static float ComputeDpiScale(float referenceDpi = DefaultReferenceDpi)
        {
            referenceDpi = Mathf.Max(1f, referenceDpi);
            float dpi = Screen.dpi;

#if UNITY_EDITOR
            dpi = Mathf.Max(dpi, DefaultReferenceDpi * EditorGUIUtility.pixelsPerPoint);
#endif

            if (dpi <= 0f)
            {
                return 1f;
            }

            return Mathf.Max(1f, dpi / referenceDpi);
        }
        #endregion
    }

    /// <summary>
    /// Interface that represent what should implement an UI window container
    /// A container is a piece of code that can host UI windows
    /// - Unity main Window Container : should be unique instance
    /// - OpenTK window Container : can be multiples, used for external windows
    /// </summary>
    public interface IFuWindowContainer
    {
        #region State
        public Vector2Int LocalMousePos { get; }
        public FuContext Context { get; }
        public Vector2Int Position { get; }
        public Vector2Int Size { get; }
        public FuKeyboardState Keyboard { get; }
        public FuMouseState Mouse { get; }
        public FuContainerScaleConfig ContainerScaleConfig { get; }
        #endregion

        #region Methods
        /// <summary>
        /// Configure how this container scales its context.
        /// </summary>
        /// <param name="config">Scale configuration.</param>
        public void SetContainerScaleConfig(FuContainerScaleConfig config);

        /// <summary>
        /// Execute a callback on each windows on this container
        /// </summary>
        /// <param name="callback">callback to execute on each windows</param>
        public void OnEachWindow(Action<FuWindow> callback);

        /// <summary>
        /// Try to add an UI window to this container
        /// </summary>
        /// <param name="UIWindow">the window object to add</param>
        /// <returns>true if success</returns>
        public bool TryAddWindow(FuWindow UIWindow);

        /// <summary>
        /// Try to remove an UI window from this container
        /// </summary>
        /// <param name="id">id of the window object to add</param>
        /// <returns>true if success</returns>
        public bool TryRemoveWindow(string id);

        /// <summary>
        /// Whatever this container contains a window
        /// </summary>
        /// <param name="id">ID of the window to check</param>
        /// <returns>true if contains</returns>
        public bool HasWindow(string id);

        /// <summary>
        /// Method that render every UI windows hosted by this container
        /// Must call RenderUIWindo(UIWindow UIWindow) for each hoster windows
        /// </summary>
        public void RenderFuWindows();

        /// <summary>
        /// Methos that render a single UIWindow object
        /// </summary>
        /// <param name="UIWindow">UIWindow object to render</param>
        public void RenderFuWindow(FuWindow UIWindow);

        /// <summary>
        /// Did the container must force UI window position to it self context ?
        /// Unity main WUIWindowContainer must return false, because UI window can move into it
        /// OpenTK container must return false, because the graphic context move instead of the UI window itself
        /// </summary>
        /// <returns>true or false according to the container</returns>
        public bool ForcePos();
        #endregion
    }
}
