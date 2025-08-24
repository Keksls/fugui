using UnityEngine;
using System.Collections;

namespace Fu
{
    public class FuPanelMesh : MonoBehaviour
    {
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
        private bool _flipBackFaceUV = true;
        private bool _doubleSided = true;
        private Vector3[] _Vertices;
        private Vector3[] _Normals;
        private Vector2[] _UV;
        private int[] _Triangles;

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

        private Mesh createRoundedRectangleMesh(Mesh uiMesh)
        {
            if (_cornerVertexCount < 2)
                _cornerVertexCount = 2;
            int sides = _doubleSided ? 2 : 1;
            int vCount = _cornerVertexCount * 4 * sides + sides; //+sides for center vertices
            int triCount = (_cornerVertexCount * 4) * sides;
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
            int count = _cornerVertexCount * 4;
            if (_createUV)
            {
                _UV[0] = Vector2.one * 0.5f;
                if (_doubleSided)
                    _UV[count + 1] = _UV[0];
            }
            float tl = Mathf.Max(0, _roundTopLeft + _roundEdges);
            float tr = Mathf.Max(0, _roundTopRight + _roundEdges);
            float bl = Mathf.Max(0, _roundBottomLeft + _roundEdges);
            float br = Mathf.Max(0, _roundBottomRight + _roundEdges);
            float f = Mathf.PI * 0.5f / (_cornerVertexCount - 1);
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
                if (tl + tr > _rect.width)
                {
                    float b = _rect.width / (tl + tr);
                    tl *= b;
                    tr *= b;
                }
                if (bl + br > _rect.width)
                {
                    float b = _rect.width / (bl + br);
                    bl *= b;
                    br *= b;
                }
                if (tl + bl > _rect.height)
                {
                    float b = _rect.height / (tl + bl);
                    tl *= b;
                    bl *= b;
                }
                if (tr + br > _rect.height)
                {
                    float b = _rect.height / (tr + br);
                    tr *= b;
                    br *= b;
                }
            }
            _Vertices[0] = _rect.center * _scale;
            if (_doubleSided)
                _Vertices[count + 1] = _rect.center * _scale;
            for (int i = 0; i < _cornerVertexCount; i++)
            {
                float s = Mathf.Sin((float)i * f);
                float c = Mathf.Cos((float)i * f);
                Vector2 v1 = new Vector3(-x + (1f - c) * tl * a1, y - (1f - s) * tl * a2);
                Vector2 v2 = new Vector3(x - (1f - s) * tr * a1, y - (1f - c) * tr * a2);
                Vector2 v3 = new Vector3(x - (1f - c) * br * a1, -y + (1f - s) * br * a2);
                Vector2 v4 = new Vector3(-x + (1f - s) * bl * a1, -y + (1f - c) * bl * a2);

                _Vertices[1 + i] = (Vector2.Scale(v1, rs) + _rect.center) * _scale;
                _Vertices[1 + _cornerVertexCount + i] = (Vector2.Scale(v2, rs) + _rect.center) * _scale;
                _Vertices[1 + _cornerVertexCount * 2 + i] = (Vector2.Scale(v3, rs) + _rect.center) * _scale;
                _Vertices[1 + _cornerVertexCount * 3 + i] = (Vector2.Scale(v4, rs) + _rect.center) * _scale;
                if (_createUV)
                {
                    if (!_usePercentage)
                    {
                        Vector2 adj = new Vector2(2f / _rect.width, 2f / _rect.height);
                        v1 = Vector2.Scale(v1, adj);
                        v2 = Vector2.Scale(v2, adj);
                        v3 = Vector2.Scale(v3, adj);
                        v4 = Vector2.Scale(v4, adj);
                    }
                    _UV[1 + i] = v1 * 0.5f + Vector2.one * 0.5f;
                    _UV[1 + _cornerVertexCount * 1 + i] = v2 * 0.5f + Vector2.one * 0.5f;
                    _UV[1 + _cornerVertexCount * 2 + i] = v3 * 0.5f + Vector2.one * 0.5f;
                    _UV[1 + _cornerVertexCount * 3 + i] = v4 * 0.5f + Vector2.one * 0.5f;
                }
                if (_doubleSided)
                {
                    _Vertices[1 + _cornerVertexCount * 8 - i] = _Vertices[1 + i];
                    _Vertices[1 + _cornerVertexCount * 7 - i] = _Vertices[1 + _cornerVertexCount + i];
                    _Vertices[1 + _cornerVertexCount * 6 - i] = _Vertices[1 + _cornerVertexCount * 2 + i];
                    _Vertices[1 + _cornerVertexCount * 5 - i] = _Vertices[1 + _cornerVertexCount * 3 + i];
                    if (_createUV)
                    {
                        _UV[1 + _cornerVertexCount * 8 - i] = v1 * 0.5f + Vector2.one * 0.5f;
                        _UV[1 + _cornerVertexCount * 7 - i] = v2 * 0.5f + Vector2.one * 0.5f;
                        _UV[1 + _cornerVertexCount * 6 - i] = v3 * 0.5f + Vector2.one * 0.5f;
                        _UV[1 + _cornerVertexCount * 5 - i] = v4 * 0.5f + Vector2.one * 0.5f;
                    }
                }
            }
            for (int i = 0; i < count + 1; i++)
            {
                _Normals[i] = -Vector3.forward;
                if (_doubleSided)
                {
                    _Normals[count + 1 + i] = Vector3.forward;
                    if (_flipBackFaceUV)
                    {
                        Vector2 uv = _UV[count + 1 + i];
                        uv.x = 1f - uv.x;
                        _UV[count + 1 + i] = uv;
                    }
                }
            }
            for (int i = 0; i < count; i++)
            {
                _Triangles[i * 3] = 0;
                _Triangles[i * 3 + 1] = i + 1;
                _Triangles[i * 3 + 2] = i + 2;
                if (_doubleSided)
                {
                    _Triangles[(count + i) * 3] = count + 1;
                    _Triangles[(count + i) * 3 + 1] = count + 1 + i + 1;
                    _Triangles[(count + i) * 3 + 2] = count + 1 + i + 2;
                }
            }
            _Triangles[count * 3 - 1] = 1;
            if (_doubleSided)
                _Triangles[_Triangles.Length - 1] = count + 1 + 1;

