using UnityEngine;

public class TankScan : MonoBehaviour
{
    [SerializeField] private float scanAngle = 50f;
    [SerializeField] private float scanSpeed = 1f;

    private float startYRotation;
    private float randomOffset;

    void Start()
    {
        startYRotation = transform.localEulerAngles.y;
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float angle = Mathf.Sin((Time.time + randomOffset) * scanSpeed) * scanAngle;
        transform.localRotation = Quaternion.Euler(0, startYRotation + angle, 0);
    }
}
