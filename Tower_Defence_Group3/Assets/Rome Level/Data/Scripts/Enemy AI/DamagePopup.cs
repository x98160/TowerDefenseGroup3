using UnityEngine;
using TMPro;
using UnityEditor.ShaderGraph.Internal;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro text;

    private float lifetime = 1f;
    private float floatSpeed = 1f;
    private float timer;

    public void Setup(float amount, Color colour)
    {
        text.SetText(Mathf.RoundToInt(amount).ToString());
        text.color = colour;
    }

    private void LateUpdate()
    {
        if (CameraController.ActiveCamera != null)
        {
            transform.LookAt(transform.position + CameraController.ActiveCamera.transform.forward);
        }

        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        timer += Time.deltaTime;
        text.alpha = Mathf.Lerp(1f, 0f, timer / lifetime);

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
