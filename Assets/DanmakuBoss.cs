using UnityEngine;

public class DanmakuBoss : MonoBehaviour
{
    [SerializeField] private DanmakuEmitter emitter;
    [SerializeField] private Transform player;
    [SerializeField] private DanmakuPhase[] phases;
    [SerializeField] private bool loop = true;
    [SerializeField] private BulletPool bulletPool;


    private int _idx = -1;
    private float _phaseTime;

    private void Start()
    {
        NextPhase();
    }

    private void Update()
	{
    	if (phases == null || phases.Length == 0 || emitter == null) return;

    	// ★まだフェーズ未開始なら最初へ
    	if (_idx < 0) NextPhase();

    	var phase = phases[_idx];
    	phase.Tick(emitter, player, Time.deltaTime, ref _phaseTime);

    	if (phase.duration > 0f && _phaseTime >= phase.duration)
        	NextPhase();
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

    	// 自分を停止（StartTriggerで再起動させる）
    	enabled = false;
	}


}
