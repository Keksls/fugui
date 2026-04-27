using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Represents the Fu Panel Mesh type.
    /// </summary>
    public class FuPanelMesh : MonoBehaviour
    {
        #region State
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
        /// <returns>The result of the operation.</returns>
        public Mesh CreateMesh(float width, float height, float scale, float TLRounding, float TRRounding, float BLRounding, float BRRounding, float extrusionDistance, int roundTriangles, Material UImaterial, Material PanelMaterial)
        {
            _roundEdges = 0f;
            _roundTopLeft = TLRounding;
            _roundTopRight = TRRounding;
            _roundBottomLeft = BLRounding;
            _roundBottomRight = BRRounding;
            _cornerVertexCount = roundTriangles;
            _rect = new Rect(-width / 2f, 0, width, height);
            _scale = scale;

            // UI mesh
            var uiMeshFilter = gameObject.AddComponent<MeshFilter>();
            var uiMR = gameObject.AddComponent<MeshRenderer>();
            uiMR.material = UImaterial;
            var uiMesh = new Mesh();
            uiMeshFilter.sharedMesh = uiMesh;

            // Panel mesh
            GameObject panel = new GameObject("panel");
            panel.transform.SetParent(transform);
            panel.transform.localPosition = Vector3.forward * 0.001f;
            panel.transform.localRotation = Quaternion.identity;
            var panelMeshFilter = panel.AddComponent<MeshFilter>();
            var panelMR = panel.AddComponent<MeshRenderer>();
            Material panelMat = Instantiate(PanelMaterial);
            panelMat.mainTextureScale = new Vector2(width * scale, height * scale);
            panelMR.material = panelMat;
            var panelMesh = new Mesh();
            panelMeshFilter.sharedMesh = panelMesh;

            // generate rounded rectangle
            createRoundedRectangleMesh(uiMesh);

            // generate extrusion
            extrudeMesh(uiMesh, panelMesh, new Matrix4x4[] { Matrix4x4.identity, Matrix4x4.Translate(Vector3.forward * extrusionDistance) }, false);

            return uiMesh;
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