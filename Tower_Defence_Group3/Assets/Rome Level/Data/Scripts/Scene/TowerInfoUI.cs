using TMPro;
using UnityEngine;

public class TowerInfoUI : MonoBehaviour
{
    public static TowerInfoUI Instance;

    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI abilityText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI upgradeCostText;
    [SerializeField] private TextMeshProUGUI removeCostText;
    [SerializeField] private float HeightFromCursor;

    private RectTransform rectTransform;
    

    void Awake()
    {
        Instance = this;
        rectTransform = GetComponent<RectTransform>();
        gameObject.SetActive(false);
    }

    void Update()
    {
        // Follows the cursor
        rectTransform.position = Input.mousePosition + new Vector3(0f, HeightFromCursor);
    }

    public void Show(ObjectData data)
    {
        gameObject.SetActive(true);
        nameText.text = data.Name;
        costText.text = $"Cost: ${data.Cost}";
        abilityText.text = $"Ability: {AbilityToString(data.Ability)}";
        upgradeCostText.text = $"Upgrade: ${data.Upgrade}";
        removeCostText.text = $"Refund: +${data.Cost / 2}";
        damageText.text = $"Damage: {data.Damage}";
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private string AbilityToString(Abilities ability)
    {
        return ability switch
        {
            Abilities.multishotAbility => "Multishot",
            Abilities.slowmovementAbility => "Slow Enemies",
            Abilities.poisonAbility => "Poison",
            Abilities.splashdamageAbility => "Splash Damage",
            _ => "None"
        };
    }
}
