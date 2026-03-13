using UnityEngine;

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
        if (_inputActions.UI.Quit.WasPressedThisFrame())
        {
            QuitGame();
        }

        if (_inputActions.UI.Restart.WasPressedThisFrame())
        {
            RestartGame();
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
