using UnityEngine;

[System.Serializable]
public class Building
{
    public string buildingName;
    public int productionCost;
    public int productionBonus;
    public int scienceBonus;
    public int edenStressOnBuild;
    public int edenStressPerTurn;

    public virtual void OnBuilt(City city)
    {
        // Apply one-time stress
        if (edenStressOnBuild > 0)
        {
            // You'll add EdenStressManager call here later
            Debug.Log($"{buildingName} added {edenStressOnBuild} stress on build");
        }
    }

    public virtual void OnTurnTick(City city)
    {
        // Apply per-turn stress
        if (edenStressPerTurn > 0)
        {
            // You'll add EdenStressManager call here later
            Debug.Log($"{buildingName} adding {edenStressPerTurn} stress this turn");
        }
    }
}

// Specific building types
[System.Serializable]
public class Factory : Building
{
    public Factory()
    {
        buildingName = "Factory";
        productionCost = 20;
        productionBonus = 4;
        scienceBonus = 0;
        edenStressOnBuild = 0;
        edenStressPerTurn = 2;
    }
}

[System.Serializable]
public class ResearchLab : Building
{
    public ResearchLab()
    {
        buildingName = "Research Lab";
        productionCost = 15;
        productionBonus = 0;
        scienceBonus = 4;
        edenStressOnBuild = 0;
        edenStressPerTurn = 0;
    }
}

[System.Serializable]
public class FloodBarrier : Building
{
    public FloodBarrier()
    {
        buildingName = "Flood Barrier";
        productionCost = 10;
        productionBonus = 0;
        scienceBonus = 0;
        edenStressOnBuild = 1;
        edenStressPerTurn = 0;
    }
}