using UnityEngine;
using UnityEngine.EventSystems;

public class MapObject : MonoBehaviour, IPointerClickHandler
{
    public string objectName;

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log(objectName + " selected");
        if (objectName == "Thang Long")
        {
            GameManager.levelToLoadName = "Level3";
            UnityEngine.SceneManagement.SceneManager.LoadScene("2D5_Scene");
        }
    }
}
