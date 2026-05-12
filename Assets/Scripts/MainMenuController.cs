using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenuController : MonoBehaviour
{
   // Ensure your game scene is added to Build Settings
    [SerializeField] private string mainGameScene = "MainGameplay";

    private void Start()
    {
        // Find all buttons in the scene and select the first one automatically
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        Button firstButton = null;

        foreach (Button btn in allButtons)
        {
            if (firstButton == null) firstButton = btn;

            // Ensure the controller selection color matches the mouse hover color
            // and make it shaded dark so it's very obvious
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.65f, 0.65f, 0.65f, 1f);
            cb.selectedColor = cb.highlightedColor;
            btn.colors = cb;

            // Add haptic feedback for when this button is highlighted
            EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry selectEntry = new EventTrigger.Entry { eventID = EventTriggerType.Select };
            selectEntry.callback.AddListener((data) => TriggerMenuTapHaptics());
            trigger.triggers.Add(selectEntry);
        }

        if (firstButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
        }
    }

    private Coroutine? menuHapticsRoutine;

    private void TriggerMenuTapHaptics()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Gamepad.current == null) return;
        UnityEngine.InputSystem.Gamepad.current.SetMotorSpeeds(0.1f, 0.1f);
        if (menuHapticsRoutine != null) StopCoroutine(menuHapticsRoutine);
        menuHapticsRoutine = StartCoroutine(StopMenuHapticsRoutine(0.05f));
#endif
    }

    private System.Collections.IEnumerator StopMenuHapticsRoutine(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Gamepad.current != null)
        {
            UnityEngine.InputSystem.Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
#endif
        menuHapticsRoutine = null;
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Gamepad.current != null)
        {
            UnityEngine.InputSystem.Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
#endif
    }

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
