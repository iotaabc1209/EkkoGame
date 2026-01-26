using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleUI : MonoBehaviour
{
    public void OnStartButton()
    {
        SceneManager.LoadScene("Stage01");
    }
}
