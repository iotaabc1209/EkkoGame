using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
	enum PlayerState
	{
  	  Grounded,   // 地上
	  Air        // 空中（ジャンプ直後含む）
	}

    // ===== パラメータ =====
    public float speed = 5f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;
    public LayerMask groundLayer;

    [SerializeField] float tileSizeY = 1f;        // 1マスの高さ（Grid Cell Size Y）
    [SerializeField] float bigJumpTiles = 3f;         // 大ジャンプ高さ（マス）
    [SerializeField] float smallJumpTiles = 1f;     // 小ジャンプ高さ（マス）
    float bigJumpForce;   // 実際は「初速(v0)」
    float smallJumpForce; // 実際は「初速(v0)」

    float jumpStartTime = -999f;

    [SerializeField] float smallCutWindow = 0.15f; // 0.05〜0.12 推奨
    [SerializeField] bool useMultiplierCut = true; //ジャンプ方式を変えるならここをいじる、t>f
    [SerializeField] float jumpCutMultiplier = 0.55f; // マリオっぽい係数（0.45〜0.7）

    bool canCutToSmall = false;



    [SerializeField] float airAccel = 20f;
    [SerializeField] float airDecel = 55f;

    
    // ===== 壁キック =====
    [SerializeField] LayerMask wallLayer;

    [SerializeField] float wallKickForceX = 9f;
    [SerializeField] float wallKickForceY = 8f;

    [Header("Air control (post wall-kick)")]
    [SerializeField] private float postWallKickControlTime = 0.12f;   // 0.08〜0.18
    [SerializeField] private float postWallKickControlScale = 0.25f;  // 0.2〜0.6（小さいほど壁キック慣性が残る）
    private float postWallKickTimer = 0.12f;
    
    [Header("Wall coyote")]
    [SerializeField] private float wallCoyoteTime = 0.12f; // 0.08〜0.15
    private float lastWallContactTime = -999f;
    private int lastWallDir = 0; // -1(left) / 1(right)



    bool isTouchingWall;
    int wallDir; // -1 = 左壁, 1 = 右壁　0 = なし



    // ===== 内部状態 =====
    Rigidbody2D rb;
    BoxCollider2D col;
    EkkoUltimate ekkoUltimate;
    PlayerState state;

    float lastGroundedTime;
    float lastJumpInputTime;
    float rewindLockEndTime = 0f;
    
    Vector2 externalVelocity;
    Vector2 lastSafePosition;
    Vector2 spawnPosition;



    bool isGrounded;
    bool isControlLocked;
    bool hasExternalVelocity;
    
    public Vector2 rewindAnchorPosition;
    public bool isRewindLocked = false;


    // ===== Ekko Ghost 用履歴 =====
	struct PositionSnapshot
	{
    	public Vector2 position;
    	public float time;

    	public PositionSnapshot(Vector2 p, float t)
    		{
        		position = p;
        		time = t;
    		}
	}

