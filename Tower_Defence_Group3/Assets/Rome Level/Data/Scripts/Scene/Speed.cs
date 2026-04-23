using UnityEngine;

public class Speed : MonoBehaviour
{
    private bool isDoubleSpeed = false;

    public void ToggleSpeed()
    {
        isDoubleSpeed = !isDoubleSpeed;
        Time.timeScale = isDoubleSpeed ? 2f : 1f;
    }

    void OnDestroy()
    {
        // Reset speed when object is destroyed
        Time.timeScale = 1f;
    }
}