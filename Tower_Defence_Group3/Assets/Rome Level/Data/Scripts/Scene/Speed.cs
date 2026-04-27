using UnityEngine;
using UnityEngine.UI;

public class Speed : MonoBehaviour
{
    private bool isDoubleSpeed = false;

    public Image buttonImage;
    public Sprite normalSpeedSprite;
    public Sprite doubleSpeedSprite;

    public void ToggleSpeed()
    {
        isDoubleSpeed = !isDoubleSpeed;
        Time.timeScale = isDoubleSpeed ? 2f : 1f;

        // Change the image
        buttonImage.sprite = isDoubleSpeed ? doubleSpeedSprite : normalSpeedSprite;
    }

    void OnDestroy()
    {
        // Reset speed when object is destroyed
        Time.timeScale = 1f;
    }
}