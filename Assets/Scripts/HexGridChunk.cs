using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexGridChunk : MonoBehaviour 
{
    private Mesh mesh;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();

    void Awake() {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Hex Grid Mesh";
    }

    public void AddFace(List<Vector3> faceVertices, List<int> faceTriangles, List<Vector2> faceUVs) {
        int offset = vertices.Count;
        foreach (var v in faceVertices) vertices.Add(v + transform.InverseTransformPoint(transform.position)); // Keep local
        foreach (var t in faceTriangles) triangles.Add(t + offset);
        uvs.AddRange(faceUVs);
    }

    public void ApplyMesh() {
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
    }
}
