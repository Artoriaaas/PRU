using UnityEngine;

public class PlacementController : MonoBehaviour
{
    private Camera _cam;

    void Start()
    {
        _cam = Camera.main;
    }

    void Update()
    {
        // Click placement is removed, we only use drag-and-drop now
    }

    public void AttemptPlacement(GameObject tileObj)
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Placement)
            return;

        if (GameManager.Instance.placedPlayerUnits >= GameManager.Instance.maxPlayerUnits)
            return;
            
        string[] parts = tileObj.name.Split('_');
        if (parts.Length == 3 && int.TryParse(parts[1], out int r))
        {
            // Check if tile is empty
            if (!IsTileOccupied(tileObj.transform.position))
            {
                GameManager.Instance.SpawnUnit(true, tileObj.transform.position);
                GameManager.Instance.placedPlayerUnits++;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdatePlacementUI();
                }
            }
            else
            {
                Debug.Log("Tile already occupied!");
            }
        }
    }

    bool IsTileOccupied(Vector3 position)
    {
        // Simple check based on distance to existing player units
        foreach (var unit in GameManager.Instance.playerUnits)
        {
            Vector3 unitPos = unit.transform.position;
            unitPos.y = position.y; // ignore height difference
            if (Vector3.Distance(unitPos, position) < 0.5f)
            {
                return true;
            }
        }
        return false;
    }
}
