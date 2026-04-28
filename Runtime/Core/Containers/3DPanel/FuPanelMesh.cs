using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Represents the Fu Panel Mesh type.
    /// </summary>
    public class FuPanelMesh : MonoBehaviour
    {
        #region State
        /// <summary>
        /// Fu window rendered on this mesh.
        /// </summary>
        public FuWindow Window { get; internal set; }

        /// <summary>
        /// Whether this mesh should receive raycast input for its window.
        /// </summary>
        public bool CanReceiveInput { get { return Window == null || Window.IsInterractable; } }

        private float _roundEdges = 0.5f;
        private float _roundTopLeft = 0.0f;
        private float _roundTopRight = 0.0f;
        private float _roundBottomLeft = 0.0f;
        private float _roundBottomRight = 0.0f;
        private bool _usePercentage = false;
        private Rect _rect = new Rect(-0.5f, -0.5f, 1f, 1f);
        private float _scale = 1f;
        private int _cornerVertexCount = 8;
        private bool _createUV = true;
        private Vector3[] _Vertices;
        private Vector3[] _Normals;
        private Vector2[] _UV;
        private int[] _Triangles;
        private float _flatWidth = 1f;
        private float _flatHeight = 1f;
        private float _curveAngle = 0f;
        private const float CurveEpsilon = 0.001f;
        private const float MaxCurveAngle = 359.9f;
        private MeshFilter _uiMeshFilter;
        private MeshRenderer _uiMeshRenderer;
        private Mesh _uiMesh;
        private GameObject _panelObject;
        private MeshFilter _panelMeshFilter;
        private MeshRenderer _panelMeshRenderer;
        private Mesh _panelMesh;
        private Material _panelMaterial;
        private Material _panelSourceMaterial;
        #endregion

        #region Methods
        /// <summary>
        /// Creates the mesh.
        /// </summary>
        /// <param name="width">The width value.</param>
        /// <param name="height">The height value.</param>
        /// <param name="scale">The scale value.</param>
        /// <param name="TLRounding">The TLRounding value.</param>
        /// <param name="TRRounding">The TRRounding value.</param>
        /// <param name="BLRounding">The BLRounding value.</param>
        /// <param name="BRRounding">The BRRounding value.</param>
        /// <param name="extrusionDistance">The extrusion Distance value.</param>
        /// <param name="roundTriangles">The round Triangles value.</param>
        /// <param name="UImaterial">The UImaterial value.</param>
        /// <param name="PanelMaterial">The Panel Material value.</param>
        /// <param name="curve">Horizontal curve angle in degrees.</param>
        /// <returns>The result of the operation.</returns>
        public Mesh CreateMesh(float width, float height, float scale, float TLRounding, float TRRounding, float BLRounding, float BRRounding, float extrusionDistance, int roundTriangles, Material UImaterial, Material PanelMaterial, float curve = 0f)
        {
            _roundEdges = 0f;
            _roundTopLeft = TLRounding;
            _roundTopRight = TRRounding;
            _roundBottomLeft = BLRounding;
            _roundBottomRight = BRRounding;
            _cornerVertexCount = roundTriangles;
            _rect = new Rect(-width / 2f, 0, width, height);
            _scale = scale;
            _flatWidth = Mathf.Max(0.0001f, width * scale);
            _flatHeight = Mathf.Max(0.0001f, height * scale);
            _curveAngle = Mathf.Clamp(curve, 0f, MaxCurveAngle);

            Mesh uiMesh = getOrCreateUIMesh(UImaterial);
            Mesh panelMesh = getOrCreatePanelMesh(width, height, scale, PanelMaterial);

            if (_curveAngle > CurveEpsilon)
            {
                // generate curved rounded rectangle
                createCurvedRoundedRectangleMesh(uiMesh, out int[] perimeterIndices);

                // generate curved extrusion
                createExtrudedCurvedMesh(uiMesh, panelMesh, perimeterIndices, extrusionDistance);
            }
            else
            {
                // generate rounded rectangle
                createRoundedRectangleMesh(uiMesh);

                // generate extrusion
                extrudeMesh(uiMesh, panelMesh, new Matrix4x4[] { Matrix4x4.identity, Matrix4x4.Translate(Vector3.forward * extrusionDistance) }, false);
            }

            return uiMesh;
        }

        /// <summary>
        /// Updates the panel materials without rebuilding mesh geometry.
        /// </summary>
        /// <param name="UImaterial">UI material.</param>
        /// <param name="PanelMaterial">Panel backing material.</param>
        /// <param name="width">Panel width.</param>
        /// <param name="height">Panel height.</param>
        /// <param name="scale">Panel scale.</param>
        public void UpdateMaterials(Material UImaterial, Material PanelMaterial, float width, float height, float scale)
        {
            getOrCreateUIMesh(UImaterial);
            getOrCreatePanelMesh(width, height, scale, PanelMaterial);
        }

        /// <summary>
        /// Ensures the reusable UI mesh components exist.
        /// </summary>
        /// <param name="UImaterial">UI material.</param>
        /// <returns>The reusable UI mesh.</returns>
        private Mesh getOrCreateUIMesh(Material UImaterial)
        {
            if (_uiMeshFilter == null)
            {
                _uiMeshFilter = GetComponent<MeshFilter>();
                if (_uiMeshFilter == null)
                {
                    _uiMeshFilter = gameObject.AddComponent<MeshFilter>();
                }
            }

            if (_uiMeshRenderer == null)
            {
                _uiMeshRenderer = GetComponent<MeshRenderer>();
                if (_uiMeshRenderer == null)
                {
                    _uiMeshRenderer = gameObject.AddComponent<MeshRenderer>();
                }
            }

            if (_uiMesh == null)
            {
                _uiMesh = new Mesh
                {
                    name = "Fugui UI Panel"
                };
            }

            _uiMeshFilter.sharedMesh = _uiMesh;
            _uiMeshRenderer.sharedMaterial = UImaterial;
            return _uiMesh;
        }

        /// <summary>
        /// Ensures the reusable backing panel mesh components exist.
        /// </summary>
        /// <param name="width">Panel width.</param>
        /// <param name="height">Panel height.</param>
        /// <param name="scale">Panel scale.</param>
        /// <param name="PanelMaterial">Panel backing material.</param>
        /// <returns>The reusable backing mesh.</returns>
        private Mesh getOrCreatePanelMesh(float width, float height, float scale, Material PanelMaterial)
        {
            if (_panelObject == null)
            {
                Transform existingPanel = transform.Find("panel");
                _panelObject = existingPanel != null ? existingPanel.gameObject : new GameObject("panel");
            }

            _panelObject.transform.SetParent(transform);
            _panelObject.transform.localPosition = Vector3.forward * 0.001f;
            _panelObject.transform.localRotation = Quaternion.identity;
            _panelObject.transform.localScale = Vector3.one;

            if (_panelMeshFilter == null)
            {
                _panelMeshFilter = _panelObject.GetComponent<MeshFilter>();
                if (_panelMeshFilter == null)
                {
                    _panelMeshFilter = _panelObject.AddComponent<MeshFilter>();
                }
            }

            if (_panelMeshRenderer == null)
            {
                _panelMeshRenderer = _panelObject.GetComponent<MeshRenderer>();
                if (_panelMeshRenderer == null)
                {
                    _panelMeshRenderer = _panelObject.AddComponent<MeshRenderer>();
                }
            }

            if (_panelMesh == null)
            {
                _panelMesh = new Mesh
                {
                    name = "Fugui Panel Backing"
                };
            }

            _panelMeshFilter.sharedMesh = _panelMesh;
            updatePanelMaterial(PanelMaterial, width, height, scale);
            return _panelMesh;
        }

        /// <summary>
        /// Updates the reusable panel material instance.
        /// </summary>
        /// <param name="PanelMaterial">Source panel material.</param>
        /// <param name="width">Panel width.</param>
        /// <param name="height">Panel height.</param>
        /// <param name="scale">Panel scale.</param>
        private void updatePanelMaterial(Material PanelMaterial, float width, float height, float scale)
        {
            if (PanelMaterial == null)
            {
                if (_panelMaterial != null)
                {
                    Destroy(_panelMaterial);
                    _panelMaterial = null;
                    _panelSourceMaterial = null;
                }

                _panelMeshRenderer.sharedMaterial = null;
                return;
            }

            if (_panelMaterial == null || _panelSourceMaterial != PanelMaterial)
            {
                if (_panelMaterial != null)
                {
                    Destroy(_panelMaterial);
                }

                _panelSourceMaterial = PanelMaterial;
                _panelMaterial = Instantiate(PanelMaterial);
            }

            _panelMaterial.mainTextureScale = new Vector2(width * scale, height * scale);
            _panelMeshRenderer.sharedMaterial = _panelMaterial;
        }

        /// <summary>
        /// Converts mesh UV coordinates back to the flat local panel coordinates used by Fugui input.
        /// </summary>
        /// <param name="uv">Mesh UV coordinates.</param>
        /// <param name="localPosition">Flat local panel position.</param>
        /// <returns>True if the UV can be converted.</returns>
        public bool TryGetLocalPositionFromUV(Vector2 uv, out Vector2 localPosition)
        {
            localPosition = new Vector2(
                (Mathf.Clamp01(uv.x) - 0.5f) * _flatWidth,
                Mathf.Clamp01(uv.y) * _flatHeight);
            return _flatWidth > 0f && _flatHeight > 0f;
        }

        /// <summary>
        /// Returns a surface point from flat local panel coordinates.
        /// </summary>
        /// <param name="flatLocalPosition">Flat local panel position.</param>
        /// <param name="frontOffset">Offset toward the front face.</param>
        /// <returns>The curved local position.</returns>
        public Vector3 GetSurfaceLocalPosition(Vector2 flatLocalPosition, float frontOffset = 0f)
        {
            Vector3 curvedPosition = bendLocalPosition(new Vector3(flatLocalPosition.x, flatLocalPosition.y, 0f));
            if (Mathf.Abs(frontOffset) <= 0.000001f)
            {
                return curvedPosition;
            }

            return curvedPosition + getCurvedFrontNormal(flatLocalPosition.x) * Mathf.Abs(frontOffset);
        }

        /// <summary>
        /// Returns the create rounded rectangle mesh result.
        /// </summary>
        /// <param name="uiMesh">The ui Mesh value.</param>
        /// <returns>The result of the operation.</returns>
        private Mesh createRoundedRectangleMesh(Mesh uiMesh)
        {
            if (_cornerVertexCount < 2)
                _cornerVertexCount = 2;
            int count = _cornerVertexCount * 4;
            int vCount = count + 1;
            int triCount = count;
            if (_Vertices == null || _Vertices.Length != vCount)
            {
                _Vertices = new Vector3[vCount];
                _Normals = new Vector3[vCount];
            }
            if (_Triangles == null || _Triangles.Length != triCount * 3)
                _Triangles = new int[triCount * 3];
            if (_createUV && (_UV == null || _UV.Length != vCount))
            {
                _UV = new Vector2[vCount];
            }
            if (_createUV)
            {
                _UV[0] = Vector2.one * 0.5f;
            }
            float tl = Mathf.Max(0, _roundTopLeft + _roundEdges);
            float tr = Mathf.Max(0, _roundTopRight + _roundEdges);
            float bl = Mathf.Max(0, _roundBottomLeft + _roundEdges);
            float br = Mathf.Max(0, _roundBottomRight + _roundEdges);
            float a1 = 1f;
            float a2 = 1f;
            float x = 1f;
            float y = 1f;
            Vector2 rs = Vector2.one;
            if (_usePercentage)
            {
                rs = new Vector2(_rect.width, _rect.height) * 0.5f;
                if (_rect.width > _rect.height)
                    a1 = _rect.height / _rect.width;
                else
                    a2 = _rect.width / _rect.height;
                tl = Mathf.Clamp01(tl);
                tr = Mathf.Clamp01(tr);
                bl = Mathf.Clamp01(bl);
                br = Mathf.Clamp01(br);
            }
            else
            {
                x = _rect.width * 0.5f;
                y = _rect.height * 0.5f;
                float width = Mathf.Abs(_rect.width);
                float height = Mathf.Abs(_rect.height);

                float scaleFactor = 1f;

                if (tl + tr > width && tl + tr > 0f)
                    scaleFactor = Mathf.Min(scaleFactor, width / (tl + tr));

                if (bl + br > width && bl + br > 0f)
                    scaleFactor = Mathf.Min(scaleFactor, width / (bl + br));

                if (tl + bl > height && tl + bl > 0f)
                    scaleFactor = Mathf.Min(scaleFactor, height / (tl + bl));

                if (tr + br > height && tr + br > 0f)
                    scaleFactor = Mathf.Min(scaleFactor, height / (tr + br));

                tl *= scaleFactor;
                tr *= scaleFactor;
                bl *= scaleFactor;
                br *= scaleFactor;
            }
            _Vertices[0] = _rect.center * _scale;

            int index = 1;
            AddCorner(index, new Vector2(-x + tl, y - tl), tl, Mathf.PI, Mathf.PI * 0.5f, rs, a1, a2);
            index += _cornerVertexCount;
            AddCorner(index, new Vector2(x - tr, y - tr), tr, Mathf.PI * 0.5f, 0f, rs, a1, a2);
            index += _cornerVertexCount;
            AddCorner(index, new Vector2(x - br, -y + br), br, 0f, -Mathf.PI * 0.5f, rs, a1, a2);
            index += _cornerVertexCount;
            AddCorner(index, new Vector2(-x + bl, -y + bl), bl, -Mathf.PI * 0.5f, -Mathf.PI, rs, a1, a2);

            for (int i = 0; i < count + 1; i++)
            {
                _Normals[i] = -Vector3.forward;
            }

            for (int i = 0; i < count; i++)
            {
                int a = i + 1;
                int b = i == count - 1 ? 1 : i + 2;

                _Triangles[i * 3] = 0;
                _Triangles[i * 3 + 1] = a;
                _Triangles[i * 3 + 2] = b;
            }

            uiMesh.Clear();
            uiMesh.vertices = _Vertices;
            uiMesh.normals = _Normals;
            if (_createUV)
                uiMesh.uv = _UV;
            uiMesh.triangles = _Triangles;
            return uiMesh;
        }

        /// <summary>
        /// Runs the add corner workflow.
        /// </summary>
        /// <param name="startIndex">The start Index value.</param>
        /// <param name="center">The center value.</param>
        /// <param name="radius">The radius value.</param>
        /// <param name="startAngle">The start Angle value.</param>
        /// <param name="endAngle">The end Angle value.</param>
        /// <param name="rs">The rs value.</param>
        /// <param name="a1">The a1 value.</param>
        /// <param name="a2">The a2 value.</param>
        private void AddCorner(int startIndex, Vector2 center, float radius, float startAngle, float endAngle, Vector2 rs, float a1, float a2)
        {
            for (int i = 0; i < _cornerVertexCount; i++)
            {
                float t = (float)i / (_cornerVertexCount - 1);
                float angle = Mathf.Lerp(startAngle, endAngle, t);

                Vector2 local = new Vector2(
                    center.x + Mathf.Cos(angle) * radius * a1,
                    center.y + Mathf.Sin(angle) * radius * a2
                );

                _Vertices[startIndex + i] = (Vector2.Scale(local, rs) + _rect.center) * _scale;

                if (_createUV)
                {
                    Vector2 uv = local;

                    if (!_usePercentage)
                    {
                        Vector2 adj = new Vector2(2f / _rect.width, 2f / _rect.height);
                        uv = Vector2.Scale(uv, adj);
                    }

                    _UV[startIndex + i] = uv * 0.5f + Vector2.one * 0.5f;
                }
            }
        }

        /// <summary>
        /// Returns the create curved rounded rectangle mesh result.
        /// </summary>
        /// <param name="uiMesh">The ui Mesh value.</param>
        /// <param name="perimeterIndices">The perimeter indices used by the extrusion pass.</param>
        /// <returns>The result of the operation.</returns>
        private Mesh createCurvedRoundedRectangleMesh(Mesh uiMesh, out int[] perimeterIndices)
        {
            getAbsoluteCornerRadii(out float tl, out float tr, out float bl, out float br);

            float halfWidth = _rect.width * 0.5f;
            float height = _rect.height;
            List<float> columnPositions = getCurveColumnPositions(halfWidth, tl, tr, bl, br);
            int columnCount = columnPositions.Count;
            int segmentCount = columnCount - 1;
            int vertexCount = columnCount * 2;
            int triangleCount = segmentCount * 2;

            Vector3[] vertices = new Vector3[vertexCount];
            Vector3[] normals = new Vector3[vertexCount];
            Vector2[] uvs = new Vector2[vertexCount];
            int[] triangles = new int[triangleCount * 3];

            for (int column = 0; column < columnCount; column++)
            {
                float x = columnPositions[column];
                float t = Mathf.InverseLerp(-halfWidth, halfWidth, x);
                float bottom = getRoundedBottomY(x, halfWidth, bl, br);
                float top = getRoundedTopY(x, halfWidth, height, tl, tr);

                int bottomIndex = getCurveBottomIndex(column);
                int topIndex = getCurveTopIndex(column);
                float flatX = x * _scale;

                vertices[bottomIndex] = bendLocalPosition(new Vector3(flatX, bottom * _scale, 0f));
                vertices[topIndex] = bendLocalPosition(new Vector3(flatX, top * _scale, 0f));
                normals[bottomIndex] = getCurvedFrontNormal(flatX);
                normals[topIndex] = normals[bottomIndex];
                uvs[bottomIndex] = new Vector2(t, Mathf.Clamp01(bottom / height));
                uvs[topIndex] = new Vector2(t, Mathf.Clamp01(top / height));
            }

            int triangle = 0;
            for (int column = 0; column < segmentCount; column++)
            {
                int bottom = getCurveBottomIndex(column);
                int top = getCurveTopIndex(column);
                int nextBottom = getCurveBottomIndex(column + 1);
                int nextTop = getCurveTopIndex(column + 1);

                triangles[triangle++] = bottom;
                triangles[triangle++] = top;
                triangles[triangle++] = nextBottom;

                triangles[triangle++] = top;
                triangles[triangle++] = nextTop;
                triangles[triangle++] = nextBottom;
            }

            perimeterIndices = getCurvePerimeterIndices(segmentCount);

            uiMesh.Clear();
            uiMesh.vertices = vertices;
            uiMesh.normals = normals;
            uiMesh.uv = uvs;
            uiMesh.triangles = triangles;
            uiMesh.RecalculateBounds();
            return uiMesh;
        }

        /// <summary>
        /// Runs the create extruded curved mesh workflow.
        /// </summary>
        /// <param name="srcMesh">The src Mesh value.</param>
        /// <param name="extrudedMesh">The extruded Mesh value.</param>
        /// <param name="perimeterIndices">The perimeter Indices value.</param>
        /// <param name="extrusionDistance">The extrusion Distance value.</param>
        private void createExtrudedCurvedMesh(Mesh srcMesh, Mesh extrudedMesh, int[] perimeterIndices, float extrusionDistance)
        {
            Vector3[] inputVertices = srcMesh.vertices;
            Vector2[] inputUV = srcMesh.uv;
            int[] inputTriangles = srcMesh.triangles;
            int inputVertexCount = inputVertices.Length;

            Vector3[] vertices = new Vector3[inputVertexCount * 2];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[inputTriangles.Length * 2 + perimeterIndices.Length * 6];

            for (int i = 0; i < inputVertexCount; i++)
            {
                vertices[i] = inputVertices[i];
                uvs[i] = inputUV[i];

                float flatX = (inputUV[i].x - 0.5f) * _flatWidth;
                vertices[inputVertexCount + i] = inputVertices[i] - getCurvedFrontNormal(flatX) * extrusionDistance;
                uvs[inputVertexCount + i] = inputUV[i];
            }

            int triangle = 0;
            for (int i = 0; i < inputTriangles.Length; i += 3)
            {
                triangles[triangle++] = inputTriangles[i];
                triangles[triangle++] = inputTriangles[i + 1];
                triangles[triangle++] = inputTriangles[i + 2];
            }

            for (int i = 0; i < inputTriangles.Length; i += 3)
            {
                triangles[triangle++] = inputTriangles[i] + inputVertexCount;
                triangles[triangle++] = inputTriangles[i + 2] + inputVertexCount;
                triangles[triangle++] = inputTriangles[i + 1] + inputVertexCount;
            }

            for (int i = 0; i < perimeterIndices.Length; i++)
            {
                int a = perimeterIndices[i];
                int b = perimeterIndices[(i + 1) % perimeterIndices.Length];
                int backA = a + inputVertexCount;
                int backB = b + inputVertexCount;

                triangles[triangle++] = a;
                triangles[triangle++] = backA;
                triangles[triangle++] = b;

                triangles[triangle++] = b;
                triangles[triangle++] = backA;
                triangles[triangle++] = backB;
            }

            extrudedMesh.Clear();
            extrudedMesh.name = "extruded";
            extrudedMesh.vertices = vertices;
            extrudedMesh.uv = uvs;
            extrudedMesh.triangles = triangles;
            extrudedMesh.RecalculateNormals();
            extrudedMesh.RecalculateBounds();
        }

        /// <summary>
        /// Gets the absolute corner radii.
        /// </summary>
        /// <param name="tl">The tl value.</param>
        /// <param name="tr">The tr value.</param>
        /// <param name="bl">The bl value.</param>
        /// <param name="br">The br value.</param>
        private void getAbsoluteCornerRadii(out float tl, out float tr, out float bl, out float br)
        {
            tl = Mathf.Max(0, _roundTopLeft + _roundEdges);
            tr = Mathf.Max(0, _roundTopRight + _roundEdges);
            bl = Mathf.Max(0, _roundBottomLeft + _roundEdges);
            br = Mathf.Max(0, _roundBottomRight + _roundEdges);

            if (_usePercentage)
            {
                float radiusScale = Mathf.Min(Mathf.Abs(_rect.width), Mathf.Abs(_rect.height)) * 0.5f;
                tl = Mathf.Clamp01(tl) * radiusScale;
                tr = Mathf.Clamp01(tr) * radiusScale;
                bl = Mathf.Clamp01(bl) * radiusScale;
                br = Mathf.Clamp01(br) * radiusScale;
            }

            float width = Mathf.Abs(_rect.width);
            float height = Mathf.Abs(_rect.height);
            float scaleFactor = 1f;

            if (tl + tr > width && tl + tr > 0f)
                scaleFactor = Mathf.Min(scaleFactor, width / (tl + tr));

            if (bl + br > width && bl + br > 0f)
                scaleFactor = Mathf.Min(scaleFactor, width / (bl + br));

            if (tl + bl > height && tl + bl > 0f)
                scaleFactor = Mathf.Min(scaleFactor, height / (tl + bl));

            if (tr + br > height && tr + br > 0f)
                scaleFactor = Mathf.Min(scaleFactor, height / (tr + br));

            tl *= scaleFactor;
            tr *= scaleFactor;
            bl *= scaleFactor;
            br *= scaleFactor;
        }

        /// <summary>
        /// Returns sorted x columns for the curved mesh, preserving corner arc samples.
        /// </summary>
        /// <param name="halfWidth">The half Width value.</param>
        /// <param name="tl">The tl value.</param>
        /// <param name="tr">The tr value.</param>
        /// <param name="bl">The bl value.</param>
        /// <param name="br">The br value.</param>
        /// <returns>The result of the operation.</returns>
        private List<float> getCurveColumnPositions(float halfWidth, float tl, float tr, float bl, float br)
        {
            List<float> columns = new List<float>();
            int baseColumnCount = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(_cornerVertexCount, _curveAngle / 4f)), 8, 128);

            for (int i = 0; i <= baseColumnCount; i++)
            {
                addCurveColumn(columns, Mathf.Lerp(-halfWidth, halfWidth, (float)i / baseColumnCount));
            }

            addCornerColumns(columns, -halfWidth + tl, tl, Mathf.PI, Mathf.PI * 0.5f);
            addCornerColumns(columns, halfWidth - tr, tr, Mathf.PI * 0.5f, 0f);
            addCornerColumns(columns, halfWidth - br, br, 0f, -Mathf.PI * 0.5f);
            addCornerColumns(columns, -halfWidth + bl, bl, -Mathf.PI * 0.5f, -Mathf.PI);

            columns.Sort();

            for (int i = columns.Count - 1; i > 0; i--)
            {
                if (Mathf.Abs(columns[i] - columns[i - 1]) < 0.0001f)
                {
                    columns.RemoveAt(i);
                }
            }

            if (columns.Count < 2)
            {
                columns.Clear();
                columns.Add(-halfWidth);
                columns.Add(halfWidth);
            }

            return columns;
        }

        /// <summary>
        /// Adds corner arc x columns.
        /// </summary>
        /// <param name="columns">The columns value.</param>
        /// <param name="centerX">The center X value.</param>
        /// <param name="radius">The radius value.</param>
        /// <param name="startAngle">The start Angle value.</param>
        /// <param name="endAngle">The end Angle value.</param>
        private void addCornerColumns(List<float> columns, float centerX, float radius, float startAngle, float endAngle)
        {
            if (radius <= 0f)
            {
                return;
            }

            int samples = Mathf.Max(2, _cornerVertexCount);
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / (samples - 1);
                float angle = Mathf.Lerp(startAngle, endAngle, t);
                addCurveColumn(columns, centerX + Mathf.Cos(angle) * radius);
            }
        }

        /// <summary>
        /// Adds a curve column.
        /// </summary>
        /// <param name="columns">The columns value.</param>
        /// <param name="x">The x value.</param>
        private void addCurveColumn(List<float> columns, float x)
        {
            columns.Add(Mathf.Clamp(x, _rect.xMin, _rect.xMax));
        }

        /// <summary>
        /// Gets the curve bottom index.
        /// </summary>
        /// <param name="column">The column value.</param>
        /// <returns>The result of the operation.</returns>
        private int getCurveBottomIndex(int column)
        {
            return column * 2;
        }

        /// <summary>
        /// Gets the curve top index.
        /// </summary>
        /// <param name="column">The column value.</param>
        /// <returns>The result of the operation.</returns>
        private int getCurveTopIndex(int column)
        {
            return column * 2 + 1;
        }

        /// <summary>
        /// Gets the curve perimeter indices.
        /// </summary>
        /// <param name="columns">The columns value.</param>
        /// <returns>The result of the operation.</returns>
        private int[] getCurvePerimeterIndices(int columns)
        {
            List<int> indices = new List<int>(columns * 2 + 2);
            indices.Add(getCurveBottomIndex(0));
            indices.Add(getCurveTopIndex(0));

            for (int column = 1; column <= columns; column++)
            {
                indices.Add(getCurveTopIndex(column));
            }

            indices.Add(getCurveBottomIndex(columns));

            for (int column = columns - 1; column >= 1; column--)
            {
                indices.Add(getCurveBottomIndex(column));
            }

            return indices.ToArray();
        }

        /// <summary>
        /// Returns the rounded top y result.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="halfWidth">The half Width value.</param>
        /// <param name="height">The height value.</param>
        /// <param name="tl">The tl value.</param>
        /// <param name="tr">The tr value.</param>
        /// <returns>The result of the operation.</returns>
        private float getRoundedTopY(float x, float halfWidth, float height, float tl, float tr)
        {
            if (tl > 0f && x < -halfWidth + tl)
            {
                float dx = x - (-halfWidth + tl);
                return height - tl + Mathf.Sqrt(Mathf.Max(0f, tl * tl - dx * dx));
            }

            if (tr > 0f && x > halfWidth - tr)
            {
                float dx = x - (halfWidth - tr);
                return height - tr + Mathf.Sqrt(Mathf.Max(0f, tr * tr - dx * dx));
            }

            return height;
        }

        /// <summary>
        /// Returns the rounded bottom y result.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="halfWidth">The half Width value.</param>
        /// <param name="bl">The bl value.</param>
        /// <param name="br">The br value.</param>
        /// <returns>The result of the operation.</returns>
        private float getRoundedBottomY(float x, float halfWidth, float bl, float br)
        {
            if (bl > 0f && x < -halfWidth + bl)
            {
                float dx = x - (-halfWidth + bl);
                return bl - Mathf.Sqrt(Mathf.Max(0f, bl * bl - dx * dx));
            }

            if (br > 0f && x > halfWidth - br)
            {
                float dx = x - (halfWidth - br);
                return br - Mathf.Sqrt(Mathf.Max(0f, br * br - dx * dx));
            }

            return 0f;
        }

        /// <summary>
        /// Bends a flat local position around the vertical axis.
        /// </summary>
        /// <param name="flatPosition">The flat Position value.</param>
        /// <returns>The result of the operation.</returns>
        private Vector3 bendLocalPosition(Vector3 flatPosition)
        {
            if (_curveAngle <= CurveEpsilon || _flatWidth <= 0.0001f)
            {
                return flatPosition;
            }

            float angleRad = Mathf.Clamp(_curveAngle, 0f, MaxCurveAngle) * Mathf.Deg2Rad;
            float radius = _flatWidth / angleRad;
            float theta = flatPosition.x / radius;

            return new Vector3(
                Mathf.Sin(theta) * radius,
                flatPosition.y,
                flatPosition.z + (Mathf.Cos(theta) - 1f) * radius);
        }

        /// <summary>
        /// Gets the curved front normal.
        /// </summary>
        /// <param name="flatX">The flat X value.</param>
        /// <returns>The result of the operation.</returns>
        private Vector3 getCurvedFrontNormal(float flatX)
        {
            if (_curveAngle <= CurveEpsilon || _flatWidth <= 0.0001f)
            {
                return -Vector3.forward;
            }

            float angleRad = Mathf.Clamp(_curveAngle, 0f, MaxCurveAngle) * Mathf.Deg2Rad;
            float radius = _flatWidth / angleRad;
            float theta = flatX / radius;

            return new Vector3(-Mathf.Sin(theta), 0f, -Mathf.Cos(theta)).normalized;
        }

        /// <summary>
        /// Releases meshes and material instances owned by this panel.
        /// </summary>
        private void OnDestroy()
        {
            if (_panelMaterial != null)
            {
                Destroy(_panelMaterial);
                _panelMaterial = null;
            }

            if (_uiMesh != null)
            {
                Destroy(_uiMesh);
                _uiMesh = null;
            }

            if (_panelMesh != null)
            {
                Destroy(_panelMesh);
                _panelMesh = null;
            }
        }
        #endregion

        #region Nested Types
        /// <summary>
        /// Represents the Edge type.
        /// </summary>
        private class Edge
        {
            #region State
            // The indiex to each vertex
            public int[] vertexIndex = new int[2];
            // The index into the face.
            // (faceindex[0] == faceindex[1] means the edge connects to only one triangle)
            public int[] faceIndex = new int[2];
            #endregion
        }
        #endregion

        #region Methods
        /// <summary>
        /// Runs the extrude mesh workflow.
        /// </summary>
        /// <param name="srcMesh">The src Mesh value.</param>
        /// <param name="extrudedMesh">The extruded Mesh value.</param>
        /// <param name="extrusion">The extrusion value.</param>
        /// <param name="invertFaces">The invert Faces value.</param>
        private void extrudeMesh(Mesh srcMesh, Mesh extrudedMesh, Matrix4x4[] extrusion, bool invertFaces)
        {
            //Edge[] edges = BuildManifoldEdges(srcMesh);
            int count = _cornerVertexCount * 4;
            Edge[] edges = new Edge[count];

            for (int i = 0; i < count; i++)
            {
                edges[i] = new Edge();
                edges[i].vertexIndex[0] = i + 1;
                edges[i].vertexIndex[1] = (i + 1) % count + 1;
            }
            extrudeMesh(srcMesh, extrudedMesh, extrusion, edges, invertFaces);
        }

        /// <summary>
        /// Runs the extrude mesh workflow.
        /// </summary>
        /// <param name="srcMesh">The src Mesh value.</param>
        /// <param name="extrudedMesh">The extruded Mesh value.</param>
        /// <param name="extrusion">The extrusion value.</param>
        /// <param name="edges">The edges value.</param>
        /// <param name="invertFaces">The invert Faces value.</param>
        private static void extrudeMesh(Mesh srcMesh, Mesh extrudedMesh, Matrix4x4[] extrusion, Edge[] edges, bool invertFaces)
        {
            int extrudedVertexCount = edges.Length * 2 * extrusion.Length;
            int triIndicesPerStep = edges.Length * 6;
            int extrudedTriIndexCount = triIndicesPerStep * (extrusion.Length - 1);

            Vector3[] inputVertices = srcMesh.vertices;
            Vector2[] inputUV = srcMesh.uv;
            int[] inputTriangles = srcMesh.triangles;

            Vector3[] vertices = new Vector3[extrudedVertexCount + srcMesh.vertexCount * 2];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[extrudedTriIndexCount + inputTriangles.Length * 2];

            // Build extruded vertices
            int v = 0;
            for (int i = 0; i < extrusion.Length; i++)
            {
                Matrix4x4 matrix = extrusion[i];
                float vcoord = (float)i / (extrusion.Length - 1);
                foreach (Edge e in edges)
                {
                    vertices[v + 0] = matrix.MultiplyPoint(inputVertices[e.vertexIndex[0]]);
                    vertices[v + 1] = matrix.MultiplyPoint(inputVertices[e.vertexIndex[1]]);

                    uvs[v + 0] = new Vector2(inputUV[e.vertexIndex[0]].x, vcoord);
                    uvs[v + 1] = new Vector2(inputUV[e.vertexIndex[1]].x, vcoord);

                    v += 2;
                }
            }

            // Build cap vertices
            // * The bottom mesh we scale along it's negative extrusion direction. This way extruding a half sphere results in a capsule.
            for (int c = 0; c < 2; c++)
            {
                Matrix4x4 matrix = extrusion[c == 0 ? 0 : extrusion.Length - 1];
                int firstCapVertex = c == 0 ? extrudedVertexCount : extrudedVertexCount + inputVertices.Length;
                for (int i = 0; i < inputVertices.Length; i++)
                {
                    vertices[firstCapVertex + i] = matrix.MultiplyPoint(inputVertices[i]);
                    uvs[firstCapVertex + i] = inputUV[i];
                }
            }

            // Build extruded triangles
            for (int i = 0; i < extrusion.Length - 1; i++)
            {
                int baseVertexIndex = (edges.Length * 2) * i;
                int nextVertexIndex = (edges.Length * 2) * (i + 1);
                for (int e = 0; e < edges.Length; e++)
                {
                    int triIndex = i * triIndicesPerStep + e * 6;

                    triangles[triIndex + 0] = baseVertexIndex + e * 2;
                    triangles[triIndex + 1] = nextVertexIndex + e * 2;
                    triangles[triIndex + 2] = baseVertexIndex + e * 2 + 1;
                    triangles[triIndex + 3] = nextVertexIndex + e * 2;
                    triangles[triIndex + 4] = nextVertexIndex + e * 2 + 1;
                    triangles[triIndex + 5] = baseVertexIndex + e * 2 + 1;
                }
            }

            // build cap triangles
            int triCount = inputTriangles.Length / 3;
            // Top
            {
                int firstCapVertex = extrudedVertexCount;
                int firstCapTriIndex = extrudedTriIndexCount;
                for (int i = 0; i < triCount; i++)
                {
                    triangles[i * 3 + firstCapTriIndex + 0] = inputTriangles[i * 3 + 1] + firstCapVertex;
                    triangles[i * 3 + firstCapTriIndex + 1] = inputTriangles[i * 3 + 2] + firstCapVertex;
                    triangles[i * 3 + firstCapTriIndex + 2] = inputTriangles[i * 3 + 0] + firstCapVertex;
                }
            }

            // Bottom
            {
                int firstCapVertex = extrudedVertexCount + inputVertices.Length;
                int firstCapTriIndex = extrudedTriIndexCount + inputTriangles.Length;
                for (int i = 0; i < triCount; i++)
                {
                    triangles[i * 3 + firstCapTriIndex + 0] = inputTriangles[i * 3 + 0] + firstCapVertex;
                    triangles[i * 3 + firstCapTriIndex + 1] = inputTriangles[i * 3 + 2] + firstCapVertex;
                    triangles[i * 3 + firstCapTriIndex + 2] = inputTriangles[i * 3 + 1] + firstCapVertex;
                }
            }

            if (invertFaces)
            {
                for (int i = 0; i < triangles.Length / 3; i++)
                {
                    int temp = triangles[i * 3 + 0];
                    triangles[i * 3 + 0] = triangles[i * 3 + 1];
                    triangles[i * 3 + 1] = temp;
                }
            }

            extrudedMesh.Clear();
            extrudedMesh.name = "extruded";
            extrudedMesh.vertices = vertices;
            extrudedMesh.uv = uvs;
            extrudedMesh.triangles = triangles;
            extrudedMesh.RecalculateNormals();
        }
        #endregion
    }
}
