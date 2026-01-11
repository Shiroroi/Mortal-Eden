using UnityEngine;
using System.Collections.Generic;

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
    
    [Header("Tile Types")]
    public HexTileType[] tileTypes;

    [Header("Generation Settings")]
    [Range(1, 10)]
    public int mountainClusters = 3;
    [Range(0f, 0.3f)]
    public float lakeChance = 0.1f;

    private HexTileType[,] tileMap;

    // Tile type indices (set these in inspector or find by name)
    private const string PLAINS = "Plains";
    private const string FOREST = "Forest";
    private const string MOUNTAINS = "Mountains";
    private const string RIVER = "River";
    private const string ALIEN_BASE = "Alien Base";


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
        GenerateTileMap();
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

    void GenerateTileMap()
    {
        tileMap = new HexTileType[gridSize.x, gridSize.y];
        int totalTiles = gridSize.x * gridSize.y;

        // Calculate exact tile counts from spawn weights
        float totalWeight = 0f;
        foreach (var t in tileTypes) totalWeight += t.spawnWeight;

        Dictionary<string, int> tileCounts = new Dictionary<string, int>();
        int assigned = 0;

        foreach (var type in tileTypes)
        {
            float percentage = type.spawnWeight / totalWeight;
            int count = Mathf.RoundToInt(totalTiles * percentage);
            tileCounts[type.name] = count;
            assigned += count;
        }

        // Adjust for rounding errors
        if (assigned != totalTiles && tileTypes.Length > 0)
        {
            tileCounts[tileTypes[0].name] += (totalTiles - assigned);
        }

        // 1. Fill with base terrain (Plains/Forest)
        FillBaseTerrain(tileCounts);

        // 2. Place mountain clusters
        PlaceMountainClusters(tileCounts);

        // 3. Generate rivers
        GenerateRivers(tileCounts);

        // 4. Place single alien base
        PlaceAlienBase(tileCounts);
    }

    void FillBaseTerrain(Dictionary<string, int> tileCounts)
    {
        // Fill entire map with plains first
        HexTileType plains = GetTileType(PLAINS);
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                tileMap[x, y] = plains;
            }
        }

        // Place forest tiles randomly
        HexTileType forest = GetTileType(FOREST);
        if (forest != null && tileCounts.ContainsKey(FOREST))
        {
            int forestCount = tileCounts[FOREST];
            int placed = 0;

            while (placed < forestCount)
            {
                int x = Random.Range(0, gridSize.x);
                int y = Random.Range(0, gridSize.y);

                if (tileMap[x, y].name == PLAINS)
                {
                    tileMap[x, y] = forest;
                    placed++;
                }
            }
        }
    }

    void PlaceMountainClusters(Dictionary<string, int> tileCounts)
    {
        HexTileType mountain = GetTileType(MOUNTAINS);
        if (mountain == null || !tileCounts.ContainsKey(MOUNTAINS)) return;

        int targetCount = tileCounts[MOUNTAINS];
        int tilesPerCluster = Mathf.Max(1, targetCount / mountainClusters);
        int placed = 0;

        for (int i = 0; i < mountainClusters && placed < targetCount; i++)
        {
            int seedX = Random.Range(0, gridSize.x);
            int seedY = Random.Range(0, gridSize.y);

            // Flood fill cluster
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            queue.Enqueue(new Vector2Int(seedX, seedY));
            visited.Add(new Vector2Int(seedX, seedY));

            int clusterPlaced = 0;
            int clusterTarget = Mathf.Min(tilesPerCluster, targetCount - placed);

            while (queue.Count > 0 && clusterPlaced < clusterTarget)
            {
                Vector2Int current = queue.Dequeue();
                
                if (tileMap[current.x, current.y].name == PLAINS || 
                    tileMap[current.x, current.y].name == FOREST)
                {
                    tileMap[current.x, current.y] = mountain;
                    clusterPlaced++;
                    placed++;

                    // Add neighbors
                    foreach (Vector2Int neighbor in GetNeighbors(current.x, current.y))
                    {
                        if (!visited.Contains(neighbor) && Random.value < 0.7f)
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }
        }
    }

    void GenerateRivers(Dictionary<string, int> tileCounts)
    {
        HexTileType river = GetTileType(RIVER);
        if (river == null || !tileCounts.ContainsKey(RIVER)) return;

        int targetCount = tileCounts[RIVER];
        int placed = 0;
        int attempts = 0;
        int maxAttempts = 50;

        while (placed < targetCount && attempts < maxAttempts)
        {
            attempts++;
            
            // Start from a random edge
            Vector2Int start = GetRandomEdgePosition();
            Vector2Int current = start;

            List<Vector2Int> riverPath = new List<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            int maxLength = gridSize.x + gridSize.y;
            int steps = 0;

            // Walk randomly but try to reach target count
            while (steps < maxLength && placed < targetCount)
            {
                if (!visited.Contains(current))
                {
                    visited.Add(current);
                    
                    // Only add if not mountain
                    if (tileMap[current.x, current.y].name != MOUNTAINS)
                    {
                        riverPath.Add(current);
                        
                        // Occasionally create a lake (2-3 tile cluster)
                        if (Random.value < lakeChance && placed < targetCount - 2)
                        {
                            foreach (Vector2Int neighbor in GetNeighbors(current.x, current.y))
                            {
                                if (!visited.Contains(neighbor) && 
                                    tileMap[neighbor.x, neighbor.y].name != MOUNTAINS &&
                                    Random.value < 0.5f)
                                {
                                    riverPath.Add(neighbor);
                                    visited.Add(neighbor);
                                }
                            }
                        }
                    }
                }

                // Pick next tile
                List<Vector2Int> neighbors = GetNeighbors(current.x, current.y);
                if (neighbors.Count == 0) break;

                Vector2Int next = neighbors[Random.Range(0, neighbors.Count)];
                current = next;
                steps++;

                // Exit if we reach another edge
                if (IsEdge(current.x, current.y) && steps > 3)
                    break;
            }

            // Apply river to map
            foreach (Vector2Int pos in riverPath)
            {
                if (placed >= targetCount) break;
                
                if (tileMap[pos.x, pos.y].name != MOUNTAINS)
                {
                    tileMap[pos.x, pos.y] = river;
                    placed++;
                }
            }
        }
    }

    void PlaceAlienBase(Dictionary<string, int> tileCounts)
    {
        HexTileType alienBase = GetTileType(ALIEN_BASE);
        if (alienBase == null || !tileCounts.ContainsKey(ALIEN_BASE)) return;

        int targetCount = tileCounts[ALIEN_BASE];
        if (targetCount == 0) return;

        // Pick center point (avoid edges)
        int centerX = Random.Range(2, Mathf.Max(3, gridSize.x - 2));
        int centerY = Random.Range(2, Mathf.Max(3, gridSize.y - 2));

        // Grow base from center
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        queue.Enqueue(new Vector2Int(centerX, centerY));
        visited.Add(new Vector2Int(centerX, centerY));

        int placed = 0;
        while (queue.Count > 0 && placed < targetCount)
        {
            Vector2Int current = queue.Dequeue();
            tileMap[current.x, current.y] = alienBase;
            placed++;

            // Expand to neighbors
            foreach (Vector2Int neighbor in GetNeighbors(current.x, current.y))
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
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

                HexTileType type = tileMap[x, y];
                float tileHeight = Random.Range(type.minHeight, type.maxHeight);

                HexRenderer hex = tile.AddComponent<HexRenderer>();
                hex.outerSize = outerSize;
                hex.innerSize = innerSize;
                hex.height = tileHeight;
                hex.isFlatTopped = isFlatTopped;
                hex.material = type.material;

                hex.DrawMesh();
            }
        }
    }


    // -------------------- HELPERS --------------------

    List<Vector2Int> GetNeighbors(int x, int y)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        // Hex grid neighbor offsets (flat-topped)
        int[,] offsets = isFlatTopped
            ? new int[,] {
                { 1, 0 }, { -1, 0 },
                { 0, 1 }, { 0, -1 },
                { 1, x % 2 == 0 ? -1 : 1 },
                { -1, x % 2 == 0 ? -1 : 1 }
            }
            : new int[,] {
                { 1, 0 }, { -1, 0 },
                { 0, 1 }, { 0, -1 },
                { y % 2 == 0 ? -1 : 1, 1 },
                { y % 2 == 0 ? -1 : 1, -1 }
            };

        for (int i = 0; i < 6; i++)
        {
            int nx = x + offsets[i, 0];
            int ny = y + offsets[i, 1];

            if (nx >= 0 && nx < gridSize.x && ny >= 0 && ny < gridSize.y)
                neighbors.Add(new Vector2Int(nx, ny));
        }

        return neighbors;
    }

    Vector2Int GetRandomEdgePosition()
    {
        int edge = Random.Range(0, 4);
        return edge switch
        {
            0 => new Vector2Int(Random.Range(0, gridSize.x), 0), // top
            1 => new Vector2Int(Random.Range(0, gridSize.x), gridSize.y - 1), // bottom
            2 => new Vector2Int(0, Random.Range(0, gridSize.y)), // left
            _ => new Vector2Int(gridSize.x - 1, Random.Range(0, gridSize.y)) // right
        };
    }

    bool IsEdge(int x, int y)
    {
        return x == 0 || x == gridSize.x - 1 || y == 0 || y == gridSize.y - 1;
    }

    HexTileType GetTileType(string typeName)
    {
        foreach (var type in tileTypes)
        {
            if (type.name == typeName)
                return type;
        }
        return tileTypes.Length > 0 ? tileTypes[0] : null;
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