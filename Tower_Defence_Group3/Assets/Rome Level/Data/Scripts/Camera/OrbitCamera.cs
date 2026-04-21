using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class OrbitCamera : MonoBehaviour
{
    public Transform centerOfGrid;
    public float distance = 50f;
    public float xSpeed = 120f;
    public float ySpeed = 120f;
    public float yMinLimit = 10f;
    public float yMaxLimit = 80f;
    public float zoomSpeed = 5f;
    public float minDistance = 5f;
    public float maxDistance = 25f;

    private float x = 0f;
    private float y = 0f;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
    }

    void LateUpdate()
    {
        if (centerOfGrid && OrbitCameraIsActive())
        {
            if (Input.GetMouseButton(2)) // middle click to rotate
            {
                x += Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
                y -= Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;
                y = Mathf.Clamp(y, yMinLimit, yMaxLimit);
            }

            // zoom
            distance -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);

            Quaternion rotation = Quaternion.Euler(y, x, 0);
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 position = rotation * negDistance + centerOfGrid.position;

            transform.rotation = rotation;
            transform.position = position;
        }
    }

    private bool OrbitCameraIsActive()
    {
        return gameObject.activeInHierarchy && GetComponent<Camera>().enabled;
    }
}
