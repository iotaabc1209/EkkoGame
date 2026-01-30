using UnityEngine;

public class DanmakuBoss : MonoBehaviour
{
    [SerializeField] private DanmakuEmitter emitter;
    [SerializeField] private Transform player;
    [SerializeField] private DanmakuPhase[] phases;
    [SerializeField] private bool loop = true;
    [SerializeField] private BulletPool bulletPool;
    [SerializeField] private float phaseInterval = 0.5f; // ★追加：フェーズ間の休み
    private bool _waiting;  // ★追加
    private float _wait;    // ★追加



    private int _idx = -1;
    private float _phaseTime;

    private void Start()
    {
        NextPhase();
    }

    private void Update()
    {
    if (phases == null || phases.Length == 0 || emitter == null) return;

    // ★フェーズ間の待機（この間はTickしない＝撃たない）
    if (_waiting)
    {
        _wait += Time.deltaTime;
        if (_wait >= phaseInterval)
        {
            _waiting = false;
            _wait = 0f;
            NextPhase(); // 次フェーズ開始
        }
        return;
    }

    // ★まだフェーズ未開始なら最初へ
    if (_idx < 0) NextPhase();

    var phase = phases[_idx];
    phase.Tick(emitter, player, Time.deltaTime, ref _phaseTime);

    if (phase.duration > 0f && _phaseTime >= phase.duration)
    {
        // ここで即NextPhaseせず、待機に入る
        _waiting = true;
        _wait = 0f;
    }
}


    private void NextPhase()
    {
        if (phases == null || phases.Length == 0) return;

        if (_idx >= 0) phases[_idx].OnExit(emitter);

        _idx++;
        if (_idx >= phases.Length)
        {
            if (!loop) { enabled = false; return; }
            _idx = 0;
        }

        _phaseTime = 0f;
	_waiting = false; // ★追加：念のため
	_wait = 0f;        // ★追加：念のため
	phases[_idx].OnEnter(emitter);

        phases[_idx].OnEnter(emitter);
    }

    private void OnEnable()
	{
    		RespawnEvents.Respawned += OnRespawned;
	}

    private void OnDisable()
	{
    		RespawnEvents.Respawned -= OnRespawned;
	}

    private void OnRespawned()
	{
    	// 弾を全消し
    	if (bulletPool != null)
        	bulletPool.DespawnAll();

    	// フェーズ時間をリセット
    	_phaseTime = 0f;
    	_idx = -1;
	_waiting = false; // ★追加
	_wait = 0f;       // ★追加


    	// 自分を停止（StartTriggerで再起動させる）
    	enabled = false;
	}


}
