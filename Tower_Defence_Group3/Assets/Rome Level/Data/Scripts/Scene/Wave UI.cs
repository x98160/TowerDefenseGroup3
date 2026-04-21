using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class WaveUI : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] AiSpawner aiSpawner;

    private void FixedUpdate()
    {
        UpdateWaveUI();
    }
    void UpdateWaveUI()
    {
        waveText.text = aiSpawner.currentWave.ToString();
    }
}
