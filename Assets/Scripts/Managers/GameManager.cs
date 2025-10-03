using UnityEngine;
using UnityEngine.InputSystem; // Add this

public class GameManager : MonoBehaviour
{
    private InputSystem_Actions _inputActions;

    private void OnEnable()
    {
        _inputActions = new InputSystem_Actions();
        _inputActions.Enable();
    }

    private void OnDisable()
    {
        _inputActions.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        if (_inputActions.UI.Quit.WasPressedThisFrame()) // Map this to Escape in your Input Actions
        {
            Application.Quit();
        }

        if (_inputActions.UI.Restart.WasPressedThisFrame()) // Map this to R in your Input Actions
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
