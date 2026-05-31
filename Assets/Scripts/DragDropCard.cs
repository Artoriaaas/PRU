using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragDropCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform _dragIcon;
    private Canvas _canvas;

    void Start()
    {
        _canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Placement) return;
        if (GameManager.Instance.placedPlayerUnits >= GameManager.Instance.maxPlayerUnits) return;

        // Create a temporary icon to follow mouse
        GameObject iconObj = new GameObject("DragIcon");
        iconObj.transform.SetParent(_canvas.transform, false);
        Image img = iconObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 1f, 0.7f); // semi-transparent blue
        _dragIcon = iconObj.GetComponent<RectTransform>();
        _dragIcon.sizeDelta = new Vector2(50, 50);

        UpdateDragPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragIcon != null)
        {
            UpdateDragPosition(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_dragIcon != null)
        {
            Destroy(_dragIcon.gameObject);
            _dragIcon = null;

            // Check if dropped on grid
            Ray ray = Camera.main.ScreenPointToRay(eventData.position);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider != null && hit.collider.name.StartsWith("Tile_"))
                {
                    PlacementController pc = Object.FindFirstObjectByType<PlacementController>();
                    if (pc != null)
                    {
                        pc.AttemptPlacement(hit.collider.gameObject);
                    }
                    else 
                    {
                        Debug.LogWarning("PlacementController not found in scene!");
                    }
                }
                else
                {
                    Debug.Log("Dropped on: " + (hit.collider != null ? hit.collider.name : "nothing"));
                }
            }
            else
            {
                Debug.Log("Raycast hit nothing. eventData.position: " + eventData.position);
            }
        }
    }

    private void UpdateDragPosition(PointerEventData eventData)
    {
        Vector2 localPointerPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.GetComponent<RectTransform>(), 
            eventData.position, 
            eventData.pressEventCamera, 
            out localPointerPosition);
        _dragIcon.localPosition = localPointerPosition;
    }
}
