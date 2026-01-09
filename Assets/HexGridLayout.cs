using UnityEngine;

[ExecuteAlways]
public class HexGridLayout : MonoBehaviour
{
    [Header("Grid")]
    public Vector2Int gridSize = new(5, 5);
    public bool generateAtStart = true;

    [Header("Hex Settings")]
    public float outerSize = 1f;
    public float innerSize = 0f;
    public float height = 1f;
    public bool isFlatTopped = true;
    public Material material;

    // -------------------- UNITY --------------------

    void Start()
    {
        if (!Application.isPlaying) return;
        if (generateAtStart)
            RebuildGrid();
    }

#if UNITY_EDITOR
    [ContextMenu("Rebuild Grid")]
#endif
    public void RebuildGrid()
    {
        ClearGrid();
        BuildGrid();
    }

    // -------------------- GRID --------------------

    void ClearGrid()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            while (transform.childCount > 0)
                DestroyImmediate(transform.GetChild(0).gameObject);
            return;
        }
#endif

        foreach (Transform child in transform)
            Destroy(child.gameObject);
    }

    void BuildGrid()
    {
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                GameObject tile = new($"Hex {x},{y}");
                tile.transform.SetParent(transform, false);
                tile.transform.localPosition = GetHexPosition(x, y);

                HexRenderer hex = tile.AddComponent<HexRenderer>();
                hex.outerSize = outerSize;
                hex.innerSize = innerSize;
                hex.height = height;
                hex.isFlatTopped = isFlatTopped;
                hex.material = material;

                hex.DrawMesh();
            }
        }
    }

    // -------------------- POSITIONING --------------------

    Vector3 GetHexPosition(int x, int y)
    {
        float size = outerSize;

        if (isFlatTopped)
        {
            float width = 2f * size;
            float height = Mathf.Sqrt(3f) * size;

            float horiz = width * 0.75f;
            float vert = height;

            float offset = (x % 2 == 0) ? height * 0.5f : 0f;

            return new Vector3(
                x * horiz,
                0f,
                -(y * vert + offset)
            );
        }
        else
        {
            float width = Mathf.Sqrt(3f) * size;
            float height = 2f * size;

            float horiz = width;
            float vert = height * 0.75f;

            float offset = (y % 2 == 0) ? width * 0.5f : 0f;

            return new Vector3(
                x * horiz + offset,
                0f,
                -(y * vert)
            );
        }
    }
}
