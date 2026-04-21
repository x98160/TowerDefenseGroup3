using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemiesUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI EnemiesRemainingText;
    [SerializeField] AiSpawner aiSpawner;
    
    private void Update()
    {
        UpdateEnemiesUI();
    }
    void UpdateEnemiesUI()
    {
        EnemiesRemainingText.text = aiSpawner.enemiesInWave.ToString();
    }
}
