using UnityEngine;

public class GhostController : MonoBehaviour
{
    [Header("参照")]
    public PlayerController playerController;
    public GhostFading ghostFading;   // ★ 追加

    Vector2 lastPosition;
    bool alphaRestored;

    void Start()
    {
        lastPosition = transform.position;
        alphaRestored = false;
    }

    void Update()
    {
        if (playerController == null)
            return;

        Vector2 newPosition = playerController.GetRewindPosition();
        transform.position = newPosition;
	
	// ★ 毎フレーム確認用ログ
        //Debug.Log($"Ghost pos: {newPosition}, last: {lastPosition}");

        // ★ 履歴がたまって追尾し始めた瞬間
        if (!alphaRestored && Vector2.Distance(newPosition, lastPosition) > 0.001f)
        {
	    //Debug.Log("追尾開始を検知！");
            if (ghostFading != null)
            {
                ghostFading.RestoreAlphaAndStopFade();
            }
            alphaRestored = true;
        }

        lastPosition = newPosition;
    }

    public void ResetFollowDetection()
    {
    	alphaRestored = false;
    }

}
