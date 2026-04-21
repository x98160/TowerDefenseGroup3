using System.Collections.Generic;
using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    public UpgradeSystem upgradeSystem;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private GameObject mouseIndicator, cellIndicator;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private Grid grid;
    [SerializeField] private TowerData database;
    [SerializeField] private GameObject gridVisual;
    [SerializeField] private LayerMask blockingLayers;
    [SerializeField] private LayerMask towerLayerMask;

    private int selectedObjectIndex = -1;
    private GridData towersData;
    private GameObject currentPreviewInstance;
    private Renderer[] previewRenderer;
    private List<GameObject> placedGameObjects = new();
    private Vector3Int selectedGridPos;
    private bool towerLocked = false;

    public PlacementData SelectedTowerData { get; private set; }
    public event System.Action<PlacementData> OnTowerSelected;
    public event System.Action OnTowerDeselected;

    private void Start()
    {
        selectedObjectIndex = -1;
        gridVisual.SetActive(false);
        cellIndicator.SetActive(false);
        mouseIndicator.SetActive(false);

        towersData = new();
        //previewRenderer = cellIndicator.GetComponentInChildren<Renderer>();
        inputManager.OnClicked += OnLeftClick;
        inputManager.OnExit += CancelAll;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(2)) CancelAll();

        if (Input.GetKeyDown(KeyCode.Alpha1)) StartPlacement(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) StartPlacement(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) StartPlacement(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) StartPlacement(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) StartPlacement(4);

        // Update cell indicator only in placement mode
        if (selectedObjectIndex < 0) return;

        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);

        bool placementValid = CheckPlacementIsValid(gridPosition, selectedObjectIndex);

        if (previewRenderer != null)
        {
            Color previewcolour = placementValid ? Color.green : new Color(1f, 0f, 0f, 0.5f);
            foreach (Renderer r in previewRenderer)
            {
                r.material.color = previewcolour;
            }
        }

        if (currentPreviewInstance != null)
        {
            currentPreviewInstance.transform.position = grid.CellToWorld(gridPosition);
        }

        /*previewRenderer.material.color = placementValid ? Color.white : Color.red;
        mouseIndicator.transform.position = mousePosition;
        cellIndicator.transform.position = grid.CellToWorld(gridPosition);*/
    }

    private void OnLeftClick()
    {
        // Block all clicks when in orbit view
        if (!cameraController.IsTopDown) return;

        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        Vector3Int gridPos = grid.WorldToCell(mousePosition);

        if (selectedObjectIndex >= 0)
        {
            PlacementData existing = towersData.GetPlacementDataAt(gridPos);
            if (existing != null)
            {
                CancelPlacementOnly();
                LockTower(gridPos, existing);
            }
            else
            {
                PlaceTower();
            }
            return;
        }

        PlacementData data = towersData.GetPlacementDataAt(gridPos);
        if (data != null)
            LockTower(gridPos, data);
        else
        {
            towerLocked = false;
            SelectedTowerData = null;
            OnTowerDeselected?.Invoke();
        }
    }

    private void LockTower(Vector3Int gridPos, PlacementData data)
    {
        selectedGridPos = gridPos;
        SelectedTowerData = data;
        towerLocked = true;
        OnTowerSelected?.Invoke(data);
    }

    // Called from UI button - selects tower type and enters build mode (top-down)
    public void StartPlacement(int ID)
    {
        // Close upgrade menu if open
        towerLocked = false;
        SelectedTowerData = null;
        OnTowerDeselected?.Invoke();

        selectedObjectIndex = database.objectsData.FindIndex(data => data.ID == ID);
        if (selectedObjectIndex < 0)
        {
            Debug.LogError($"No ID found {ID}");
            return;
        }

        TowerInfoUI.Instance.Hide();

        if (currentPreviewInstance != null)
        {
            Destroy(currentPreviewInstance);
        }

        currentPreviewInstance = Instantiate(database.objectsData[selectedObjectIndex].Prefab);
        DisablePreview(currentPreviewInstance);
        previewRenderer = currentPreviewInstance.GetComponentsInChildren<Renderer>();

        gridVisual.SetActive(true);
        cellIndicator.SetActive(false);
        mouseIndicator.SetActive(false);
        cameraController.setBuildMode(true); // switch to top-down
    }

    private void DisablePreview(GameObject preview)
    {
        foreach (MonoBehaviour mb in preview.GetComponentsInChildren<MonoBehaviour>())
        {
            mb.enabled = false;
        }

        foreach (Collider col in preview.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
    }

    private void PlaceTower()
    {
        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);

        bool placementValid = CheckPlacementIsValid(gridPosition, selectedObjectIndex);
        if (!placementValid) return;

        int cost = database.objectsData[selectedObjectIndex].Cost;
        if (!Currency.main.SpendCurrency(cost)) return;

        GameObject towerObject = Instantiate(database.objectsData[selectedObjectIndex].Prefab);
        towerObject.transform.position = grid.CellToWorld(gridPosition);
        placedGameObjects.Add(towerObject);

        UpgradeSystem towerUpgrade = towerObject.GetComponent<UpgradeSystem>();
        towersData.AddObjectAt(gridPosition, database.objectsData[selectedObjectIndex].Size,
            database.objectsData[selectedObjectIndex].ID, placedGameObjects.Count - 1, towerUpgrade);

        CancelPlacementOnly();
        cameraController.setBuildMode(false); // switch to orbit after placing
        Debug.Log("Tower placed at: " + gridPosition);
    }

    public void DeleteTower()
    {
        if (SelectedTowerData == null) return;

        int index = towersData.GetObjectIndexAt(selectedGridPos);
        if (index < 0) return;

        GameObject tower = placedGameObjects[index];
        if (tower != null) Destroy(tower);

        Currency.main.IncreaseCurrency(database.objectsData[SelectedTowerData.ID].Cost / 2);
        placedGameObjects[index] = null;
        towersData.RemoveObjectAt(selectedGridPos);

        towerLocked = false;
        SelectedTowerData = null;
        OnTowerDeselected?.Invoke();
        Debug.Log("Tower deleted");
    }

    // Cancels placement visuals only - does NOT close upgrade menu
    private void CancelPlacementOnly()
    {
        selectedObjectIndex = -1;
        gridVisual.SetActive(false);
        cellIndicator.SetActive(false);
        mouseIndicator.SetActive(false);

        if (currentPreviewInstance != null)
        {
            Destroy(currentPreviewInstance);
            currentPreviewInstance = null;
            previewRenderer = null;
        }
    }

    // Cancels everything - placement AND upgrade menu
    private void CancelAll()
    {
        CancelPlacementOnly();
        cameraController.setBuildMode(false);
        towerLocked = false;
        SelectedTowerData = null;
        OnTowerDeselected?.Invoke();
    }

    // Public so Close button on upgrade UI can call this
    public void CloseUpgradeMenu()
    {
        towerLocked = false;
        SelectedTowerData = null;
        OnTowerDeselected?.Invoke();
    }

    private bool CheckPlacementIsValid(Vector3Int gridPosition, int selectedObjectIndex)
    {
        GridData selectedData = towersData;

        bool gridFree = selectedData.CanPlaceObjectAt(gridPosition, database.objectsData[selectedObjectIndex].Size);
        if (!gridFree) return false;

        Vector3 worldPos = grid.CellToWorld(gridPosition);
        Vector2Int sizeCell = database.objectsData[selectedObjectIndex].Size;
        Vector3 cellSize = grid.cellSize;

        Vector3 worldSize = new Vector3(sizeCell.x * cellSize.x, cellSize.y, sizeCell.y);
        Vector3 halfExtents = worldSize * 0.5f;
        Vector3 center = worldPos + new Vector3(halfExtents.x, 0f, halfExtents.z);
        Vector3 checkExtents = halfExtents - new Vector3(0.01f, 0.5f, 0.01f);

        bool blocked = Physics.CheckBox(center, checkExtents, Quaternion.identity, blockingLayers, QueryTriggerInteraction.Ignore);
        return !blocked;
    }
}