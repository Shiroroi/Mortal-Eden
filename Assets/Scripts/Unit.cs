using UnityEngine;
using System.Collections.Generic;

public class Unit : MonoBehaviour
{
    [Header("Unit Stats")]
    public string unitName = "Unit";
    public int maxMovement = 2;
    public int visionRadius = 2;

    [Header("Current State")]
    public Vector2Int gridPosition;
    public int remainingMovement;
    public bool isSelected = false;

    [Header("References")]
    public HexGridLayout hexGrid;
    public Material selectedMaterial;
    public Material normalMaterial;

    protected MeshRenderer meshRenderer;
    private Material originalMaterial;

    protected virtual void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer && meshRenderer.material)
            originalMaterial = meshRenderer.material;

        remainingMovement = maxMovement;

        if (hexGrid == null)
            hexGrid = Object.FindFirstObjectByType<HexGridLayout>();

        // Register with turn manager
        if (TurnManager.Instance != null)
            TurnManager.Instance.RegisterUnit(this);

        // Set initial grid position based on world position
        UpdateGridPositionFromWorld();
    }

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.UnregisterUnit(this);
    }

    public virtual void RefreshTurn()
    {
        remainingMovement = maxMovement;
        Debug.Log($"{unitName} movement refreshed: {remainingMovement}/{maxMovement}");
    }

    public void Select()
    {
        isSelected = true;
        if (meshRenderer && selectedMaterial)
            meshRenderer.material = selectedMaterial;

        Debug.Log($"{unitName} selected at {gridPosition}");
    }

    public void Deselect()
    {
        isSelected = false;
        if (meshRenderer && originalMaterial)
            meshRenderer.material = originalMaterial;
    }

    public bool CanMoveTo(Vector2Int targetPos)
    {
        if (remainingMovement <= 0)
        {
            Debug.Log("No movement remaining");
            return false;
        }

        // Check if target is adjacent
        List<Vector2Int> neighbors = GetNeighbors(gridPosition);
        if (!neighbors.Contains(targetPos))
        {
            Debug.Log("Target is not adjacent");
            return false;
        }

        // Check if target is in bounds
        if (targetPos.x < 0 || targetPos.x >= hexGrid.gridSize.x ||
            targetPos.y < 0 || targetPos.y >= hexGrid.gridSize.y)
        {
            Debug.Log("Target out of bounds");
            return false;
        }

        // Check if target is walkable (not mountain)
        HexTileType targetTile = hexGrid.GetTileAt(targetPos);
        if (targetTile != null && targetTile.name == "Mountains")
        {
            Debug.Log("Cannot move to mountains");
            return false;
        }

        return true;
    }

    public virtual void MoveTo(Vector2Int targetPos)
    {
        if (!CanMoveTo(targetPos))
            return;

        // Update grid position
        gridPosition = targetPos;

        // Move the GameObject to the hex position (account for grid's world position)
        Vector3 localHexPos = hexGrid.GetHexPosition(targetPos.x, targetPos.y);
        Vector3 worldPos = hexGrid.transform.position + localHexPos + Vector3.up * 0.5f;
        transform.position = worldPos;

        // Consume movement
        remainingMovement--;

        Debug.Log($"{unitName} moved to {gridPosition}. Remaining movement: {remainingMovement}");

        // You'll trigger fog of war reveal here later
    }

    private void UpdateGridPositionFromWorld()
    {
        // Find closest hex position
        float closestDist = float.MaxValue;
        Vector2Int closest = Vector2Int.zero;

        for (int y = 0; y < hexGrid.gridSize.y; y++)
        {
            for (int x = 0; x < hexGrid.gridSize.x; x++)
            {
                Vector3 localHexPos = hexGrid.GetHexPosition(x, y);
                Vector3 worldHexPos = hexGrid.transform.position + localHexPos;
                float dist = Vector3.Distance(transform.position, worldHexPos);
                
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = new Vector2Int(x, y);
                }
            }
        }

        gridPosition = closest;
        Debug.Log($"{unitName} initialized at grid position {gridPosition}");
    }

    private List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        // Use same neighbor logic as HexGridLayout
        int x = pos.x;
        int y = pos.y;

        int[,] offsets = hexGrid.isFlatTopped
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

            if (nx >= 0 && nx < hexGrid.gridSize.x && ny >= 0 && ny < hexGrid.gridSize.y)
                neighbors.Add(new Vector2Int(nx, ny));
        }

        return neighbors;
    }

    // Helper to get valid movement tiles (for UI highlighting later)
    public List<Vector2Int> GetValidMovementTiles()
    {
        List<Vector2Int> validTiles = new List<Vector2Int>();
        
        if (remainingMovement <= 0)
            return validTiles;

        // For now, just return adjacent tiles
        // Later you can expand this for movement > 1
        validTiles = GetNeighbors(gridPosition);

        return validTiles;
    }
}