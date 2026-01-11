using System.Collections.Generic;
using UnityEngine;

public class Face
{
    public List<Vector3> vertices;
    public List<int> triangles;
    public List<Vector2> uvs;

    public Face(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
    {
        this.vertices = vertices;
        this.triangles = triangles;
        this.uvs = uvs;
    }
}

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class HexRenderer : MonoBehaviour
{
    [Header("Hex Settings")]
    public float innerSize = 0.5f;   // hole radius (set to 0 for solid hex)
    public float outerSize = 1f;
    public float height = 1f;
    public bool isFlatTopped = true;

    [Header("Rendering")]
    public Material material;

    private Mesh m_mesh;
    private MeshFilter m_meshFilter;
    private MeshRenderer m_meshRenderer;

    private List<Face> m_faces;

    

    private void Init()
    {
        if (!m_meshFilter)
            m_meshFilter = GetComponent<MeshFilter>();

        if (!m_meshRenderer)
            m_meshRenderer = GetComponent<MeshRenderer>();

        if (!m_mesh)
        {
            m_mesh = new Mesh { name = "Hex" };
            m_meshFilter.sharedMesh = m_mesh;
        }

        if (material)
            m_meshRenderer.sharedMaterial = material;
    }

    public void DrawMesh()
    {
        if (!m_mesh) return;

        if (material && m_meshRenderer.sharedMaterial != material)
            m_meshRenderer.sharedMaterial = material;

        m_mesh.Clear();
        DrawFaces();
        CombineFaces();
    }

// Add this to HexRenderer.cs
    public Mesh GetGeneratedMesh()
    {
        // Ensure components exist even if not placed in scene
        m_mesh = new Mesh { name = "HexData" };
        DrawFaces();
        CombineFaces();
        return m_mesh;
    }
    public void DrawFaces()
    {
        m_faces = new List<Face>();

        float top = height / 2f;
        float bottom = -height / 2f;

        // TOP
        for (int i = 0; i < 6; i++)
            m_faces.Add(CreateRingFace(innerSize, outerSize, top, i, false));

        // BOTTOM
        for (int i = 0; i < 6; i++)
            m_faces.Add(CreateRingFace(innerSize, outerSize, bottom, i, true));

        // OUTER SIDES
        for (int i = 0; i < 6; i++)
        {
            Vector3 tA = GetPoint(outerSize, top, i);
            Vector3 tB = GetPoint(outerSize, top, (i + 1) % 6);
            Vector3 bB = GetPoint(outerSize, bottom, (i + 1) % 6);
            Vector3 bA = GetPoint(outerSize, bottom, i);

            m_faces.Add(new Face(
                new List<Vector3> { tA, tB, bB, bA },
                new List<int> { 0, 1, 2, 2, 3, 0 },
                QuadUV()
            ));
        }

        // INNER SIDES (only if hollow)
        if (innerSize > 0f)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector3 tA = GetPoint(innerSize, top, i);
                Vector3 tB = GetPoint(innerSize, top, (i + 1) % 6);
                Vector3 bB = GetPoint(innerSize, bottom, (i + 1) % 6);
                Vector3 bA = GetPoint(innerSize, bottom, i);

                m_faces.Add(new Face(
                    new List<Vector3> { tA, bA, bB, tB },
                    new List<int> { 0, 1, 2, 2, 3, 0 },
                    QuadUV()
                ));
            }
        }
    }

    private Face CreateRingFace(float innerRad, float outerRad, float y, int i, bool flip)
    {
        Vector3 A = GetPoint(innerRad, y, i);
        Vector3 B = GetPoint(innerRad, y, (i + 1) % 6);
        Vector3 C = GetPoint(outerRad, y, (i + 1) % 6);
        Vector3 D = GetPoint(outerRad, y, i);

        return new Face(
            new List<Vector3> { A, B, C, D },
            flip
                ? new List<int> { 0, 3, 2, 2, 1, 0 }
                : new List<int> { 0, 1, 2, 2, 3, 0 },
            QuadUV()
        );
    }

    private void CombineFaces()
    {
        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector2> uvs = new();

        foreach (var face in m_faces)
        {
            int offset = vertices.Count;
            vertices.AddRange(face.vertices);
            uvs.AddRange(face.uvs);

            foreach (int t in face.triangles)
                triangles.Add(t + offset);
        }

        m_mesh.SetVertices(vertices);
        m_mesh.SetTriangles(triangles, 0);
        m_mesh.SetUVs(0, uvs);
        m_mesh.RecalculateNormals();
        m_mesh.RecalculateBounds();
    }

    private Vector3 GetPoint(float radius, float y, int index)
    {
        float angle = isFlatTopped
            ? 60f * index
            : 60f * index - 30f;

        float rad = Mathf.Deg2Rad * angle;
        return new Vector3(
            radius * Mathf.Cos(rad),
            y,
            radius * Mathf.Sin(rad)
        );
    }

    private List<Vector2> QuadUV()
    {
        return new List<Vector2>
        {
            new(0,1),
            new(1,1),
            new(1,0),
            new(0,0)
        };
    }
}
