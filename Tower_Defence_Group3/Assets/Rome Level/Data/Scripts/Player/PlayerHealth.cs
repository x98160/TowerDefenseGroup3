using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public Image[] hearts;
    public int maxHearts = 5;

    private float currentHealth;
    private float healthPerHeart;

    /// <summary>
    /// Returns current health as a 0-100 percentage.
    /// Read by WavePerformanceTracker without reflection.
    /// </summary>
    public float CurrentHealthPercent => currentHealth;

    void Start()
    {
        currentHealth = 100f;
        healthPerHeart = 100f / maxHearts;
        UpdateHeartUI();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, 100f);
        UpdateHeartUI();

        if (currentHealth <= 0)
        {
            SceneManager.LoadScene("LoseScene");
        }
    }

    void UpdateHeartUI()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            float heartHealth = currentHealth - (i * healthPerHeart);
            hearts[i].fillAmount = Mathf.Clamp01(heartHealth / healthPerHeart);
        }
    }
}