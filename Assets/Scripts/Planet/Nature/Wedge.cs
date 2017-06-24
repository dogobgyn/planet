using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Planet
{
    public class Wedge : MonoBehaviour
    {
        Mesh CreateExtrudedConvexPolyMesh(List<Vector3> polyVerts, float zAmount)
        {
            Mesh mesh = new Mesh();
            Vector3[] meshVerts = new Vector3[polyVerts.Count * 2];

            Vector3 zPlus = new Vector3(0.0f, 0.0f, zAmount);

            var PhysicsEdges = new List<int>();
            for (int i = 0; i < polyVerts.Count; i++)
            {
                meshVerts[i * 2 + 0] = polyVerts[i] + zPlus;
                meshVerts[i * 2 + 1] = polyVerts[i] - zPlus;

                PhysicsEdges.Add(i);
                PhysicsEdges.Add((i + 1) % polyVerts.Count);
            }
            List<int> meshTriangleList = new List<int>();

            for (int i = 0; i < PhysicsEdges.Count / 2; i++)
            {
                int index1 = PhysicsEdges[i * 2 + 0];
                int index2 = PhysicsEdges[i * 2 + 1];
                meshTriangleList.Add(index1 * 2);
                meshTriangleList.Add(index2 * 2);
                meshTriangleList.Add(index2 * 2 + 1);

                meshTriangleList.Add(index1 * 2);
                meshTriangleList.Add(index2 * 2 + 1);
                meshTriangleList.Add(index1 * 2 + 1);

                // @TEST - Show collision normals

                var cross = Vector3.Cross(meshVerts[index2 * 2] - meshVerts[index1 * 2], meshVerts[index2 * 2 + 1] - meshVerts[index1 * 2]);
                var debugDrawPoint = Vector3.Lerp(transform.TransformPoint(meshVerts[index1 * 2]),
                    transform.TransformPoint(meshVerts[index2 * 2]), 0.5f); debugDrawPoint.z = -0.1f;
                Debug.DrawLine(debugDrawPoint, debugDrawPoint + cross, Color.black, 1000);
                cross = Vector3.Cross(meshVerts[index2 * 2 + 1] - meshVerts[index1 * 2], meshVerts[index1 * 2 + 1] - meshVerts[index1 * 2]);
                //debugDrawPoint = transform.TransformPoint(meshVerts[index2 * 2]); debugDrawPoint.z = 0.0f;
                Debug.DrawLine(debugDrawPoint, debugDrawPoint + cross, Color.black, 1000);

            }

            for (int i = 0; i < polyVerts.Count - 2; i++)
            {
                meshTriangleList.Add(0);
                meshTriangleList.Add((i + 2) * 2);
                meshTriangleList.Add((i + 1) * 2);

                meshTriangleList.Add(0 + 1);
                meshTriangleList.Add((i + 1) * 2 + 1);
                meshTriangleList.Add((i + 2) * 2 + 1);
            }

            mesh.vertices = meshVerts;
            mesh.triangles = meshTriangleList.ToArray();
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return mesh;
        }

        void AddExtrudedConvexPoly(int blockId, List<Vector3> polyVerts)
        {
            var go = GetComponent<TwoDee.PolyGenerator>().GetMeshObject(blockId);
            var mc = go.GetComponent<MeshCollider>();
            mc.convex = true;
            var mf = go.GetComponent<MeshFilter>();

            var mesh = CreateExtrudedConvexPolyMesh(polyVerts, 0.3f);
            var meshc = CreateExtrudedConvexPolyMesh(polyVerts, 4.0f);

            mc.sharedMesh = meshc;
            mf.sharedMesh = mesh;
            mf.mesh = mesh;
        }

        private void Start()
        {
            var vertList = new List<Vector3>();
            vertList.Add(new Vector3(-1.0f, 1.0f));
            vertList.Add(new Vector3(1.0f, 1.0f));
            vertList.Add(new Vector3(1.0f, -1.0f));

            AddExtrudedConvexPoly(0, vertList);
        }
    }

}