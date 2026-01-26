using UnityEngine;
using UnityEngine.SceneManagement;

public class ClearUI : MonoBehaviour
{
    public void OnReturnButton()
    {
        SceneManager.LoadScene("Title");
    }
}
