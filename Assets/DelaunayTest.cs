using System.Collections.Generic;
using UnityEngine;
using MySpace;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter))]
public class DelaunayTest : MonoBehaviour
{
    [SerializeField]
    private int maxNumPoints = 4096;

    private MeshFilter meshFilter;

    private void CreateMesh()
    {
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        var delaunay = new Delaunay(maxNumPoints);
        {
            delaunay.Setup(0.0f, 0.0f, 4.0f);
            for (var i = delaunay.NumPoints; i < maxNumPoints; i++)
            {
                var x = UnityEngine.Random.value * 2.0f - 1.0f;
                var y = UnityEngine.Random.value * 2.0f - 1.0f;
                delaunay.Insert(x, y);
            }
        }
        sw.Stop();
        Debug.Log(sw.Elapsed.ToString());
        var mesh = new Mesh();
        {
            var ps = UnityEngine.Random.value * 4.0f;
            var px = UnityEngine.Random.value + 1.0f;
            var py = UnityEngine.Random.value + 1.0f;
            var ph = UnityEngine.Random.value + 0.1f;
            var vertices = new Vector3[maxNumPoints];
            for (var i = 0; i < maxNumPoints; i++)
            {
                var x = delaunay.Points[i].x;
                var y = delaunay.Points[i].y;
                vertices[i].x = x;
                vertices[i].y = UnityEngine.Mathf.PerlinNoise((px + x) * ps, (py + y) * ps) * ph;
                vertices[i].z = y;
            }
            mesh.SetVertices(new List<Vector3>(vertices));
        }
        {
            var triangles = new List<int>();
            for (var i = 0; i < maxNumPoints * 2; i++)
            {
                var p0 = delaunay.Triangles[i].p0;
                var p1 = delaunay.Triangles[i].p1;
                var p2 = delaunay.Triangles[i].p2;
#if true
                // バウンディングボックスは描画しない
                if ((p0 < 4) || (p1 < 4) || (p2 < 4))
                {
                    continue;
                }
#endif
                triangles.Add(p0);
                triangles.Add(p2);
                triangles.Add(p1);
            }
            mesh.SetTriangles(triangles, 0);
        }
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        meshFilter.mesh = mesh;
    }
    private void OnEnable()
    {
        meshFilter = GetComponent<MeshFilter>();
        CreateMesh();
    }

    private void Update()
    {
        transform.localRotation *= Quaternion.AngleAxis(15.0f * Time.deltaTime, Vector3.up);

        if (Input.anyKeyDown)
        {
            CreateMesh();
        }
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(DelaunayTest))]
    private class DelaunayTestEditor : Editor
    {
        public sealed override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var obj = target as DelaunayTest;
            if (GUILayout.Button("CreateMesh"))
            {
                obj.CreateMesh();
            }
        }
    }
#endif
}
