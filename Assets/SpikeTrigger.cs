using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SpikeTrigger : MonoBehaviour
{
    [Header("Who to notify")]
    [SerializeField] private SpikeShooter shooter;

    [Header("Detect")]
    [SerializeField] private string playerTag = "Player";

    [Header("Trigger Area")]
    [SerializeField] private Vector2 areaSize = new Vector2(1f, 1f);
    [SerializeField] private Vector2 areaOffset = Vector2.zero;

    [Header("Fire Control")]
    [SerializeField] private bool oneShot = true;

    private BoxCollider2D _col;
    private bool _hasTriggered;

    private void Awake()
    {
        _col = GetComponent<BoxCollider2D>();
        ApplyArea();
    }

    // ★方式2：リスポーン通知を購読（自分で復活）
    private void OnEnable()
    {
        RespawnEvents.Respawned += ResetState;
    }

    private void OnDisable()
    {
        RespawnEvents.Respawned -= ResetState;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_col == null) _col = GetComponent<BoxCollider2D>();
        ApplyArea();
    }
#endif

    private void ApplyArea()
    {
        if (_col == null) return;
        _col.isTrigger = true;
        _col.size = new Vector2(Mathf.Max(0.01f, areaSize.x), Mathf.Max(0.01f, areaSize.y));
        _col.offset = areaOffset;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (oneShot && _hasTriggered) return;
        _hasTriggered = true;

        if (shooter == null)
        {
            Debug.LogError("[SpikeTrigger] Shooter is NULL. Assign SpikeShooter on Trigger inspector.", this);
            return;
        }

        shooter.Fire();

        if (oneShot) _col.enabled = false;
    }

    // ★追加：リスポーン時に判定を復活させる
    public void ResetState()
    {
        _hasTriggered = false;

        if (_col == null) _col = GetComponent<BoxCollider2D>();
        if (_col != null) _col.enabled = true;

        ApplyArea(); // サイズ/オフセットも念のため反映
    }
}
