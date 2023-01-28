using UnityEngine;
using System.Collections;

namespace Fugui.Core
{
    public class RoundedRectangleMesh : MonoBehaviour
    {
        public Material UIMat;
        public Material PanelMat;

        private float RoundEdges = 0.5f;
        private float RoundTopLeft = 0.0f;
        private float RoundTopRight = 0.0f;
        private float RoundBottomLeft = 0.0f;
        private float RoundBottomRight = 0.0f;
        private bool UsePercentage = false;
        private Rect rect = new Rect(-0.5f, -0.5f, 1f, 1f);
        private float Scale = 1f;
        private int CornerVertexCount = 8;
        private Vector3[] m_Vertices;
        private Vector3[] m_Normals;
        private Vector2[] m_UV;
        private int[] m_Triangles;

        public Mesh CreateMesh(float width, float height, float scale, float TLRounding, float TRRounding, float BLRounding, float BRRounding, float extrusionDistance, int roundTriangles, Material UImaterial, Material PanelMaterial)
        {
            RoundEdges = 0f;
            RoundTopLeft = TLRounding;
            RoundTopRight = TRRounding;
            RoundBottomLeft = BLRounding;
            RoundBottomRight = BRRounding;
            CornerVertexCount = roundTriangles;
            rect = new Rect(-width / 2f, 0, width, height);
            Scale = scale;

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

        private bool CreateUV = true;
        private bool FlipBackFaceUV = true;
        private bool DoubleSided = true;
        private Mesh createRoundedRectangleMesh(Mesh uiMesh)
        {
            if (CornerVertexCount < 2)
                CornerVertexCount = 2;
            int sides = DoubleSided ? 2 : 1;
            int vCount = CornerVertexCount * 4 * sides + sides; //+sides for center vertices
            int triCount = (CornerVertexCount * 4) * sides;
            if (m_Vertices == null || m_Vertices.Length != vCount)
            {
                m_Vertices = new Vector3[vCount];
                m_Normals = new Vector3[vCount];
            }
            if (m_Triangles == null || m_Triangles.Length != triCount * 3)
                m_Triangles = new int[triCount * 3];
            if (CreateUV && (m_UV == null || m_UV.Length != vCount))
            {
                m_UV = new Vector2[vCount];
            }
            int count = CornerVertexCount * 4;
            if (CreateUV)
            {
                m_UV[0] = Vector2.one * 0.5f;
                if (DoubleSided)
                    m_UV[count + 1] = m_UV[0];
            }
            float tl = Mathf.Max(0, RoundTopLeft + RoundEdges);
            float tr = Mathf.Max(0, RoundTopRight + RoundEdges);
            float bl = Mathf.Max(0, RoundBottomLeft + RoundEdges);
            float br = Mathf.Max(0, RoundBottomRight + RoundEdges);
            float f = Mathf.PI * 0.5f / (CornerVertexCount - 1);
            float a1 = 1f;
            float a2 = 1f;
            float x = 1f;
            float y = 1f;
            Vector2 rs = Vector2.one;
            if (UsePercentage)
            {
                rs = new Vector2(rect.width, rect.height) * 0.5f;
                if (rect.width > rect.height)
                    a1 = rect.height / rect.width;
                else
                    a2 = rect.width / rect.height;
                tl = Mathf.Clamp01(tl);
                tr = Mathf.Clamp01(tr);
                bl = Mathf.Clamp01(bl);
                br = Mathf.Clamp01(br);
            }
            else
            {
                x = rect.width * 0.5f;
                y = rect.height * 0.5f;
                if (tl + tr > rect.width)
                {
                    float b = rect.width / (tl + tr);
                    tl *= b;
                    tr *= b;
                }
                if (bl + br > rect.width)
                {
                    float b = rect.width / (bl + br);
                    bl *= b;
                    br *= b;
                }
                if (tl + bl > rect.height)
                {
                    float b = rect.height / (tl + bl);
                    tl *= b;
                    bl *= b;
                }
                if (tr + br > rect.height)
                {
                    float b = rect.height / (tr + br);
                    tr *= b;
                    br *= b;
                }
            }
            m_Vertices[0] = rect.center * Scale;
            if (DoubleSided)
                m_Vertices[count + 1] = rect.center * Scale;
            for (int i = 0; i < CornerVertexCount; i++)
            {
                float s = Mathf.Sin((float)i * f);
                float c = Mathf.Cos((float)i * f);
                Vector2 v1 = new Vector3(-x + (1f - c) * tl * a1, y - (1f - s) * tl * a2);
                Vector2 v2 = new Vector3(x - (1f - s) * tr * a1, y - (1f - c) * tr * a2);
                Vector2 v3 = new Vector3(x - (1f - c) * br * a1, -y + (1f - s) * br * a2);
                Vector2 v4 = new Vector3(-x + (1f - s) * bl * a1, -y + (1f - c) * bl * a2);

                m_Vertices[1 + i] = (Vector2.Scale(v1, rs) + rect.center) * Scale;
                m_Vertices[1 + CornerVertexCount + i] = (Vector2.Scale(v2, rs) + rect.center) * Scale;
                m_Vertices[1 + CornerVertexCount * 2 + i] = (Vector2.Scale(v3, rs) + rect.center) * Scale;
                m_Vertices[1 + CornerVertexCount * 3 + i] = (Vector2.Scale(v4, rs) + rect.center) * Scale;
                if (CreateUV)
                {
                    if (!UsePercentage)
                    {
                        Vector2 adj = new Vector2(2f / rect.width, 2f / rect.height);
                        v1 = Vector2.Scale(v1, adj);
                        v2 = Vector2.Scale(v2, adj);
                        v3 = Vector2.Scale(v3, adj);
                        v4 = Vector2.Scale(v4, adj);
                    }
                    m_UV[1 + i] = v1 * 0.5f + Vector2.one * 0.5f;
                    m_UV[1 + CornerVertexCount * 1 + i] = v2 * 0.5f + Vector2.one * 0.5f;
                    m_UV[1 + CornerVertexCount * 2 + i] = v3 * 0.5f + Vector2.one * 0.5f;
                    m_UV[1 + CornerVertexCount * 3 + i] = v4 * 0.5f + Vector2.one * 0.5f;
                }
                if (DoubleSided)
                {
                    m_Vertices[1 + CornerVertexCount * 8 - i] = m_Vertices[1 + i];
                    m_Vertices[1 + CornerVertexCount * 7 - i] = m_Vertices[1 + CornerVertexCount + i];
                    m_Vertices[1 + CornerVertexCount * 6 - i] = m_Vertices[1 + CornerVertexCount * 2 + i];
                    m_Vertices[1 + CornerVertexCount * 5 - i] = m_Vertices[1 + CornerVertexCount * 3 + i];
                    if (CreateUV)
                    {
                        m_UV[1 + CornerVertexCount * 8 - i] = v1 * 0.5f + Vector2.one * 0.5f;
                        m_UV[1 + CornerVertexCount * 7 - i] = v2 * 0.5f + Vector2.one * 0.5f;
                        m_UV[1 + CornerVertexCount * 6 - i] = v3 * 0.5f + Vector2.one * 0.5f;
                        m_UV[1 + CornerVertexCount * 5 - i] = v4 * 0.5f + Vector2.one * 0.5f;
                    }
                }
            }
            for (int i = 0; i < count + 1; i++)
            {
                m_Normals[i] = -Vector3.forward;
                if (DoubleSided)
                {
                    m_Normals[count + 1 + i] = Vector3.forward;
                    if (FlipBackFaceUV)
                    {
                        Vector2 uv = m_UV[count + 1 + i];
                        uv.x = 1f - uv.x;
                        m_UV[count + 1 + i] = uv;
                    }
                }
            }
            for (int i = 0; i < count; i++)
            {
                m_Triangles[i * 3] = 0;
                m_Triangles[i * 3 + 1] = i + 1;
                m_Triangles[i * 3 + 2] = i + 2;
                if (DoubleSided)
                {
                    m_Triangles[(count + i) * 3] = count + 1;
                    m_Triangles[(count + i) * 3 + 1] = count + 1 + i + 1;
                    m_Triangles[(count + i) * 3 + 2] = count + 1 + i + 2;
                }
            }
            m_Triangles[count * 3 - 1] = 1;
            if (DoubleSided)
                m_Triangles[m_Triangles.Length - 1] = count + 1 + 1;

            uiMesh.Clear();
            uiMesh.vertices = m_Vertices;
            uiMesh.normals = m_Normals;
            if (CreateUV)
                uiMesh.uv = m_UV;
            uiMesh.triangles = m_Triangles;
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