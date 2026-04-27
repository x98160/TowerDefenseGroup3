using UnityEngine;

public class TowerSelectMenu : MonoBehaviour
{
    // Tower preview rederers for each tower
    public GameObject[] towerPreviews;

    private bool previewsActive = false;

    public void ShowPreviews()
    {
        previewsActive = !previewsActive;

        foreach (GameObject gameObject in towerPreviews)
        {
            gameObject.SetActive(previewsActive);
        }
    }
}
