using System;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Optional runtime manipulator for a Fu3DWindowBehaviour.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Fu3DWindowBehaviour))]
    public class Fu3DWindowManipulator : MonoBehaviour
    {
        #region Nested Types
        /// <summary>
        /// Facing constraint used while moving or floating a 3D window.
        /// </summary>
        public enum Fu3DWindowFacingConstraint
        {
            None,
            YawOnly,
            Full
        }

        /// <summary>
        /// Position of the generated grab handle around the 3D window.
        /// </summary>
        public enum Fu3DWindowManipulatorHandlePosition
        {
            Top,
            Bottom
        }
        #endregion

        #region State
        [Tooltip("Allow the 3D window to be grabbed and moved at runtime.")]
        public bool RuntimeMovable = true;

        [Tooltip("Create an external grab bar outside the window so grabbing does not steal UI clicks.")]
        public bool CreateGrabHandle = true;

        [Tooltip("Target the panel faces. When empty, the main camera is used.")]
        public Transform FacingTarget;

        [Tooltip("How the panel rotates toward the facing target.")]
        public Fu3DWindowFacingConstraint FacingConstraint = Fu3DWindowFacingConstraint.YawOnly;

        [Tooltip("Rotate the panel toward the facing target while it is being dragged.")]
        public bool FaceTargetWhileDragging = true;

        [Tooltip("Keep rotating the panel toward the facing target while it is not anchored.")]
        public bool FaceTargetWhenUnanchored = false;

        [Tooltip("Anchor the panel in world space when the grab is released.")]
        public bool AnchorOnRelease = true;

        [Tooltip("Start in anchored world-space mode.")]
        public bool StartAnchored = true;

        [Tooltip("Where the generated grab bar is placed relative to the window.")]
        public Fu3DWindowManipulatorHandlePosition GrabHandlePosition = Fu3DWindowManipulatorHandlePosition.Top;

        [Tooltip("World axis used by YawOnly facing.")]
        public Vector3 WorldUpAxis = Vector3.up;

        [Tooltip("Grab bar width as a ratio of the panel width.")]
        [Range(0.1f, 1f)]
        public float GrabHandleWidthRatio = 0.38f;

        [Tooltip("Minimum grab bar width in world units.")]
        public float GrabHandleMinWidth = 0.18f;

        [Tooltip("Maximum grab bar width in world units.")]
        public float GrabHandleMaxWidth = 0.55f;

        [Tooltip("Grab bar height in world units.")]
        public float GrabHandleHeight = 0.045f;

        [Tooltip("Distance between the panel edge and the grab bar.")]
        [UnityEngine.Serialization.FormerlySerializedAs("GrabHandleTopOffset")]
        public float GrabHandleEdgeOffset = 0.035f;

        [Tooltip("Front offset applied to the generated grab bar.")]
        public float GrabHandleFrontOffset = -0.05f;

        public bool IsAnchored => _anchored;
        public bool IsDragging => _dragging;
        public event Action<Fu3DWindowManipulator> OnMoveStarted;
        public event Action<Fu3DWindowManipulator> OnMoveEnded;

        private Fu3DWindowBehaviour _windowBehaviour;
        private GameObject _grabHandle;
        private Mesh _grabHandleMesh;
        private Material _grabHandleMaterial;
        private MaterialPropertyBlock _grabHandlePropertyBlock;
        private bool _anchored;
        private bool _dragging;
        private bool _grabHandleHovered;
        private string _activeRaycasterID;
        [SerializeField, HideInInspector]
        private string _grabHandleID;
        private float _grabDistance;
        private Vector3 _grabOffset;
        private const int GrabHandleSegments = 8;
        #endregion

        #region Methods
        /// <summary>
        /// Handles the Awake event.
        /// </summary>
        private void Awake()
        {
            _windowBehaviour = GetComponent<Fu3DWindowBehaviour>();
            _anchored = StartAnchored;
        }

        /// <summary>
        /// Handles the Enable event.
        /// </summary>
        private void OnEnable()
        {
            _windowBehaviour = GetComponent<Fu3DWindowBehaviour>();
            _anchored = StartAnchored;
            if (Fugui.Themes != null)
            {
                Fugui.Themes.OnThemeSet -= ThemeManager_OnThemeSet;
                Fugui.Themes.OnThemeSet += ThemeManager_OnThemeSet;
            }
        }

        /// <summary>
        /// Handles the Disable event.
        /// </summary>
        private void OnDisable()
        {
            finishDrag(false);
            setContainerResizeBlocked(false);
            if (Fugui.Themes != null)
            {
                Fugui.Themes.OnThemeSet -= ThemeManager_OnThemeSet;
            }
        }

        /// <summary>
        /// Handles the LateUpdate event.
        /// </summary>
        private void LateUpdate()
        {
            if (_windowBehaviour == null)
            {
                _windowBehaviour = GetComponent<Fu3DWindowBehaviour>();
            }

            updateGrabHandle();

            if (!RuntimeMovable)
            {
                finishDrag(false);
                setContainerResizeBlocked(false);
                return;
            }

            if (_dragging)
            {
                continueDrag();
                return;
            }

            if (isContainerRuntimeResizing())
            {
                _grabHandleHovered = false;
                updateGrabHandleVisualState();
                return;
            }

            bool grabHovered = tryGetGrabInput(out InputState inputState);
            _grabHandleHovered = grabHovered;
            setContainerResizeBlocked(grabHovered || _dragging);
            updateGrabHandleVisualState();

            if (grabHovered && inputState.MouseButtons[0])
            {
                startDrag(inputState.RaycasterID);
                return;
            }

            if (!_anchored && FaceTargetWhenUnanchored)
            {
                ApplyFacing();
            }
        }

        /// <summary>
        /// Anchors the current panel transform in world space.
        /// </summary>
        public void Anchor()
        {
            _anchored = true;
        }

        /// <summary>
        /// Allows the panel to keep its configured floating facing behavior.
        /// </summary>
        public void Unanchor()
        {
            _anchored = false;
        }

        /// <summary>
        /// Applies the configured facing constraint immediately.
        /// </summary>
        public void ApplyFacing()
        {
            if (FacingConstraint == Fu3DWindowFacingConstraint.None)
            {
                applyTransformToContainer();
                return;
            }

            if (tryGetFacingRotation(transform.position, out Quaternion rotation))
            {
                transform.rotation = rotation;
            }

            applyTransformToContainer();
        }

        /// <summary>
        /// Starts a runtime grab from a Fugui raycaster.
        /// </summary>
        /// <param name="raycasterID">Raycaster ID.</param>
        private void startDrag(string raycasterID)
        {
            if (string.IsNullOrEmpty(raycasterID) ||
                !FuRaycasting.TryGetRaycaster(raycasterID, out FuRaycaster raycaster))
            {
                return;
            }

            Ray ray = raycaster.GetRay();
            Vector3 hitPoint = raycaster.RaycastThisFrame ? raycaster.Hit.point : transform.position;
            float directionSqrMagnitude = Mathf.Max(0.0001f, ray.direction.sqrMagnitude);
            _grabDistance = Mathf.Max(0.0001f, Vector3.Dot(hitPoint - ray.origin, ray.direction) / directionSqrMagnitude);
            _grabOffset = transform.position - hitPoint;
            _activeRaycasterID = raycasterID;
            _dragging = true;
            _grabHandleHovered = true;
            _anchored = false;
            setContainerResizeBlocked(true);
            updateGrabHandleVisualState();
            OnMoveStarted?.Invoke(this);
        }

        /// <summary>
        /// Continues the current runtime grab.
        /// </summary>
        private void continueDrag()
        {
            if (!FuRaycasting.TryGetRaycaster(_activeRaycasterID, out FuRaycaster raycaster) ||
                !raycaster.MouseButton0())
            {
                finishDrag(true);
                return;
            }

            Ray ray = raycaster.GetRay();
            Vector3 newPosition = ray.GetPoint(_grabDistance) + _grabOffset;
            transform.position = newPosition;

            if (FaceTargetWhileDragging)
            {
                ApplyFacing();
            }
            else
            {
                applyTransformToContainer();
            }
        }

        /// <summary>
        /// Ends the current runtime grab.
        /// </summary>
        /// <param name="invokeEvent">Whether to invoke the move-ended event.</param>
        private void finishDrag(bool invokeEvent)
        {
            if (!_dragging)
            {
                return;
            }

            _dragging = false;
            _activeRaycasterID = null;
            _grabHandleHovered = false;
            if (AnchorOnRelease)
            {
                _anchored = true;
            }
            setContainerResizeBlocked(false);
            updateGrabHandleVisualState();
            applyTransformToContainer();

            if (invokeEvent)
            {
                OnMoveEnded?.Invoke(this);
            }
        }

        /// <summary>
        /// Reads input from the generated grab handle.
        /// </summary>
        /// <param name="inputState">Current input state.</param>
        /// <returns>True if the grab handle is hovered.</returns>
        private bool tryGetGrabInput(out InputState inputState)
        {
            inputState = default;
            if (_grabHandle == null || !_grabHandle.activeSelf)
            {
                return false;
            }

            inputState = FuRaycasting.GetInputState(getGrabHandleID(), _grabHandle);
            return inputState.Hovered;
        }

        /// <summary>
        /// Creates and updates the optional grab handle.
        /// </summary>
        private void updateGrabHandle()
        {
            if (!CreateGrabHandle || _windowBehaviour == null || _windowBehaviour.Container == null || _windowBehaviour.Container.IsClosed)
            {
                setGrabHandleActive(false);
                return;
            }

            GameObject panelObject = _windowBehaviour.Container.PanelGameObject;
            if (panelObject == null)
            {
                setGrabHandleActive(false);
                return;
            }

            ensureGrabHandle(panelObject);
            updateGrabHandleTransform(panelObject);
            setGrabHandleActive(RuntimeMovable);
        }

        /// <summary>
        /// Ensures the generated grab handle exists.
        /// </summary>
        /// <param name="panelObject">Current panel object.</param>
        private void ensureGrabHandle(GameObject panelObject)
        {
            if (_grabHandle == null)
            {
                _grabHandle = new GameObject(getGrabHandleID());
            }

            _grabHandle.name = getGrabHandleID();
            _grabHandle.layer = panelObject.layer;
            _grabHandle.transform.SetParent(panelObject.transform, false);

            MeshFilter meshFilter = _grabHandle.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = _grabHandle.AddComponent<MeshFilter>();
            }

            if (_grabHandleMesh == null)
            {
                _grabHandleMesh = new Mesh
                {
                    name = getGrabHandleID() + "_Mesh"
                };
            }
            meshFilter.sharedMesh = _grabHandleMesh;

            MeshRenderer renderer = _grabHandle.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = _grabHandle.AddComponent<MeshRenderer>();
            }
            renderer.sharedMaterial = getGrabHandleMaterial();
            setRendererColor(renderer, getGrabHandleColor());

            BoxCollider collider = _grabHandle.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = _grabHandle.AddComponent<BoxCollider>();
            }
        }

        /// <summary>
        /// Updates the generated grab handle transform, mesh and collider.
        /// </summary>
        /// <param name="panelObject">Current panel object.</param>
        private void updateGrabHandleTransform(GameObject panelObject)
        {
            Vector2 localSize = _windowBehaviour.Container.LocalSize;
            float width = Mathf.Clamp(localSize.x * GrabHandleWidthRatio, GrabHandleMinWidth, GrabHandleMaxWidth);
            float height = Mathf.Max(0.005f, GrabHandleHeight);
            float edgeOffset = Mathf.Max(0f, GrabHandleEdgeOffset);
            float y = GrabHandlePosition == Fu3DWindowManipulatorHandlePosition.Bottom
                ? -edgeOffset - height * 0.5f
                : localSize.y + edgeOffset + height * 0.5f;
            Vector2 flatPosition = new Vector2(0f, y);

            FuPanelMesh panelMesh = panelObject.GetComponent<FuPanelMesh>();
            _grabHandle.transform.localPosition = panelMesh != null
                ? panelMesh.GetSurfaceLocalPosition(flatPosition, GrabHandleFrontOffset)
                : new Vector3(flatPosition.x, flatPosition.y, GrabHandleFrontOffset);
            _grabHandle.transform.localRotation = Quaternion.identity;
            _grabHandle.transform.localScale = Vector3.one;
            _grabHandle.layer = panelObject.layer;

            updateGrabHandleMesh(width, height);

            BoxCollider collider = _grabHandle.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.center = Vector3.zero;
                collider.size = new Vector3(width, height, Mathf.Max(0.01f, _windowBehaviour.Depth + 0.04f));
                collider.enabled = RuntimeMovable;
            }

            updateGrabHandleVisualState();
        }

        /// <summary>
        /// Updates the rounded grab handle mesh.
        /// </summary>
        /// <param name="width">Handle width.</param>
        /// <param name="height">Handle height.</param>
        private void updateGrabHandleMesh(float width, float height)
        {
            if (_grabHandleMesh == null)
            {
                return;
            }

            int segments = Mathf.Max(3, GrabHandleSegments);
            int halfSegments = segments;
            int vertexCount = (halfSegments + 1) * 2 + 1;
            Vector3[] vertices = new Vector3[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            int[] triangles = new int[(vertexCount - 1) * 3];
            float radius = height * 0.5f;
            float halfStraight = Mathf.Max(0f, width * 0.5f - radius);

            vertices[0] = Vector3.zero;
            normals[0] = -Vector3.forward;
            uvs[0] = new Vector2(0.5f, 0.5f);

            int index = 1;
            for (int i = 0; i <= halfSegments; i++)
            {
                float angle = Mathf.Lerp(Mathf.PI * 0.5f, Mathf.PI * 1.5f, (float)i / halfSegments);
                vertices[index++] = new Vector3(-halfStraight + Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            }

            for (int i = 0; i <= halfSegments; i++)
            {
                float angle = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, (float)i / halfSegments);
                vertices[index++] = new Vector3(halfStraight + Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            }

            for (int i = 1; i < vertices.Length; i++)
            {
                normals[i] = -Vector3.forward;
                uvs[i] = new Vector2(
                    Mathf.InverseLerp(-width * 0.5f, width * 0.5f, vertices[i].x),
                    Mathf.InverseLerp(-height * 0.5f, height * 0.5f, vertices[i].y));
            }

            int triangle = 0;
            for (int i = 1; i < vertices.Length; i++)
            {
                int next = i == vertices.Length - 1 ? 1 : i + 1;
                triangles[triangle++] = 0;
                triangles[triangle++] = next;
                triangles[triangle++] = i;
            }

            _grabHandleMesh.Clear();
            _grabHandleMesh.vertices = vertices;
            _grabHandleMesh.normals = normals;
            _grabHandleMesh.uv = uvs;
            _grabHandleMesh.triangles = triangles;
            _grabHandleMesh.RecalculateBounds();
        }

        /// <summary>
        /// Sets grab handle active state.
        /// </summary>
        /// <param name="active">Whether the handle should be active.</param>
        private void setGrabHandleActive(bool active)
        {
            if (_grabHandle != null)
            {
                _grabHandle.SetActive(active);
            }
        }

        /// <summary>
        /// Updates the grab handle color from current hover/active state.
        /// </summary>
        private void updateGrabHandleVisualState()
        {
            if (_grabHandle == null)
            {
                return;
            }

            MeshRenderer renderer = _grabHandle.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                setRendererColor(renderer, getGrabHandleColor());
            }
        }

        /// <summary>
        /// Returns the themed grab handle color.
        /// </summary>
        /// <returns>Theme color.</returns>
        private Color getGrabHandleColor()
        {
            if (_dragging)
            {
                return getThemeColor(FuColors.HighlightActive, new Color(0f, 0.58f, 0.8f, 1f));
            }

            if (_grabHandleHovered)
            {
                return getThemeColor(FuColors.HighlightHovered, new Color(0f, 0.65f, 0.9f, 1f));
            }

            return getThemeColor(FuColors.Highlight, new Color(0f, 0.72f, 1f, 1f));
        }

        /// <summary>
        /// Returns a Fugui theme color with a safe fallback.
        /// </summary>
        /// <param name="color">Theme color key.</param>
        /// <param name="fallback">Fallback color.</param>
        /// <returns>Theme color.</returns>
        private Color getThemeColor(FuColors color, Color fallback)
        {
            if (Fugui.Themes == null || Fugui.Themes.CurrentTheme == null)
            {
                return fallback;
            }

            return Fugui.Themes.GetColor(color);
        }

        /// <summary>
        /// Applies a per-renderer color without duplicating the material.
        /// </summary>
        /// <param name="renderer">Renderer to update.</param>
        /// <param name="color">Color to apply.</param>
        private void setRendererColor(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            if (_grabHandlePropertyBlock == null)
            {
                _grabHandlePropertyBlock = new MaterialPropertyBlock();
            }

            renderer.GetPropertyBlock(_grabHandlePropertyBlock);
            _grabHandlePropertyBlock.SetColor("_BaseColor", color);
            _grabHandlePropertyBlock.SetColor("_Color", color);
            renderer.SetPropertyBlock(_grabHandlePropertyBlock);
        }

        /// <summary>
        /// Blocks or unblocks resize interactions on the attached container.
        /// </summary>
        /// <param name="blocked">Whether resize should be blocked.</param>
        private void setContainerResizeBlocked(bool blocked)
        {
            if (_windowBehaviour == null || _windowBehaviour.Container == null || _windowBehaviour.Container.IsClosed)
            {
                return;
            }

            _windowBehaviour.Container.SetRuntimeResizeBlocked(blocked);
        }

        /// <summary>
        /// Returns whether the attached container is currently resizing.
        /// </summary>
        /// <returns>True when a resize handle owns the pointer.</returns>
        private bool isContainerRuntimeResizing()
        {
            return _windowBehaviour != null &&
                   _windowBehaviour.Container != null &&
                   !_windowBehaviour.Container.IsClosed &&
                   _windowBehaviour.Container.IsRuntimeResizing;
        }

        /// <summary>
        /// Handles Fugui theme changes.
        /// </summary>
        /// <param name="theme">Current theme.</param>
        private void ThemeManager_OnThemeSet(FuTheme theme)
        {
            updateGrabHandleVisualState();
        }

        /// <summary>
        /// Applies this behaviour transform to the active 3D window container immediately.
        /// </summary>
        private void applyTransformToContainer()
        {
            if (_windowBehaviour == null || _windowBehaviour.Container == null || _windowBehaviour.Container.IsClosed)
            {
                return;
            }

            _windowBehaviour.Container.SetPosition(transform.position);
            _windowBehaviour.Container.SetRotation(transform.rotation);
        }

        /// <summary>
        /// Gets a rotation that makes the panel front face the configured target.
        /// </summary>
        /// <param name="position">Panel position.</param>
        /// <param name="rotation">Computed rotation.</param>
        /// <returns>True if a valid rotation was computed.</returns>
        private bool tryGetFacingRotation(Vector3 position, out Quaternion rotation)
        {
            rotation = transform.rotation;
            Transform target = getFacingTarget();
            if (target == null)
            {
                return false;
            }

            Vector3 panelForward = position - target.position;
            if (FacingConstraint == Fu3DWindowFacingConstraint.YawOnly)
            {
                Vector3 up = getWorldUpAxis();
                panelForward = Vector3.ProjectOnPlane(panelForward, up);
                if (panelForward.sqrMagnitude <= 0.000001f)
                {
                    return false;
                }

                rotation = Quaternion.LookRotation(panelForward.normalized, up);
                return true;
            }

            if (panelForward.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            rotation = Quaternion.LookRotation(panelForward.normalized, getWorldUpAxis());
            return true;
        }

        /// <summary>
        /// Returns the facing target, defaulting to the main camera.
        /// </summary>
        /// <returns>Facing target transform.</returns>
        private Transform getFacingTarget()
        {
            if (FacingTarget != null)
            {
                return FacingTarget;
            }

            Camera mainCamera = Camera.main;
            return mainCamera != null ? mainCamera.transform : null;
        }

        /// <summary>
        /// Returns a safe world up axis.
        /// </summary>
        /// <returns>Normalized world up axis.</returns>
        private Vector3 getWorldUpAxis()
        {
            if (WorldUpAxis.sqrMagnitude <= 0.000001f)
            {
                return Vector3.up;
            }

            return WorldUpAxis.normalized;
        }

        /// <summary>
        /// Returns the generated grab handle ID.
        /// </summary>
        /// <returns>Grab handle ID.</returns>
        private string getGrabHandleID()
        {
            if (string.IsNullOrEmpty(_grabHandleID))
            {
                _grabHandleID = "Fu3DWindowManipulator_" + Guid.NewGuid().ToString("N") + "_GrabHandle";
            }

            return _grabHandleID;
        }

        /// <summary>
        /// Returns the grab handle material.
        /// </summary>
        /// <returns>Handle material.</returns>
        private Material getGrabHandleMaterial()
        {
            if (_grabHandleMaterial != null)
            {
                setMaterialColor(_grabHandleMaterial, getThemeColor(FuColors.Highlight, new Color(0f, 0.72f, 1f, 1f)));
                return _grabHandleMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader != null)
            {
                _grabHandleMaterial = new Material(shader);
            }
            else if (Fugui.Settings != null && Fugui.Settings.UIPanelMaterial != null)
            {
                _grabHandleMaterial = Instantiate(Fugui.Settings.UIPanelMaterial);
            }

            setMaterialColor(_grabHandleMaterial, getThemeColor(FuColors.Highlight, new Color(0f, 0.72f, 1f, 1f)));
            return _grabHandleMaterial;
        }

        /// <summary>
        /// Sets a material color on common Unity shader properties.
        /// </summary>
        /// <param name="material">Material to update.</param>
        /// <param name="color">Color to apply.</param>
        private void setMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        /// <summary>
        /// Handles the Destroy event.
        /// </summary>
        private void OnDestroy()
        {
            if (_grabHandle != null)
            {
                Destroy(_grabHandle);
                _grabHandle = null;
            }

            if (_grabHandleMesh != null)
            {
                Destroy(_grabHandleMesh);
                _grabHandleMesh = null;
            }

            if (_grabHandleMaterial != null)
            {
                Destroy(_grabHandleMaterial);
                _grabHandleMaterial = null;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Handles inspector value changes.
        /// </summary>
        private void OnValidate()
        {
            GrabHandleMinWidth = Mathf.Max(0.001f, GrabHandleMinWidth);
            GrabHandleMaxWidth = Mathf.Max(GrabHandleMinWidth, GrabHandleMaxWidth);
            GrabHandleHeight = Mathf.Max(0.001f, GrabHandleHeight);
            GrabHandleEdgeOffset = Mathf.Max(0f, GrabHandleEdgeOffset);
            if (WorldUpAxis.sqrMagnitude <= 0.000001f)
            {
                WorldUpAxis = Vector3.up;
            }

            if (_grabHandleMaterial != null)
            {
                setMaterialColor(_grabHandleMaterial, getThemeColor(FuColors.Highlight, new Color(0f, 0.72f, 1f, 1f)));
            }
            updateGrabHandleVisualState();
        }
#endif
        #endregion
    }
}
