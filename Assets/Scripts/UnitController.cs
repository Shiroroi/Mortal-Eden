using UnityEngine;

/// <summary>
/// Handles unit selection and movement via mouse clicks
/// Attach this to your main camera or a manager object
/// </summary>
public class UnitController : MonoBehaviour
{
    private Unit selectedUnit = null;
    public LayerMask unitLayer;
    public LayerMask hexLayer;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left click
        {
            HandleClick();
        }

        // Right click to deselect
        if (Input.GetMouseButtonDown(1))
        {
            DeselectUnit();
        }

        // Keyboard shortcut for settler action
        if (Input.GetKeyDown(KeyCode.B) && selectedUnit != null)
        {
            Settler settler = selectedUnit as Settler;
            if (settler != null)
            {
                settler.StartBuildingCapital();
            }
        }
    }

    void HandleClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // First, try to click on a unit
        if (Physics.Raycast(ray, out hit, 1000f, unitLayer))
        {
            Unit unit = hit.collider.GetComponent<Unit>();
            if (unit != null)
            {
                SelectUnit(unit);
                return;
            }
        }

        // If we have a selected unit, try to move it
        if (selectedUnit != null && Physics.Raycast(ray, out hit, 1000f, hexLayer))
        {
            // Get the hex grid position from world position
            Vector3 hitPoint = hit.point;
            Vector2Int targetPos = WorldToGridPosition(hitPoint);
            
            if (targetPos.x != -1) // Valid position
            {
                selectedUnit.MoveTo(targetPos);
            }
        }
    }

    void SelectUnit(Unit unit)
    {
        // Deselect previous
        if (selectedUnit != null)
        {
            selectedUnit.Deselect();
        }

        selectedUnit = unit;
        selectedUnit.Select();
    }

    void DeselectUnit()
    {
        if (selectedUnit != null)
        {
            selectedUnit.Deselect();
            selectedUnit = null;
        }
    }

    Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        HexGridLayout hexGrid = Object.FindAnyObjectByType<HexGridLayout>();
        if (hexGrid == null) return new Vector2Int(-1, -1);

        // Find closest hex
        float closestDist = float.MaxValue;
        Vector2Int closest = new Vector2Int(-1, -1);

        for (int y = 0; y < hexGrid.gridSize.y; y++)
        {
            for (int x = 0; x < hexGrid.gridSize.x; x++)
            {
                Vector3 hexPos = hexGrid.GetHexPosition(x, y);
                float dist = Vector3.Distance(worldPos, hexPos);
                
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = new Vector2Int(x, y);
                }
            }
        }

        return closest;
    }
}