Queue<PositionSnapshot> positionHistory = new Queue<PositionSnapshot>();



    void Awake()
    {
        // ===== 自分自身の参照取得 =====
        rb = GetComponent<Rigidbody2D>();
	col = GetComponent<BoxCollider2D>();
        ekkoUltimate = GetComponent<EkkoUltimate>();
	
	// ===== 初速計算結果の反映 =====
	RecalcJumpFromTiles_GravityFixed();

        // ===== 無効な初期値 =====
        lastGroundedTime = -999f;
        lastJumpInputTime = -999f;
    }

    void Start()
    {
	spawnPosition = rb.position;
        // 初期状態決定
        if (CheckGrounded())
        {
            state = PlayerState.Grounded;
            lastGroundedTime = Time.time;
        }
        else
        {
            state = PlayerState.Air;
        }
    }


    void FixedUpdate()
    {
        RecordPosition();
    }


    void Update()
    {
	if (isControlLocked)
		return;

        // ===== 入力取得 =====
        float move = Input.GetAxis("Horizontal");
	Vector2 velocity = rb.linearVelocity;

	// ===== ジャンプ入力管理 =====
    	if (Input.GetButtonDown("Jump"))
    	{
        	lastJumpInputTime = Time.time;
    	}


    　　// ★ 先に接地/壁判定を取る（wallDirもここで決まる)
　　　　isGrounded = CheckGrounded();
	isTouchingWall = CheckWall();
	//壁のコヨーテタイム
	if (isTouchingWall)
	{
    		lastWallContactTime = Time.time;
    		lastWallDir = wallDir;
	}

	
	//壁キック直後入力減衰タイマー
	if (postWallKickTimer > 0f)
	{
    	postWallKickTimer -= Time.deltaTime;
    	if (postWallKickTimer < 0f) postWallKickTimer = 0f;
	}



	// ===== 外部速度取得 =====
	if (hasExternalVelocity)
	{
		velocity = externalVelocity;
		hasExternalVelocity = false;
	}
	else
	{
		if (state == PlayerState.Air)
		{
			float targetX = move * speed;

			// 入力なし or 反対入力（減速を強める）
			bool noInput = Mathf.Abs(move) < 0.01f;
			bool reversing = !noInput && Mathf.Abs(velocity.x) > 0.01f && Mathf.Sign(targetX) != Mathf.Sign(velocity.x);

			float accel = (noInput || reversing) ? airDecel : airAccel;

			// 壁キック直後だけ空中制御を弱める（入力は生きてるけど反転しづらい）
			if (postWallKickTimer > 0f)
    			accel *= postWallKickControlScale;

			velocity.x = Mathf.MoveTowards(velocity.x, targetX, accel * Time.deltaTime);

    		}
		else
		{
    		velocity.x = move * speed;
		}
	}

        rb.linearVelocity = velocity;

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
	    lastSafePosition = rb.position; // ★ 安全地点更新
        }

        // ===== State Machine =====
        switch (state)
        {
            case PlayerState.Grounded:
                UpdateGrounded(isGrounded);
                break;

            case PlayerState.Air:
                UpdateAir(isGrounded);
                break;
        }
        //Debug.Log(state);

	// 離したら小ジャンに寄せる（大→小カット）
	// 時間tから数式で現在状態を推定して補正する
	if (canCutToSmall)
	{
    		float t = Time.time - jumpStartTime;

    		// 一定時間を過ぎたら「大確定」（何もしない）
    		if (t > smallCutWindow)
    		{
        		canCutToSmall = false;
    		}
    		else if (Input.GetButtonUp("Jump"))
    		{
        		// 係数方式（マリオっぽい可変）を使うなら従来通り
        		if (useMultiplierCut)
        		{
            			float vy = rb.linearVelocity.y;
            			if (vy > 0f)
            			{
                			vy *= jumpCutMultiplier;
                			rb.linearVelocity = new Vector2(rb.linearVelocity.x, vy);
            			}
        		}
        		else
        		{
            			// タイル基準：小ジャンの頂点(マス)を「なるべく固定」したい
            			// bigで踏切ったと仮定して、時刻tでの理論状態を数式で出す
            			float g = -Physics2D.gravity.y * rb.gravityScale; // 正の値
            			float v0 = bigJumpForce; // 踏切初速（大）

            			// いまの理論上の上向き速度（上昇中のみ補正）
            			float vyNow = v0 - g * t;
            			if (vyNow > 0f)
            			{
                			// いまの理論上の相対高さ
                			float yNow = v0 * t - 0.5f * g * t * t;

                			// 小ジャンの目標頂点（相対）
                			float Hsmall = smallJumpTiles * tileSizeY;

                			// 残り上がるべき高さ
                			float remain = Mathf.Max(0f, Hsmall - yNow);

                			// その remain を上がるために必要な速度
                			float vNeeded = Mathf.Sqrt(2f * g * remain);

                			// “小にする”なので、速度を上げない（下げるだけ）
                			float newVy = Mathf.Min(vyNow, vNeeded);

                			rb.linearVelocity = new Vector2(rb.linearVelocity.x, newVy);
            			}
            			else
            			{
                			// 既に下降に入ってる/頂点付近なら、何もしない（小にできないタイミング）
                			// ここは好みで newVy=0 に落とす等も可
            			}
        		}

        		canCutToSmall = false;
    		}
	}





        //ゴーストのロック解除
        if (isRewindLocked && Time.time >= rewindLockEndTime)
        {
            isRewindLocked = false;
        }

    }

    // ===== 状態処理 =====

    void UpdateGrounded(bool isGrounded)
    {
        // 地面から離れた
        if (!isGrounded)
        {
            state = PlayerState.Air;
            return;
        }

        // ジャンプ
        if (HasBufferedJump())
        {
            Jump();
            state = PlayerState.Air;
        }
    }

    void UpdateAir(bool isGrounded)
    {
        // コヨーテタイム中ジャンプ
        if (CanCoyoteJump())
        {
            Jump();
            state = PlayerState.Air;
            return;
        }

	// ★ 壁コヨーテ
    	bool wallCoyoteActive = (Time.time - lastWallContactTime) <= wallCoyoteTime;
    	bool canUseWall = (!isGrounded) && (isTouchingWall || wallCoyoteActive);
    	int usableWallDir = isTouchingWall ? wallDir : lastWallDir;

	// ★ 壁キック
        if (canUseWall && HasBufferedJump())
        {
		wallDir = usableWallDir;
                WallKick();
                return;
        }

        // 着地
        if (isGrounded)
        {
            state = PlayerState.Grounded;
        }
    }

    // ===== ジャンプ関連 =====

    void Jump()
    {
    	// ★大ジャンプ初速で即踏切
    	rb.linearVelocity = new Vector2(rb.linearVelocity.x, bigJumpForce);

    	jumpStartTime = Time.time;
    	canCutToSmall = true;   // ←新しいフラグ

    	// バッファ消費
    	lastJumpInputTime = -999f;
    	lastGroundedTime = -999f;
    }


    bool HasBufferedJump()
    {
        return Time.time - lastJumpInputTime <= jumpBufferTime;
    }

    bool CanCoyoteJump()
    {
        return HasBufferedJump()
            && Time.time - lastGroundedTime <= coyoteTime;
    }

    // ===== 接地判定（3本Ray） =====

    bool CheckGrounded()
    {
    if (col == null) col = GetComponent<BoxCollider2D>();

    // コライダーサイズに追従するパラメータ
    float w = col.bounds.size.x;
    float h = col.bounds.size.y;

    float skin = Mathf.Clamp(h * 0.05f, 0.005f, 0.03f);           // だいたい 0.01〜0.02に落ち着く
    float rayLength = Mathf.Clamp(h * 0.20f, 0.03f, 0.15f);       // 体が小さいと短くなる
    float inset = Mathf.Clamp(w * 0.25f, 0.01f, 0.08f);           // 端から少し内側

    float y = col.bounds.min.y + skin;

    Vector2 center = new Vector2(col.bounds.center.x, y);
    Vector2 left   = new Vector2(col.bounds.min.x + inset, y);
    Vector2 right  = new Vector2(col.bounds.max.x - inset, y);

    bool hitCenter = Physics2D.Raycast(center, Vector2.down, rayLength, groundLayer);
    bool hitLeft   = Physics2D.Raycast(left,   Vector2.down, rayLength, groundLayer);
    bool hitRight  = Physics2D.Raycast(right,  Vector2.down, rayLength, groundLayer);

    //Debug.DrawRay(center, Vector2.down * rayLength, Color.red);
    //Debug.DrawRay(left,   Vector2.down * rayLength, Color.red);
    //Debug.DrawRay(right,  Vector2.down * rayLength, Color.red);

    return hitCenter || hitLeft || hitRight;
    }



    // ===== 壁判定（3本Ray） =====
    bool CheckWall()
    {
    if (col == null) col = GetComponent<BoxCollider2D>();

    float w = col.bounds.size.x;
    float h = col.bounds.size.y;

    float skin = Mathf.Clamp(w * 0.05f, 0.005f, 0.03f);
    float dist = Mathf.Clamp(w * 0.25f, 0.03f, 0.15f); // 小さいと短くなる
    float y = col.bounds.center.y;

    Vector2 left  = new Vector2(col.bounds.min.x + skin, y);
    Vector2 right = new Vector2(col.bounds.max.x - skin, y);

    bool hitLeft = Physics2D.Raycast(left, Vector2.left, dist, wallLayer);
    bool hitRight = Physics2D.Raycast(right, Vector2.right, dist, wallLayer);

    //Debug.DrawRay(left,  Vector2.left  * dist, Color.blue);
    //Debug.DrawRay(right, Vector2.right * dist, Color.blue);

    if (hitLeft) wallDir = -1;
    else if (hitRight) wallDir = 1;
    else wallDir = 0;

    return hitLeft || hitRight;
    }


    void WallKick()
    {
	postWallKickTimer = postWallKickControlTime;
        Vector2 v = rb.linearVelocity;
        v.x = -wallDir * wallKickForceX;
        v.y = wallKickForceY;
        rb.linearVelocity = v;

        // ジャンプ入力消費（重要）
        lastJumpInputTime = -999f;
    }

    //初速計算関数
    void RecalcJumpFromTiles_GravityFixed()
    {
    	tileSizeY = Mathf.Max(0.0001f, tileSizeY);
    	bigJumpTiles = Mathf.Max(0.01f, bigJumpTiles);
    	smallJumpTiles = Mathf.Clamp(smallJumpTiles, 0.01f, bigJumpTiles);

    	float g = -Physics2D.gravity.y * rb.gravityScale; // 正の値

    	float Hbig = bigJumpTiles * tileSizeY;
    	float Hsmall = smallJumpTiles * tileSizeY;

    	bigJumpForce = Mathf.Sqrt(2f * g * Hbig);
    	smallJumpForce = Mathf.Sqrt(2f * g * Hsmall);

	// ===== デバッグログ =====
    	float bigApexTime = bigJumpForce / g;
	float smallApexTime = smallJumpForce / g;

	Debug.Log(
    	$"[Jump Debug] g={g:F3}  " +
    	$"Big v0={bigJumpForce:F3}, T={bigApexTime:F3}s  " +
    	$"Small v0={smallJumpForce:F3}, T={smallApexTime:F3}s"
	);

    }






    void RecordPosition()
    {
	//ロック中は履歴を記録しない
	if (isRewindLocked)
	{
		if (Time.time >= rewindLockEndTime)
		{
			isRewindLocked = false;
		}
		else
		{
			return;
		}
	}
	
	//現在位置を記録
	positionHistory.Enqueue(
		new PositionSnapshot(rb.position, Time.time)
	);

	//２秒以上古い履歴は捨てる
	while (positionHistory.Count > 0 &&
		Time.time - positionHistory.Peek().time > 2f)
	{
		positionHistory.Dequeue();
	}

	//今戻るならここというアンカーポイントを常に一意に更新
	if (positionHistory.Count > 0)
	{
		rewindAnchorPosition = positionHistory.Peek().position;
	}
    }


    public Vector2 GetRewindPosition()
    {
		return rewindAnchorPosition;
    }

    public void LockRewind(float lockDuration)
    {
        isRewindLocked = true;
        rewindLockEndTime = Time.time + lockDuration;
        positionHistory.Clear(); // 履歴は消す
    }

    public void SetControlLock(bool locked)
    {
        isControlLocked = locked;

        if (locked)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }


	// --- EkkoUltimate 用API：いま地面にいる？ ---
	public bool IsGroundedNow()
	{
    	// Raycastしない。Updateで更新された状態を見るだけ
    	return isGrounded;
	}

	// --- EkkoUltimate 用API：いま壁キック可能？（壁接触 or 壁コヨーテ） ---
	public bool IsWallKickWindowNow()
	{
    	// いま接触している or 壁コヨーテ中
    	bool wallCoyote = (Time.time - lastWallContactTime) <= wallCoyoteTime;
    	return isTouchingWall || wallCoyote;
	}

	// --- EkkoUltimate 用：ジャンプ初速を渡す ---
	public float GetSmallJumpV0() => smallJumpForce;
	public float GetBigJumpV0()   => bigJumpForce;

	// --- 壁キックの向き ---
	public float GetWallKickVY()  => wallKickForceY;
	public float GetWallKickVX() => wallKickForceX;
	public int GetWallDirNow() => isTouchingWall ? wallDir : lastWallDir; // -1 or +1






    //外部速度注入API
    public void InjectVelocity(Vector2 velocity)
    {
        externalVelocity = velocity;
        hasExternalVelocity = true;
    }

    //トゲやゴールに当たった時の処理
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Hazard"))
        {
	    if (IsInvincible) return;   // ★追加：無敵中はHazard無視
            Respawn();
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Goal"))
        {
            Debug.Log("GOAL!");
            // ここにクリア処理
        }
	else if (other.gameObject.layer == LayerMask.NameToLayer("Checkpoint"))
	{
		spawnPosition = other.transform.position; // 旗の位置にする
		//Debug.Log($"[Checkpoint] updated -> {spawnPosition}");
	}
    }

	public bool IsInvincible { get; private set; }

	public void SetInvincible(bool on)
	{
    		IsInvincible = on;
	}


       void Respawn()
	{
    	rb.linearVelocity = Vector2.zero;
    	rb.angularVelocity = 0f;

    	rb.position = spawnPosition; 
	// 死んだ位置に戻れないように履歴を初期化
        positionHistory.Clear();
        rewindAnchorPosition = spawnPosition;
	RespawnEvents.RaiseRespawned();

    	// rb.MovePosition(spawnPosition); // 必要ならこっちに変更

    	state = PlayerState.Air; // ここはあなたの設計でOK
	}





}
