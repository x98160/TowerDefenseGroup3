using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Camera TopDownCamera;
    public Camera OrbitCamera;
    public static Camera ActiveCamera;
    public bool IsTopDown { get; private set; }

    private void Start()
    {
        setBuildMode(false);
    }

    public void setBuildMode(bool buildMode)
    {
        IsTopDown = buildMode;
        TopDownCamera.enabled = buildMode;
        OrbitCamera.enabled = !buildMode;
        ActiveCamera = buildMode ? TopDownCamera : OrbitCamera;
    }

    private void Update()
    {
        TopDownCamera.transform.rotation = Quaternion.Euler(90f, OrbitCamera.transform.eulerAngles.y, 0f);
    }
}