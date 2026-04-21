using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance;
    [SerializeField] private DamagePopup popupPrefab;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowDamage(float amount, Vector3 worldPosition, Color? colour = null)
    {
        Vector3 spawnPos = worldPosition + new Vector3(
            Random.Range(-0.3f, 0.3f), 0.5f, 0
        );
        DamagePopup popup = Instantiate(popupPrefab, spawnPos, Quaternion.identity);
        popup.Setup(amount, colour ?? Color.red);
    }
}
