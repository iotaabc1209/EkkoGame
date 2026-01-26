using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossStartTrigger : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private DanmakuBoss boss;

    [Header("Detect")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool oneShot = true;

    private bool _started;
    private Collider2D _col;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;

        if (boss != null)
            boss.enabled = false; // 開始前は停止
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
        // リスポーンしたら「再スタート可能」に戻す
        _started = false;

        if (_col != null)
            _col.enabled = true;

        // ボスは止まっている前提に合わせる
        if (boss != null)
            boss.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (oneShot && _started) return;
        if (!other.CompareTag(playerTag)) return;

        _started = true;

        if (boss != null)
            boss.enabled = true;

        // 起動したら親から外したいならここ（任意）
        transform.SetParent(null, true);

        if (oneShot && _col != null)
            _col.enabled = false;
    }
}
