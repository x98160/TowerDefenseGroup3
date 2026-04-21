using UnityEngine;
using TMPro;

// Implementation
// Currency.main.IncreaseCurrency(amount);
// [SerializeField] private int currencyWorth = 50;
public class Currency : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;
    public static Currency main;
    public int amount = 0;
    public int currency = 100;

    private void Awake()
    {
        main = this;
    }

    private void Start()
    {
        UpdateUI();
    }

    // Call this inside AIController Death - also notifies the wave tracker
    public void IncreaseCurrency(int amount)
    {
        currency += amount;
        UpdateUI();

        // Notify tracker so gold earned = kill gold only, not affected by spending
        WavePerformanceTracker.TrackGoldEarned(amount);
    }

    public bool SpendCurrency(int amount)
    {
        if (amount <= currency)
        {
            currency -= amount;
            UpdateUI();
            return true;
        }
        else
        {
            Debug.Log("You dont have enough currency");
            return false;
        }
    }

    void UpdateUI()
    {
        moneyText.text = currency.ToString();
    }
}