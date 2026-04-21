using UnityEngine;

public class HealthBar : MonoBehaviour
{
    public Transform healthBarTransform;

    private void LateUpdate()
    {
        if (healthBarTransform == null) return;
        if (CameraController.ActiveCamera == null) return;

        healthBarTransform.LookAt(healthBarTransform.position + CameraController.ActiveCamera.transform.forward);
    }
}