            uiMesh.Clear();
            uiMesh.vertices = _Vertices;
            uiMesh.normals = _Normals;
            if (_createUV)
                uiMesh.uv = _UV;
            uiMesh.triangles = _Triangles;
            return uiMesh;
        }

        private class Edge
        {
            // The indiex to each vertex
            public int[] vertexIndex = new int[2];
            // The index into the face.
            // (faceindex[0] == faceindex[1] means the edge connects to only one triangle)
            public int[] faceIndex = new int[2];
        }

        private static void extrudeMesh(Mesh srcMesh, Mesh extrudedMesh, Matrix4x4[] extrusion, bool invertFaces)
        {
            Edge[] edges = BuildManifoldEdges(srcMesh);
            extrudeMesh(srcMesh, extrudedMesh, extrusion, edges, invertFaces);
        }

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

        /// Builds an array of edges that connect to only one triangle.
        /// In other words, the outline of the mesh	
        private static Edge[] BuildManifoldEdges(Mesh mesh)
        {
            // Build a edge list for all unique edges in the mesh
            Edge[] edges = BuildEdges(mesh.vertexCount, mesh.triangles);

            // We only want edges that connect to a single triangle
            ArrayList culledEdges = new ArrayList();
            foreach (Edge edge in edges)
            {
                if (edge.faceIndex[0] == edge.faceIndex[1])
                {
                    culledEdges.Add(edge);
                }
            }

            return culledEdges.ToArray(typeof(Edge)) as Edge[];
        }

