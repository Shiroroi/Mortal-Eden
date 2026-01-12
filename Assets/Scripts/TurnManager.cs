using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("Turn State")]
    public int currentTurn = 1;
    public bool isPlayerTurn = true;

    [Header("Events")]
    public UnityEvent OnTurnStart;
    public UnityEvent OnTurnEnd;

    private List<Unit> allUnits = new List<Unit>();
    private List<City> allCities = new List<City>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartTurn();
    }

    public void RegisterUnit(Unit unit)
    {
        if (!allUnits.Contains(unit))
            allUnits.Add(unit);
    }

    public void UnregisterUnit(Unit unit)
    {
        allUnits.Remove(unit);
    }

    public void RegisterCity(City city)
    {
        if (!allCities.Contains(city))
            allCities.Add(city);
    }

    public void UnregisterCity(City city)
    {
        allCities.Remove(city);
    }

    void StartTurn()
    {
        Debug.Log($"=== Turn {currentTurn} Started ===");
        
        // Refresh all units
        foreach (Unit unit in allUnits)
        {
            unit.RefreshTurn();
        }

        OnTurnStart?.Invoke();
    }

    public void EndTurn()
    {
        if (!isPlayerTurn) return;

        Debug.Log($"=== Turn {currentTurn} Ending ===");

        // Process all cities
        foreach (City city in allCities)
        {
            city.ProcessTurn();
        }

        OnTurnEnd?.Invoke();

        currentTurn++;
        StartTurn();
    }

    public List<Unit> GetAllUnits()
    {
        return new List<Unit>(allUnits);
    }

    public List<City> GetAllCities()
    {
        return new List<City>(allCities);
    }
}