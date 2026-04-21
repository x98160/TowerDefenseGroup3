using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using TMPro;
using NUnit.Framework.Internal;

public class AiSpawner : MonoBehaviour
{
    [SerializeField] private GameObject startText;
    public Transform spawnPoint;
    public Waves[] wave;
    public int currentGroup;
    public int currentWave;
    private int spawned;
    private float timer;
    private bool spawning = false;
    public Button button;
    public int enemiesInWave;

    void Start()
    {
        Button waveButton = button.GetComponent<Button>();
        waveButton.onClick.AddListener(WaveIncrease);
        startText.GetComponent<Animation>().Play();
    }

    void Update()
    {
        enemiesInWave = GameObject.FindGameObjectsWithTag("AI").Length;
        if (!spawning)
        {
            if (currentWave > 0 && GameObject.FindGameObjectsWithTag("AI").Length == 0)
            {
                if (!startText.GetComponent<Animation>().isPlaying)
                {
                    startText.GetComponent<Animation>().Play();
                }
            }
            return;
        }

        if (currentWave >= wave.Length)
        {
            return;
        }

        timer += Time.deltaTime;

        AIGroup group = wave[currentWave].aiGroups[currentGroup];

        if (timer > group.spawnRate && spawned < group.aiCount)
        {
            timer = 0f;
            Instantiate(group.aiPrefab, spawnPoint.position, Quaternion.identity);
            spawned++;
        }

        if (spawned >= group.aiCount)
        {
            spawned = 0;
            currentGroup++;
            timer = 0f;

            if (currentGroup >= wave[currentWave].aiGroups.Length)
            {
                spawning = false;
                currentWave++;
                Debug.Log("Next Wave is coming");
            }
        }
    }

    public void Spawn()
    {
        if (!spawning)
        {
            if (GameObject.FindGameObjectsWithTag("AI").Length > 0)
                return;
            spawning = true;
            spawned = 0;
            timer = 0f;
            startText.GetComponent<Animation>().enabled = true;
        }
    }

    public void WaveIncrease()
    {

        if (enemiesInWave == 0 && !spawning)
        {
            startText.GetComponent<Animation>()["Flashing start wave"].wrapMode = WrapMode.Once;
            currentGroup = 0;
            spawning = true;
            timer = 0f;

            // Spawn the first enemy immediately, then let Update handle the rest
            AIGroup group = wave[currentWave].aiGroups[currentGroup];
            Instantiate(group.aiPrefab, spawnPoint.position, Quaternion.identity);
            spawned = 1;
        }
        else if (currentWave == 10)
        {
            SceneManager.LoadScene("Dino Level");
        }
        else
        {
            return;
        }
    }
}