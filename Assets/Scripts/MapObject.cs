using UnityEngine;
using UnityEngine.EventSystems;

public class MapObject : MonoBehaviour, IPointerClickHandler
{
    public string objectName;

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log(objectName + " selected");
    }
}
