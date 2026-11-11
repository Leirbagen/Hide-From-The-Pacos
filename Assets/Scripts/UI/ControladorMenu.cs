using UnityEngine;
using UnityEngine.SceneManagement; 

public class ControladorMenu : MonoBehaviour
{
    public void ReiniciarNivel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}