using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UpgradeSystem : MonoBehaviour
{
    [SerializeField] private TowerData towerData;
    [SerializeField] private int objectIndex;
    private ObjectData objectData;

    private GameObject currentModel;
    private int currentTier = 0;

    private void Start()
    {
        objectData = towerData.objectsData[objectIndex]; // gets the object index of the scriptable object data for this tower

        // store the base tower model
        currentModel = transform.GetChild(0).gameObject;
    }

    public void Upgrade()
    {
        if (currentTier >= objectData.UpgradePrefabs.Length)
            return;

        if (!Currency.main.SpendCurrency(objectData.Upgrade))
            return;

        // remove old model
        Destroy(currentModel);

        // spawn upgrade model as child of the tower so it stays in the same place
        currentModel = Instantiate(
            objectData.UpgradePrefabs[currentTier],
            transform
        );

        currentModel.transform.localPosition = Vector3.zero;

        currentTier++;

        Debug.Log("Upgraded to tier " + currentTier);
        // add UI feedback here to indicate upgrade success, maybe a sound effect or particle effect
        
    }

    /*
    private void OnMouseDown()
    {
        Debug.Log("The Mouse actually clicked " + gameObject.name);
        Upgrade();
    }


    /*
    public void Upgrade(Vector3Int gridPosition)
    {

        if (currentLevel >= objectData.UpgradePrefabs.Length - 1)
        {
            Debug.Log("Max Level Reached");
            return;
        }

        currentLevel++;

        GameObject newTower = Instantiate(objectData.UpgradePrefabs[currentLevel], transform.position, Quaternion.identity);

        Destroy(gameObject);
    }*/


    /*
    // Turret Level For Turret 1
    public int LevelTurret1;

    // Turret Level For Turret 2
    public int LevelTurret2;

    // Turret Level For Turret 3
    public int LevelTurret3;

    // Upgrade Panel
    public GameObject panel;

    // Turret1 Objects
    public GameObject Tower1Level1;
    public GameObject Tower1Level2;
    public GameObject Tower1Level3;

    // Turret2 Objects
    public GameObject Tower2Level1;
    public GameObject Tower2Level2;
    public GameObject Tower2Level3;

    // Turret3 Objects
    public GameObject Tower3Level1;
    public GameObject Tower3Level2;
    public GameObject Tower3Level3;

    void Start()
    {
        LevelTurret1 = 1;
        LevelTurret2 = 1;
        LevelTurret3 = 1;
    }

    public void BackButton()
    {
        panel.SetActive (false);
    }

    public void Upgrade1()
    {
        if (LevelTurret1 < 3)
        {
            LevelTurret1 += 1;

            SetLevelTurret1 (LevelTurret1);
        }
    }
    public void Upgrade2()
    {
        if (LevelTurret2 < 3)
        {
            LevelTurret2 += 1;

            SetLevelTurret2 (LevelTurret2);
        }
    }
    public void Upgrade3()
    {
        if (LevelTurret3 < 3)
        {
            LevelTurret3 += 1;

            SetLevelTurret3 (LevelTurret3);
        }
    }

    public void SetLevelTurret1(int lvl_1)
    {
        if (lvl_1 == 1)
        {
            Tower1Level1.SetActive (true);
            Tower1Level2.SetActive (false);
            Tower1Level3.SetActive (false);
        }
        if (lvl_1 == 2)
        {
            Tower1Level1.SetActive(false);
            Tower1Level2.SetActive(true);
            Tower1Level3.SetActive(false);
        }
        if (lvl_1 == 3)
        {
            Tower1Level1.SetActive(false);
            Tower1Level2.SetActive(false);
            Tower1Level3.SetActive(true);
        }
    }
    public void SetLevelTurret2(int lvl_2)
    {
        if (lvl_2 == 1)
        {
            Tower2Level1.SetActive(true);
            Tower2Level2.SetActive(false);
            Tower2Level3.SetActive(false);
        }
        if (lvl_2 == 2)
        {
            Tower2Level1.SetActive(false);
            Tower2Level2.SetActive(true);
            Tower2Level3.SetActive(false);
        }
        if (lvl_2 == 3)
        {
            Tower2Level1.SetActive(false);
            Tower2Level2.SetActive(false);
            Tower2Level3.SetActive(true);
        }
    }
    public void SetLevelTurret3(int lvl_3)
    {
        if (lvl_3 == 1)
        {
            Tower3Level1.SetActive(true);
            Tower3Level2.SetActive(false);
            Tower3Level3.SetActive(false);
        }
        if (lvl_3 == 2)
        {
            Tower3Level1.SetActive(false);
            Tower3Level2.SetActive(true);
            Tower3Level3.SetActive(false);
        }
        if (lvl_3 == 3)
        {
            Tower3Level1.SetActive(false);
            Tower3Level2.SetActive(false);
            Tower3Level3.SetActive(true);
        }
    }
    */
}
