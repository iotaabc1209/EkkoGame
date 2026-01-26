using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float lifetime = 8f;

    private Rigidbody2D _rb;
    private float _t;
    private System.Action<Bullet> _release;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        // 弾はKinematicでもOKだが、速度で動かすならDynamic/ Kinematicどちらでも可
        // ここではDynamic想定。重力0推奨。
    }

    public void Init(Vector2 velocity, System.Action<Bullet> release)
    {
        _release = release;
        _t = 0f;
        gameObject.SetActive(true);
        _rb.linearVelocity = velocity;
    }

    private void Update()
    {
        _t += Time.deltaTime;
        if (_t >= lifetime)
            Despawn();
    }

    public void Despawn()
    {
        _rb.linearVelocity = Vector2.zero;
        gameObject.SetActive(false);
        _release?.Invoke(this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // プレイヤー側でHazard判定する運用なら、弾はHazardレイヤーにしてここは空でOK。
        // もし弾が何かに当たったら消したいなら以下を使う：
        // if (other.CompareTag("Wall")) Despawn();
    }
}
