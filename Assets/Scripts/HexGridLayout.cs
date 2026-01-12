using UnityEngine;
using System.Collections.Generic;
using System.Linq;
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

    [Header("Player Spawn")]
    public GameObject settlerPrefab;
    public int minDistanceFromAlienBase = 5;

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
        SpawnPlayer();
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

    public void BuildGrid()
    {
        ClearGrid();
        GenerateTileMap();

        // 1. Group tiles by their material
        Dictionary<Material, List<CombineInstance>> materialBatches = new Dictionary<Material, List<CombineInstance>>();

        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                HexTileType type = tileMap[x, y];
                Vector3 pos = GetHexPosition(x, y);

                // Create a temporary mesh for this one tile
                Mesh tileMesh = CreateTileMesh(type, x, y);

                CombineInstance combine = new CombineInstance();
                combine.mesh = tileMesh;
                combine.transform = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);

                if (!materialBatches.ContainsKey(type.material))
                    materialBatches[type.material] = new List<CombineInstance>();

                materialBatches[type.material].Add(combine);
            }
        }

        // 2. Combine all meshes of the same material into one single object
        foreach (var batch in materialBatches)
        {
            GameObject cluster = new GameObject("MaterialBatch_" + batch.Key.name);
            cluster.transform.SetParent(this.transform, false);
            cluster.layer = LayerMask.NameToLayer("Hex"); // Set layer for raycasting
            
            MeshFilter mf = cluster.AddComponent<MeshFilter>();
            MeshRenderer mr = cluster.AddComponent<MeshRenderer>();
            MeshCollider mc = cluster.AddComponent<MeshCollider>(); // Add collider for clicking
            
            Mesh combinedMesh = new Mesh();
            combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Allows > 65k vertices
            combinedMesh.CombineMeshes(batch.Value.ToArray(), true, true);
            
            mf.sharedMesh = combinedMesh;
            mr.sharedMaterial = batch.Key;
            mc.sharedMesh = combinedMesh; // Assign mesh to collider
        }
    }

    // Helper to generate a single hex mesh data
    Mesh CreateTileMesh(HexTileType type, int x, int y) 
    {
        GameObject tempGo = new GameObject("TempHex");
        HexRenderer renderer = tempGo.AddComponent<HexRenderer>();
    
        // Set the settings
        renderer.outerSize = outerSize;
        renderer.innerSize = innerSize;
        // Use the type settings for height variation
        renderer.height = Random.Range(type.minHeight, type.maxHeight); 
        renderer.isFlatTopped = isFlatTopped;
    
        // Get the mesh directly
        Mesh m = renderer.GetGeneratedMesh();
    
        // Clean up the temporary object immediately
        DestroyImmediate(tempGo);
        return m;
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

    public Vector3 GetHexPosition(int x, int y)
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

    // -------------------- PLAYER SPAWNING --------------------

    void SpawnPlayer()
    {
        if (!Application.isPlaying) return; // Don't spawn in editor mode
        
        if (settlerPrefab == null)
        {
            Debug.LogWarning("No Settler Prefab assigned to HexGridLayout!");
            return;
        }

        Vector2Int spawnPos = FindValidSpawnPosition();
        
        if (spawnPos.x == -1)
        {
            Debug.LogError("Could not find valid spawn position for player!");
            return;
        }

        // Double-check the tile at spawn position
        HexTileType spawnTile = GetTileAt(spawnPos);
        Debug.Log($"Spawning on tile: {spawnTile.name} at position {spawnPos}");

        // Spawn the settler with world position offset from HexGridLayout's position
        Vector3 localHexPos = GetHexPosition(spawnPos.x, spawnPos.y);
        Vector3 worldPos = transform.position + localHexPos + Vector3.up * 0.5f;
        GameObject settlerObj = Instantiate(settlerPrefab, worldPos, Quaternion.identity);
        
        Settler settler = settlerObj.GetComponent<Settler>();
        if (settler != null)
        {
            settler.gridPosition = spawnPos;
            settler.hexGrid = this;
        }

        Debug.Log($"Player spawned at grid {spawnPos}, world pos {worldPos} (Plains tile, {GetDistanceToNearestAlienBase(spawnPos)} tiles from alien base)");
    }

    Vector2Int FindValidSpawnPosition()
    {
        // Find all alien base positions first
        List<Vector2Int> alienBasePositions = new List<Vector2Int>();
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                if (tileMap[x, y].name == ALIEN_BASE)
                {
                    alienBasePositions.Add(new Vector2Int(x, y));
                }
            }
        }

        // Find all valid plains tiles (NOT river, NOT forest, NOT mountain, NOT alien base)
        List<Vector2Int> validSpawns = new List<Vector2Int>();
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                // Only spawn on actual plains tiles
                if (tileMap[x, y].name == PLAINS)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    
                    // Check distance to all alien bases
                    bool farEnough = true;
                    foreach (Vector2Int alienPos in alienBasePositions)
                    {
                        int distance = GetHexDistance(pos, alienPos);
                        if (distance < minDistanceFromAlienBase)
                        {
                            farEnough = false;
                            break;
                        }
                    }

                    if (farEnough)
                    {
                        validSpawns.Add(pos);
                    }
                }
            }
        }

        if (validSpawns.Count == 0)
        {
            Debug.LogWarning($"No valid spawn positions found! Searched for '{PLAINS}' tiles. Try reducing minDistanceFromAlienBase or check your tile type names.");
            
            // Debug: Print what tiles we actually have
            Dictionary<string, int> tileCounts = new Dictionary<string, int>();
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    string name = tileMap[x, y].name;
                    if (!tileCounts.ContainsKey(name))
                        tileCounts[name] = 0;
                    tileCounts[name]++;
                }
            }
            Debug.Log("Tile distribution: " + string.Join(", ", tileCounts));
            
            return new Vector2Int(-1, -1);
        }

        // Pick random valid spawn
        return validSpawns[Random.Range(0, validSpawns.Count)];
    }

    int GetHexDistance(Vector2Int a, Vector2Int b)
    {
        // Convert to cube coordinates for hex distance
        Vector3Int cubeA = OffsetToCube(a);
        Vector3Int cubeB = OffsetToCube(b);
        
        return (Mathf.Abs(cubeA.x - cubeB.x) + Mathf.Abs(cubeA.y - cubeB.y) + Mathf.Abs(cubeA.z - cubeB.z)) / 2;
    }

    Vector3Int OffsetToCube(Vector2Int offset)
    {
        int x, y, z;
        
        if (isFlatTopped)
        {
            x = offset.x - (offset.y - (offset.y & 1)) / 2;
            z = offset.y;
            y = -x - z;
        }
        else
        {
            x = offset.x;
            z = offset.y - (offset.x - (offset.x & 1)) / 2;
            y = -x - z;
        }
        
        return new Vector3Int(x, y, z);
    }

    int GetDistanceToNearestAlienBase(Vector2Int pos)
    {
        int minDist = int.MaxValue;
        
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                if (tileMap[x, y].name == ALIEN_BASE)
                {
                    int dist = GetHexDistance(pos, new Vector2Int(x, y));
                    minDist = Mathf.Min(minDist, dist);
                }
            }
        }
        
        return minDist;
    }

    // Helper to get tile type at position (useful for other systems)
    public HexTileType GetTileAt(int x, int y)
    {
        if (x < 0 || x >= gridSize.x || y < 0 || y >= gridSize.y)
            return null;
            
        return tileMap[x, y];
    }

    public HexTileType GetTileAt(Vector2Int pos)
    {
        return GetTileAt(pos.x, pos.y);
    }
}