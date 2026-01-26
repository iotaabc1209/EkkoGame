using System.Collections;
using UnityEngine;

public class SpikeShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform spike;          // 子のSpike
    [SerializeField] private Transform startPoint;     // 引っ込み位置(任意)

    [Header("Shoot Settings")]
    [SerializeField] private Vector2 shootDirection = Vector2.right; // 射出方向
    [SerializeField] private float speed = 12f;        // 速度(Units/sec)
    [SerializeField] private float shootDistance = 1.5f; // どこまで伸びるか
    [SerializeField] private float delay = 0f;         // 射出までの遅延
    [SerializeField] private float stayTime = 0.1f;    // 伸び切って止まる時間
    [SerializeField] private float retractSpeed = 18f; // 戻り速度

    [Header("Fire Control")]
    [SerializeField] private bool oneShot = true;      // 1回だけ発射するか

    private bool _fired;
    private Vector3 _startPos;
    private Coroutine _routine;

    private void Reset()
    {
        // エディタで追加した瞬間に参照を自動で拾いやすくする
        if (spike == null)
        {
            var t = transform.Find("Spike");
            if (t != null) spike = t;
        }
        if (startPoint == null)
        {
            var t = transform.Find("StartPoint");
            if (t != null) startPoint = t;
        }
    }

    private void Awake()
    {
        if (spike == null)
        {
            Debug.LogError("[SpikeShooter] Spike reference is missing.", this);
            enabled = false;
            return;
        }

        _startPos = (startPoint != null) ? startPoint.position : spike.position;
        spike.position = _startPos;
    }

    // ★方式2：リスポーン通知を購読
    private void OnEnable()
    {
        RespawnEvents.Respawned += ResetState;
    }

    private void OnDisable()
    {
        RespawnEvents.Respawned -= ResetState;
    }

    public void Fire()
    {
        if (oneShot && _fired) return;
        _fired = true;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(FireRoutine());
    }

    private IEnumerator FireRoutine()
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        Vector2 dir = shootDirection.sqrMagnitude > 0.0001f
            ? shootDirection.normalized
            : Vector2.right;

        Vector3 target = _startPos + (Vector3)(dir * shootDistance);

        // 伸びる
        while ((spike.position - target).sqrMagnitude > 0.0001f)
        {
            spike.position = Vector3.MoveTowards(spike.position, target, speed * Time.deltaTime);
            yield return null;
        }

        // 伸び切りで少し止める（アイワナっぽさ）
        if (stayTime > 0f) yield return new WaitForSeconds(stayTime);

        // 戻る
        while ((spike.position - _startPos).sqrMagnitude > 0.0001f)
        {
            spike.position = Vector3.MoveTowards(spike.position, _startPos, retractSpeed * Time.deltaTime);
            yield return null;
        }

        _routine = null;
    }

    // ★追加：リスポーン用リセット（位置・フラグ・発射中止）
    public void ResetState()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        _fired = false;

        if (spike != null)
        {
            spike.position = _startPos;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 方向と射出距離をScene上で見えるようにする
        Vector3 origin = (startPoint != null)
            ? startPoint.position
            : (spike != null ? spike.position : transform.position);

        Vector2 dir = shootDirection.sqrMagnitude > 0.0001f ? shootDirection.normalized : Vector2.right;
        Vector3 end = origin + (Vector3)(dir * shootDistance);

        Gizmos.DrawLine(origin, end);
        Gizmos.DrawWireSphere(end, 0.05f);
    }
#endif
}
