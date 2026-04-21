using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TowerData towerData;
    [SerializeField] private int dataIndex = 0; // which entry in objectsData this button is for
    private ObjectData objectData;

    void Start()
    {
        if (towerData != null && towerData.objectsData.Count > dataIndex)
            objectData = towerData.objectsData[dataIndex];
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (objectData != null)
            TowerInfoUI.Instance.Show(objectData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TowerInfoUI.Instance.Hide();
    }
}