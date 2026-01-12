using UnityEngine;
using System.Collections.Generic;

public class City : MonoBehaviour
{
    [Header("City Info")]
    public string cityName = "City";
    public Vector2Int gridPosition;

    [Header("Stats")]
    public int population = 5;
    public int baseProduction = 5;
    public int baseScience = 3;
    public int territoryRadius = 1;

    [Header("Current State")]
    public int currentProduction = 0;

    [Header("References")]
    public HexGridLayout hexGrid;

    private List<Building> constructedBuildings = new List<Building>();

    void Start()
    {
        if (hexGrid == null)
            hexGrid = Object.FindAnyObjectByType<HexGridLayout>();

        // Register with turn manager
        if (TurnManager.Instance != null)
            TurnManager.Instance.RegisterCity(this);

        Debug.Log($"{cityName} founded at {gridPosition}");
    }

    void OnDestroy()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.UnregisterCity(this);
    }

    public void ProcessTurn()
    {
        int totalProduction = GetTotalProduction();
        int totalScience = GetTotalScience();

        Debug.Log($"{cityName} - Production: {totalProduction}, Science: {totalScience}");

        // Accumulate production (you'll add queue system later)
        currentProduction += totalProduction;

        // Process buildings (stress, etc)
        foreach (Building building in constructedBuildings)
        {
            building.OnTurnTick(this);
        }
    }

    public int GetTotalProduction()
    {
        int total = baseProduction;
        
        foreach (Building building in constructedBuildings)
        {
            total += building.productionBonus;
        }

        // You'll add tile production modifiers here later (scarred tiles = -2)
        
        return total;
    }

    public int GetTotalScience()
    {
        int total = baseScience;
        
        foreach (Building building in constructedBuildings)
        {
            total += building.scienceBonus;
        }
        
        return total;
    }

    public void AddBuilding(Building building)
    {
        constructedBuildings.Add(building);
        building.OnBuilt(this);
        Debug.Log($"{cityName} completed {building.buildingName}");
    }

    public List<Vector2Int> GetTerritoryTiles()
    {
        List<Vector2Int> territory = new List<Vector2Int>();
        territory.Add(gridPosition); // City tile itself

        // Add tiles within radius
        for (int dx = -territoryRadius; dx <= territoryRadius; dx++)
        {
            for (int dy = -territoryRadius; dy <= territoryRadius; dy++)
            {
                if (dx == 0 && dy == 0) continue; // Already added city tile

                Vector2Int tile = new Vector2Int(gridPosition.x + dx, gridPosition.y + dy);
                
                // Check if in bounds
                if (tile.x >= 0 && tile.x < hexGrid.gridSize.x &&
                    tile.y >= 0 && tile.y < hexGrid.gridSize.y)
                {
                    // Simple radius check (you might want hex distance instead)
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) <= territoryRadius)
                    {
                        territory.Add(tile);
                    }
                }
            }
        }

        return territory;
    }
}
