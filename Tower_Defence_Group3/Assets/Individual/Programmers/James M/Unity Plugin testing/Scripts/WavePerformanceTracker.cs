using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach this to the same GameObject as AiSpawner.
/// Hooks into PlayerHealth, Currency, and AiSpawner each wave
/// and broadcasts results to the editor window via static events.
/// </summary>
[RequireComponent(typeof(AiSpawner))]
public class WavePerformanceTracker : MonoBehaviour
{
    // Static bridge to the editor window
    public static System.Action<WaveResult> OnWaveCompleted;
    public static System.Action<LiveWaveData> OnLiveUpdate;

    [Header("Scene References")]
    [Tooltip("The PlayerHealth component in the scene.")]
    public PlayerHealth playerHealth;

    // Internal state
    private AiSpawner spawner;
    private int trackedWave = -1;
    private float waveStartTime = 0f;
    private float healthAtWaveStart = 100f;
    private int goldAtWaveStart = 0;
    private int leakedThisWave = 0;
    private bool wasWaveActive = false;
    private int goldEarnedThisWave = 0;  // tracks kill gold only, ignoring spending

    private static WavePerformanceTracker instance;

    //  Public result types 

    [System.Serializable]
    public class WaveResult
    {
        public int waveIndex;
        public float timeToClear;
        public float livesLost;
        public int enemiesLeaked;
        public int goldEarned;
        public int goldAtEnd;
        public string rating;
    }

    [System.Serializable]
    public class LiveWaveData
    {
        public int waveIndex;
        public float elapsedTime;
        public float currentHealth;
        public int currentGold;
        public int enemiesAlive;
        public int enemiesLeaked;
        public bool waveActive;
    }

    //  Unity lifecycle 

    private void Awake()
    {
        instance = this;
        spawner = GetComponent<AiSpawner>();

        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        bool waveActive = IsWaveActive();
        int wave = spawner.currentWave;

        // Detect wave start: spawner just became active
        if (waveActive && !wasWaveActive)
            BeginTrackingWave(wave);

        // Always broadcast live data every frame so panel is never stale
        OnLiveUpdate?.Invoke(new LiveWaveData
        {
            waveIndex = trackedWave >= 0 ? trackedWave : wave,
            elapsedTime = trackedWave >= 0 ? Time.time - waveStartTime : 0f,
            currentHealth = GetCurrentHealth(),
            currentGold = Currency.main != null ? Currency.main.currency : 0,
            enemiesAlive = GameObject.FindGameObjectsWithTag("AI").Length,
            enemiesLeaked = leakedThisWave,
            waveActive = waveActive
        });

        // Detect wave end: was active, now inactive, no enemies left
        if (wasWaveActive && !waveActive &&
            GameObject.FindGameObjectsWithTag("AI").Length == 0 &&
            trackedWave >= 0)
        {
            EndTrackingWave();
        }

        wasWaveActive = waveActive;
    }

    //  Called by AIController when an enemy reaches the end 

    /// <summary>
    /// Call this from your AIController when an enemy reaches the goal.
    /// Example: WavePerformanceTracker.TrackEnemyLeaked();
    /// </summary>
    public static void TrackEnemyLeaked()
    {
        if (instance != null)
            instance.leakedThisWave++;
    }

    /// <summary>
    /// Call this from Currency.IncreaseCurrency so we track kill gold separately from spending.
    /// Example: WavePerformanceTracker.TrackGoldEarned(amount);
    /// </summary>
    public static void TrackGoldEarned(int amount)
    {
        if (instance != null)
            instance.goldEarnedThisWave += amount;
    }

    //  Internal helpers 

    private void BeginTrackingWave(int wave)
    {
        trackedWave = wave;
        waveStartTime = Time.time;
        healthAtWaveStart = GetCurrentHealth();
        goldAtWaveStart = Currency.main != null ? Currency.main.currency : 0;
        leakedThisWave = 0;
        goldEarnedThisWave = 0;

        Debug.Log($"[WaveTracker] Started tracking wave {wave + 1}");
    }

    private void EndTrackingWave()
    {
        float timeToClear = Time.time - waveStartTime;
        float currentHealth = GetCurrentHealth();
        float livesLost = Mathf.Max(0f, healthAtWaveStart - currentHealth);
        int goldNow = Currency.main != null ? Currency.main.currency : 0;
        int goldEarned = goldEarnedThisWave;  // kill gold only, unaffected by spending

        var result = new WaveResult
        {
            waveIndex = trackedWave,
            timeToClear = timeToClear,
            livesLost = livesLost,
            enemiesLeaked = leakedThisWave,
            goldEarned = goldEarned,
            goldAtEnd = goldNow,
            rating = CalculateRating(livesLost, leakedThisWave, timeToClear)
        };

        Debug.Log($"[WaveTracker] Wave {trackedWave + 1} complete | " +
                  $"Time: {timeToClear:0.0}s | Lives lost: {livesLost:0}% | " +
                  $"Leaked: {leakedThisWave} | Gold earned: {goldEarned} | Rating: {result.rating}");

        OnWaveCompleted?.Invoke(result);
        trackedWave = -1;
    }

    private bool IsWaveActive()
    {
        // A wave is "active" if the spawner is still spawning OR enemies are still alive.
        // This prevents false wave-end detection when spawning finishes but enemies remain.
        bool spawning = false;
        var field = typeof(AiSpawner).GetField("spawning",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            spawning = (bool)field.GetValue(spawner);

        int enemiesAlive = GameObject.FindGameObjectsWithTag("AI").Length;
        return spawning || enemiesAlive > 0;
    }

    private float GetCurrentHealth()
    {
        if (playerHealth == null) return 0f;
        return playerHealth.CurrentHealthPercent;
    }

    private string CalculateRating(float livesLost, int leaked, float time)
    {
        int penalty = 0;
        if (livesLost >= 40f) penalty += 3;
        else if (livesLost >= 20f) penalty += 2;
        else if (livesLost > 0f) penalty += 1;

        if (leaked >= 5) penalty += 3;
        else if (leaked >= 2) penalty += 2;
        else if (leaked >= 1) penalty += 1;

        if (penalty == 0) return "Easy";
        if (penalty <= 2) return "Fair";
        if (penalty <= 4) return "Hard";
        return "Brutal";
    }
}