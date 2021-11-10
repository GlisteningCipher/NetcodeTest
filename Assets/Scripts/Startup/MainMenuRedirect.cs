using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuRedirect : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
