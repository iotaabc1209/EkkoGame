using UnityEngine;

public class DanmakuEmitter : MonoBehaviour
{
    [SerializeField] private BulletPool pool;
    [SerializeField] private Transform muzzle; // 口(発射位置)。無ければ自分の位置

    public Vector3 MuzzlePos => muzzle != null ? muzzle.position : transform.position;

    public void Shoot(Vector3 pos, float speed, float angleDeg)
    {
        var dir = DirFromAngle(angleDeg);
        pool.Spawn(pos, dir * speed);
    }

    public void Shoot(Vector3 pos, Vector2 velocity)
    {
        pool.Spawn(pos, velocity);
    }

    public static Vector2 DirFromAngle(float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    public static float AngleTo(Vector2 from, Vector2 to)
    {
        Vector2 d = (to - from).normalized;
        return Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
    }
}
