using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private Grid grid;

    public UpgradeSystem upgradeSystem;
    [SerializeField]
    public Camera sceneCamera;

    private Vector3 lastPosition;

    [SerializeField]
    private LayerMask placementLayermask;

    [SerializeField]
    private LayerMask uiLayer;

    public event Action OnClicked, OnRightClicked, OnExit;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {

            PointerEventData eventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            List<RaycastResult> hits = new();
            EventSystem.current.RaycastAll(eventData, hits);
            if (hits.Exists(h => h.gameObject.GetComponent<UnityEngine.UI.Button>() != null))
                return;
            OnClicked?.Invoke();
        }
        if (Input.GetMouseButtonDown(1))
            OnRightClicked?.Invoke();
        if (Input.GetKeyDown(KeyCode.Escape))
            OnExit?.Invoke();

    }

    /*unable to get working properly
    public bool IsPointerOverUI() 
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> hits = new();
        EventSystem.current.RaycastAll(eventData, hits);

        return hits.Count > 0;
    }*/

    public Vector3 GetSelectedMapPosition()
    {
        Camera cam = CameraController.ActiveCamera;
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = cam.nearClipPlane;
        Ray ray = cam.ScreenPointToRay(mousePos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100, placementLayermask))
            lastPosition = hit.point;
        return lastPosition;
    }
}