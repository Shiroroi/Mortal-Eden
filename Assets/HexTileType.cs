using UnityEngine;

[System.Serializable]
public class HexTileType
{
    public string name;

    [Header("Height")]
    public float minHeight = 0.5f;
    public float maxHeight = 1.5f;

    [Header("Visual")]
    public Material material;

    [Range(0f, 1f)]
    public float spawnWeight = 1f;
}