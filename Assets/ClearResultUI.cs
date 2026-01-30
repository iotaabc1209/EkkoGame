using UnityEngine;
using TMPro;

public class ClearResultUI : MonoBehaviour
{
    [SerializeField] private TMP_Text deathText;
    [SerializeField] private TMP_Text timeText;

    // Player側で保存しているキーと同じ文字列にする
    private const string KEY_DEATH = "CLEAR_DEATH";
    private const string KEY_TIME  = "CLEAR_TIME";

    private void Start()
    {
        int deaths = PlayerPrefs.GetInt(KEY_DEATH, 0);
        float time = PlayerPrefs.GetFloat(KEY_TIME, 0f);

        if (deathText != null)
            deathText.text = $"DEATH: {deaths}";

        int totalSeconds = Mathf.FloorToInt(time);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        if (timeText != null)
            timeText.text = $"TIME: {minutes:00}:{seconds:00}";
    }
}