        /// Builds an array of unique edges
        /// This requires that your mesh has all vertices welded. However on import, Unity has to split
        /// vertices at uv seams and normal seams. Thus for a mesh with seams in your mesh you
        /// will get two edges adjoining one triangle.
        /// Often this is not a problem but you can fix it by welding vertices 
        /// and passing in the triangle array of the welded vertices.
        private static Edge[] BuildEdges(int vertexCount, int[] triangleArray)
        {
            int maxEdgeCount = triangleArray.Length;
            int[] firstEdge = new int[vertexCount + maxEdgeCount];
            int nextEdge = vertexCount;
            int triangleCount = triangleArray.Length / 3;

            for (int a = 0; a < vertexCount; a++)
                firstEdge[a] = -1;

            // First pass over all triangles. This finds all the edges satisfying the
            // condition that the first vertex index is less than the second vertex index
            // when the direction from the first vertex to the second vertex represents
            // a counterclockwise winding around the triangle to which the edge belongs.
            // For each edge found, the edge index is stored in a linked list of edges
            // belonging to the lower-numbered vertex index i. This allows us to quickly
            // find an edge in the second pass whose higher-numbered vertex index is i.
            Edge[] edgeArray = new Edge[maxEdgeCount];

            int edgeCount = 0;
            for (int a = 0; a < triangleCount; a++)
            {
                int i1 = triangleArray[a * 3 + 2];
                for (int b = 0; b < 3; b++)
                {
                    int i2 = triangleArray[a * 3 + b];
                    if (i1 < i2)
                    {
                        Edge newEdge = new Edge();
                        newEdge.vertexIndex[0] = i1;
                        newEdge.vertexIndex[1] = i2;
                        newEdge.faceIndex[0] = a;
                        newEdge.faceIndex[1] = a;
                        edgeArray[edgeCount] = newEdge;

                        int edgeIndex = firstEdge[i1];
                        if (edgeIndex == -1)
                        {
                            firstEdge[i1] = edgeCount;
                        }
                        else
                        {
                            while (true)
                            {
                                int index = firstEdge[nextEdge + edgeIndex];
                                if (index == -1)
                                {
                                    firstEdge[nextEdge + edgeIndex] = edgeCount;
                                    break;
                                }

                                edgeIndex = index;
                            }
                        }

                        firstEdge[nextEdge + edgeCount] = -1;
                        edgeCount++;
                    }

                    i1 = i2;
                }
            }

            // Second pass over all triangles. This finds all the edges satisfying the
            // condition that the first vertex index is greater than the second vertex index
            // when the direction from the first vertex to the second vertex represents
            // a counterclockwise winding around the triangle to which the edge belongs.
            // For each of these edges, the same edge should have already been found in
            // the first pass for a different triangle. Of course we might have edges with only one triangle
            // in that case we just add the edge here
            // So we search the list of edges
            // for the higher-numbered vertex index for the matching edge and fill in the
            // second triangle index. The maximum number of comparisons in this search for
            // any vertex is the number of edges having that vertex as an endpoint.

            for (int a = 0; a < triangleCount; a++)
            {
                int i1 = triangleArray[a * 3 + 2];
                for (int b = 0; b < 3; b++)
                {
                    int i2 = triangleArray[a * 3 + b];
                    if (i1 > i2)
                    {
                        bool foundEdge = false;
                        for (int edgeIndex = firstEdge[i2]; edgeIndex != -1; edgeIndex = firstEdge[nextEdge + edgeIndex])
                        {
                            Edge edge = edgeArray[edgeIndex];
                            if ((edge.vertexIndex[1] == i1) && (edge.faceIndex[0] == edge.faceIndex[1]))
                            {
                                edgeArray[edgeIndex].faceIndex[1] = a;
                                foundEdge = true;
                                break;
                            }
                        }

                        if (!foundEdge)
                        {
                            Edge newEdge = new Edge();
                            newEdge.vertexIndex[0] = i1;
                            newEdge.vertexIndex[1] = i2;
                            newEdge.faceIndex[0] = a;
                            newEdge.faceIndex[1] = a;
                            edgeArray[edgeCount] = newEdge;
                            edgeCount++;
                        }
                    }

                    i1 = i2;
                }
            }

            Edge[] compactedEdges = new Edge[edgeCount];
            for (int e = 0; e < edgeCount; e++)
                compactedEdges[e] = edgeArray[e];

            return compactedEdges;
        }
    }
}