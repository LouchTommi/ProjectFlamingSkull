using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EndScene : MonoBehaviour
{
    public void OnInteract(InputValue value)
    {
        SceneManager.LoadScene("MainMenu");
    }
}
