using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TwoDee
{
    struct Vec3Points
    {
        public Vector3 m_x0y0;
        public Vector3 m_x0y1;
        public Vector3 m_x1y0;
        public Vector3 m_x1y1;

        public void Set(Vector3 center, float x, float y)
        {
            m_x0y0 = center + new Vector3(-x, -y, 0.0f);
            m_x0y1 = center + new Vector3(-x, y, 0.0f);
            m_x1y0 = center + new Vector3(x, -y, 0.0f);
            m_x1y1 = center + new Vector3(x, y, 0.0f);
        }
    };

    public class PolyGenerator : MonoBehaviour
    {
        public bool ColorEq(Color32 a, Color32 b)
        {
            return a.r == b.r && a.b == b.r && a.g == b.g && a.a == b.a;
        }

        List<Vector3> m_Verts = new List<Vector3>();
        List<Vector2> m_Uvs = new List<Vector2>();
        List<int> m_Tris = new List<int>();
        List<Color32> m_Colors = new List<Color32>();
        
        protected int AddVert(Vector3 v, Color32 c)
        {
            m_Verts.Add(v);
            m_Colors.Add(c);
            m_Uvs.Add(new Vector2(0.0f, 0.0f));

            return m_Verts.Count - 1;
        }

        protected void AddTriangle(int ia, int ib, int ic)
        {
            m_Tris.Add(ia);
            m_Tris.Add(ib);
            m_Tris.Add(ic);
        }

        protected void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Color32 ca, Color32 cb, Color32 cc)
        {
            if ( Vector3.Cross(a - b, c - b).z > 0.0f)
            {
                // winding is wrong
               // return;
                //Debug.Log("whoops?\n");
            }
            Vector3[] v = { a, b, c };
            Color32[] cols = { ca, cb, cc };
            int[] foundVert = { -1, -1, -1 };

            for (int vv = 0; vv < 3; vv++)
            {
                foundVert[vv] = AddVert(v[vv], cols[vv]);
            }

            for (int i = 0; i < 3; i++)
                m_Tris.Add(foundVert[i]);
        }

        protected void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Color32 col)
        {
            AddTriangle(a, b, c, col, col, col);
        }

        
        protected void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color32 ca, Color32 cb, Color32 cc, Color32 cd)
        {
            AddTriangle(a, b, c, ca, cb, cc);
            AddTriangle(c, d, a, cc, cd, ca);
        }

        protected void AddFive(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e, Color32 cola, Color32 colb, Color32 colc, Color32 cold, Color32 cole)
        {
            AddTriangle(a, b, c, cola, colb, colc);
            AddTriangle(c, d, a, colc, cold, cola);
            AddTriangle(a, d, e, cola, cold, cole);
        }

        protected void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color32 col)
        {
            AddTriangle(a, b, c, col);
            AddTriangle(c, d, a, col);
        }

        protected void ExtrudeEdge(Vector3 eye, Vector3 a, Vector3 b)
        {
            var edgeDelta = b - a;
            var eyeDelta = eye - a;
            {
                var eyeCross = Vector3.Cross(eyeDelta, edgeDelta);
                if (eyeCross.z > 0.0f) return;
            }

            var extrudeLength = 10000.0f;
            var aDelta = (a - eye).normalized;
            var bDelta = (b - eye).normalized;
            Vector3 aExt = a + aDelta * extrudeLength;
            Vector3 bExt = b + bDelta * extrudeLength;

            AddQuad(a, b, bExt, aExt, new Color32(255, 255, 255, 255));
        }

        Dictionary<int, GameObject> m_MeshObjects = new Dictionary<int, GameObject>();

        GameObject CreateNewMeshObject()
        {
            var go = new GameObject();
            go.layer = gameObject.layer;
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.material = GetComponent<MeshRenderer>().material;
            var mc = go.AddComponent<MeshCollider>();
            go.transform.SetParent(transform, false);

            return go;
        }

        public void DeleteMeshObject(int meshIndex)
        {
            if (!m_MeshObjects.ContainsKey(meshIndex)) return;
            DestroyObject(m_MeshObjects[meshIndex]);
            m_MeshObjects.Remove(meshIndex);
        }

        public GameObject GetMeshObject(int meshIndex)
        {
            if (!m_MeshObjects.ContainsKey(meshIndex)) m_MeshObjects[meshIndex] = CreateNewMeshObject();
            return m_MeshObjects[meshIndex];
        }

        protected void DeleteGeoBlock(int meshIndex)
        {
            DeleteMeshObject(meshIndex);
        }

        protected void CreateGeoStart(int meshIndex)
        {
            var mo = GetMeshObject(meshIndex);
            // Reset geo
            m_Verts = new List<Vector3>();
            m_Uvs = new List<Vector2>();
            m_Tris = new List<int>();
            m_Colors = new List<Color32>();
        }

        protected void CreateGeoEnd(int meshIndex)
        {
            var mo = GetMeshObject(meshIndex);

            // Transfer geo to mesh filter
            var newVertices = m_Verts.ToArray();
            var newUV = m_Uvs.ToArray();
            var newTriangles = m_Tris.ToArray();
            var newC32 = m_Colors.ToArray();

            Mesh mesh = mo.GetComponent<MeshFilter>().mesh;
            mesh.Clear();
            mesh.vertices = newVertices;
            var newNormals = new Vector3[newVertices.Length];
            for (int i = 0; i < newNormals.Length; i++) newNormals[i] = Vector3.back;
            mesh.normals = newNormals;
            mesh.uv = newUV;
            mesh.colors32 = newC32;
            mesh.triangles = newTriangles;
        }

        public void CreateGeo(Camera cam)
        {
            CreateGeoStart(0);

            CreateGeoProtected(cam);

            CreateGeoEnd(0);
        }

        virtual protected void CreateGeoProtected(Camera cam)
        {
        }
    }
}