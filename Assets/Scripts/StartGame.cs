using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SimpleMenu : MonoBehaviour
{
    public void PlayGame()
    {
        // Bezpoœrednie ³adowanie sceny
        SceneManager.LoadScene("GRAMAIN");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}