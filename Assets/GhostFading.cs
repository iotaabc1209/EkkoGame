using UnityEngine;

public class GhostFading : MonoBehaviour
{
    SpriteRenderer sr;

    [Header("Fade Settings")]
    public float fadeDuration = 2f;

    [Header("Initial Alpha")]
    [Range(0f, 1f)]
    public float initialAlpha = 0.5f;

    float timer;
    bool isFading;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // ★ 初期状態：半透明で表示
        SetAlpha(initialAlpha);
    }

    void Update()
    {
        if (!isFading)
            return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / fadeDuration);

        // ★ 0.5 → 0.0 にフェード
        SetAlpha(Mathf.Lerp(initialAlpha, 0f, t));

        if (t >= 1f)
        {
            isFading = false;
            SetAlpha(0f);
        }
    }

    // ★ EkkoUltimate から呼ばれる
    public void StartFade()
    {
	Debug.Log("GhostFading.StartFade CALLED");
        timer = 0f;
        isFading = true;

        // フェード開始時は必ず初期αに戻す
        SetAlpha(initialAlpha);
    }

    void SetAlpha(float a)
    {
        if (sr == null) return;

        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }

    public void SetAlphaToInitial()
	{
    	SetAlpha(initialAlpha);
	}

    public void RestoreAlphaAndStopFade()
	{
    	isFading = false;          // ★ フェードを止める
    	SetAlpha(initialAlpha);    // ★ 0.5 に戻す
	}

}
