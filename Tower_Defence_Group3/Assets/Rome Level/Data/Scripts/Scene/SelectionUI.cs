using TMPro;
using UnityEngine;

public class SelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject pannel;
    [SerializeField] private GameObject towerInfo;
    [SerializeField] private GameObject towerSelectorUI;
    [SerializeField] private TextMeshProUGUI currentLvl;
    [SerializeField] private PlacementSystem placementSystem;
    string[] levelNames = { "BASE", "SILVER", "GOLD" };

    void Start()
    {
        placementSystem.OnTowerSelected += ShowPannel;
        placementSystem.OnTowerDeselected += HidePannel;

        // Hide the pannel at the start
        pannel.SetActive(false);
        towerInfo.SetActive(false);
    }

    private void ShowPannel(PlacementData data)
    {
        TowerInfoUI.Instance.Hide();
        currentLvl.text = $"LEVEL: {levelNames[placementSystem.SelectedTowerData.UpgradeLevel]}";
        currentLvl.gameObject.SetActive(true);
        pannel.SetActive(true);
        towerInfo.SetActive(true);
        towerSelectorUI.SetActive(false);
    }

    private void HidePannel()
    {
        currentLvl.gameObject.SetActive(false);
        pannel.SetActive(false);
        towerInfo.SetActive(false);
        towerSelectorUI.SetActive(true);
    }

    public void OnUpgradePressed()
    {
        if (placementSystem.SelectedTowerData == null) return;

        // Look up upgrade cost from TowerData using the tower's ID
        ObjectData towerData = placementSystem.database.objectsData
            .Find(d => d.ID == placementSystem.SelectedTowerData.ID);

        if (towerData == null) return;

        int upgradeCost = towerData.Upgrade;

        if (!Currency.main.SpendCurrency(upgradeCost))
        {
            Debug.Log("Not enough currency to upgrade!");
            return; // UI does NOT refresh — nothing changed
        }

        bool upgraded = placementSystem.SelectedTowerData.UpgradeTier();

        if (upgraded)
            ShowPannel(placementSystem.SelectedTowerData); // Only refresh on success
    }

    public void OnDeletePressed()
    {
        placementSystem.DeleteTower();
    }

    public void OnClosePressed()
    {
        HidePannel();
    }
}
