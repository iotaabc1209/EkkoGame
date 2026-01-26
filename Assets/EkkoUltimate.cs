using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EkkoUltimate : MonoBehaviour
{

    enum UltState
    {
        Normal,         // 通常プレイ
        RewindPrepare,  // 0.1秒停止
        RewindMove      // 0.1秒かけて補間移動
    }

    [Header("Ekko Ultimate")]
    public float rewindTime = 3f;

    [Header("Rewind Velocity Options")]
    [SerializeField]
    bool keepHorizontalInertia = true;

    [SerializeField]
    bool keepVerticalInertia = false;

    [SerializeField] float rewindUpwardMultiplier = 1f; // 1.0=維持, 1.1〜1.3=増幅

    [Header("Pseudo Double Jump Buffer")]
    [SerializeField] float pseudoJumpBufferTime = 0.12f; // 0.08〜0.18推奨
    bool pseudoJumpBufferActive = false;
    float pseudoJumpBufferEndTime = -999f;
    bool pseudoJumpHeld = false;
    // ウルト発動時に「慣性付与が許可される状態だったか」
    bool pseudoEligibleGround = false;
    bool pseudoEligibleWall = false;
    bool pseudoJumpPressed = false; // ★バッファ中にJump入力があったか

    [Header("Invincibility")]
    [SerializeField] private float postRewindInvincible = 0.1f;
    private Coroutine _invulnRoutine;






    [SerializeField]
    GhostFading ghostFading;

    [Header("Ghost")]
    [SerializeField]
    GhostController ghostController;


    UltState ultState = UltState.Normal;
    

    float stateTimer;              // フェーズ経過時間
    Vector2 rewindStartPosition;   // 巻き戻し開始地点
    Vector2 rewindTargetPosition;  // 巻き戻し先（ゴースト位置）
    Vector2 storedVelocity;   // ウルト使用時の慣性

    Rigidbody2D rb;
    PlayerController playerController;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
	playerController = GetComponent<PlayerController>();
    }

    void Start()
    {
    }
    
    void Update()
    {
	// ===== 追加：疑似二段ジャンプ入力バッファ（慣性に反映） =====
	if (pseudoJumpBufferActive)
	{
    		if (Input.GetButtonDown("Jump"))
		{
    			pseudoJumpHeld = true;
    			pseudoJumpPressed = true;
		}
    		if (Input.GetButtonUp("Jump"))
		{
			pseudoJumpHeld = false;
		}

    		if (Time.time >= pseudoJumpBufferEndTime)
    		{
        		// ★Jump入力が一度も無ければ、何も付けず終了
        		if (!pseudoJumpPressed)
        		{
            			pseudoJumpBufferActive = false;
            			return;
        		}

        		float addVy = 0f;

        		if (pseudoEligibleGround)
        		{
            			addVy = pseudoJumpHeld
                		? playerController.GetBigJumpV0()
                		: playerController.GetSmallJumpV0();
        		}
        		else if (pseudoEligibleWall)
        		{
            			// ★壁キック窓でも「Jump入力があった」時だけ来るのでOK
            			addVy = playerController.GetWallKickVY();
        		}

        		if (addVy > 0f)
        		{
            			addVy *= rewindUpwardMultiplier;
            			storedVelocity = new Vector2(storedVelocity.x, Mathf.Max(storedVelocity.y, addVy));
        		}

        	pseudoJumpBufferActive = false;
    		}
	}




        if (Input.GetKeyDown(KeyCode.E))
        {
            Activate();
        }
	
	switch (ultState)
	{
		case UltState.Normal:
			break;
		case UltState.RewindPrepare:
			UpdateRewindPrepare();
			break;
		case UltState.RewindMove:
			UpdateRewindMove();
			break;
	}
    }

    public void Activate()
    {
	if (ultState != UltState.Normal)
		return;

        rewindStartPosition  = rb.position;
        rewindTargetPosition = playerController.GetRewindPosition();

	
	// ★ ゴーストをこの位置でフェード開始
    	if (ghostFading != null)
    	{
	    ghostController.ResetFollowDetection();
    	    ghostFading.StartFade();
    	}

	// ★ 慣性を保存
	Vector2 v = rb.linearVelocity;

	float vy = 0f;
	if (keepVerticalInertia)
	{
    	// 落下は残さない（0が下限）
    	vy = Mathf.Max(v.y, 0f);

    	// 上昇は残す（必要なら増幅）
    	if (vy > 0f)
        	vy *= rewindUpwardMultiplier;
	}

	storedVelocity = new Vector2(
    	keepHorizontalInertia ? v.x : 0f,
    	vy
	);
	
	// ===== 追加：疑似二段ジャンプ入力バッファ開始 =====
	pseudoEligibleGround = playerController.IsGroundedNow();
	pseudoEligibleWall   = playerController.IsWallKickWindowNow();

	pseudoJumpBufferActive = (pseudoEligibleGround || pseudoEligibleWall);
	pseudoJumpBufferEndTime = Time.time + pseudoJumpBufferTime;

	pseudoJumpHeld = false;
	pseudoJumpPressed = false; 


	//プレイヤーロック
	playerController.SetControlLock(true);	

	// ★巻き戻し開始：無敵ON（Hazardだけ無視する）
	playerController.SetInvincible(true);

	// 既に無敵解除待ちが走ってたら止める
	if (_invulnRoutine != null)
	{
    		StopCoroutine(_invulnRoutine);
    		_invulnRoutine = null;
	}


        // 履歴ロック
        playerController.LockRewind(2f);

    	// フェーズ遷移
    	ultState = UltState.RewindPrepare;
    	stateTimer = 0f;

    	// 演出用：一旦停止
    	rb.linearVelocity = Vector2.zero;

    	Debug.Log($"Ekko Ultimate Activated → {rewindTargetPosition}");
    }

    void UpdateRewindPrepare()
    {
        stateTimer += Time.deltaTime;

        if (stateTimer >= 0.2f)
        {
            stateTimer = 0f;
            ultState = UltState.RewindMove;
        }
    }

    void UpdateRewindMove()
    {
        stateTimer += Time.deltaTime;

        float t = stateTimer / 0.1f;
        t = Mathf.Clamp01(t);

        rb.position = Vector2.Lerp(
            rewindStartPosition,
            rewindTargetPosition,
            t
        );

        if (t >= 1f)
        {
	    // ★ 補間完了時に慣性を戻す
            playerController.InjectVelocity(storedVelocity);
	    // ★ 操作ロック解除
	    playerController.SetControlLock(false);
	    // ★ 通常状態へ
            ultState = UltState.Normal;
	    // ★巻き戻し後0.1秒だけ無敵を残して解除
	    _invulnRoutine = StartCoroutine(PostRewindInvuln());

        }
    }

	private IEnumerator PostRewindInvuln()
	{
    		yield return new WaitForSeconds(postRewindInvincible);
    		playerController.SetInvincible(false);
    		_invulnRoutine = null;
	}




}
