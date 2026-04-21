using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelect : MonoBehaviour
{
    public void LoadScene_1()
    {
        SceneManager.LoadScene("Main_Gameplay");
    }
    /*
    public void LoadScene_2()
    {
        SceneManager.LoadScene("Level_2");
    }
    public void LoadScene_3()
    {
        SceneManager.LoadScene("Level_3");
    }
    */
}
