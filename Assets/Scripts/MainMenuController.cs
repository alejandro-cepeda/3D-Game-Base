using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenuController : MonoBehaviour
{
   // Ensure your game scene is added to Build Settings
    [SerializeField] private string mainGameScene = "MainGameplay";

    public void PlayGame()
    {
        // Transitions from the menu to the zombie survival action
        SceneManager.LoadScene(mainGameScene);
    }

    public void OpenOptions()
    {
        Debug.Log("Options Menu Opened");
    }

    public void QuitGame()
    {
        // Standard exit protocol
        Application.Quit();
        Debug.Log("Exiting Dead Last...");
    }
}
