using UnityEngine;
using System.Collections.Generic;

public class Settler : Unit
{
    [Header("Settler Specific")]
    public bool hasBuiltCapital = false;
    public GameObject cityPrefab;
    
    private bool isBuildingCapital = false;
    private int buildTurnsRemaining = 0;

    protected override void Start()
    {
        unitName = "Settler";
        maxMovement = 2;
        visionRadius = 2;
        
        base.Start();
    }

    public override void RefreshTurn()
    {
        base.RefreshTurn();

        // Check if building capital
        if (isBuildingCapital)
        {
            buildTurnsRemaining--;
            Debug.Log($"Building capital... {buildTurnsRemaining} turns remaining");

            if (buildTurnsRemaining <= 0)
            {
                CompleteCityConstruction();
            }
        }
    }

    public bool CanBuildCapital()
    {
        if (hasBuiltCapital)
        {
            Debug.Log("Already built capital");
            return false;
        }

        if (isBuildingCapital)
        {
            Debug.Log("Already building capital");
            return false;
        }

        // Check if on mountain
        HexTileType currentTile = hexGrid.GetTileAt(gridPosition);
        if (currentTile != null && currentTile.name == "Mountains")
        {
            Debug.Log("Cannot build capital on mountains");
            return false;
        }
        
        // Check if adjacent to alien city
        List<Vector2Int> neighbors = GetNeighbors(gridPosition);
        foreach (Vector2Int neighbor in neighbors)
        {
            HexTileType tile = hexGrid.GetTileAt(neighbor);
            if (tile != null && tile.name == "Alien Base")
            {
                Debug.Log("Cannot build capital adjacent to alien base");
                return false;
            }
        }

        return true;
    }

    private List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
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

    public void StartBuildingCapital()
    {
        if (!CanBuildCapital())
            return;

        isBuildingCapital = true;
        buildTurnsRemaining = 1; // Takes 1 full turn
        remainingMovement = 0; // Can't move while building

        Debug.Log($"Settler started building capital at {gridPosition}. Will complete next turn.");
    }

    private void CompleteCityConstruction()
    {
        Debug.Log($"Capital construction complete at {gridPosition}!");

        // Create the city
        if (cityPrefab != null)
        {
            Vector3 localHexPos = hexGrid.GetHexPosition(gridPosition.x, gridPosition.y);
            Vector3 worldPos = hexGrid.transform.position + localHexPos + Vector3.up * 0.5f;
            GameObject cityObj = Instantiate(cityPrefab, worldPos, Quaternion.identity);
            
            City city = cityObj.GetComponent<City>();
            if (city != null)
            {
                city.gridPosition = gridPosition;
                city.hexGrid = hexGrid;
                city.cityName = "Capital";
            }
        }

        // Mark as built
        hasBuiltCapital = true;
        isBuildingCapital = false;

        // Destroy the settler
        Destroy(gameObject);
    }

    // Override MoveTo to prevent movement while building
    public override void MoveTo(Vector2Int targetPos)
    {
        if (isBuildingCapital)
        {
            Debug.Log("Cannot move while building capital");
            return;
        }

        base.MoveTo(targetPos);
    }
}