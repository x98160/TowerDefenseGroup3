using UnityEngine;

public class TowerBuild : MonoBehaviour
{
    [SerializeField] private float buildTime = 5f;
    [SerializeField] private GameObject buildEffectPrefab;
    [SerializeField] private GameObject Range;

    private Vector3 startPos;
    private Vector3 targetPos;
    private float elapsed = 0f;
    private bool isBuilding = false;

    private GameObject buildEffectInstance;

    private void Start()
    {
        startPos = transform.position + new Vector3(0, -3f, 0);
        targetPos = transform.position;

        transform.position = startPos;

        if (buildEffectPrefab != null)
        {
            buildEffectInstance = Instantiate(buildEffectPrefab, targetPos, Quaternion.identity);
        }

        isBuilding = true;

        if (Range != null)
            Range.SetActive(false);
    }

    private void Update()
    {
        if (!isBuilding) return;

        elapsed += Time.deltaTime;

        float t = elapsed / buildTime;
        transform.position = Vector3.Lerp(startPos, targetPos, t);

        if (t >= 1f)
        {
            transform.position = targetPos;
            isBuilding = false;

            if (buildEffectInstance != null)
            {
                Destroy(buildEffectInstance);
            }
        }
    }

    private void OnDestroy()
    {
        if (buildEffectInstance != null)
        {
            Destroy(buildEffectInstance);
        }
    }
}