using UnityEngine;

[CreateAssetMenu(menuName = "Danmaku/Phase/Spiral")]
public class Phase_Spiral : DanmakuPhase
{
    public float speed = 4f;
    public float fireRate = 18f;     // 秒あたり発射数
    public float turnSpeed = 180f;   // 角速度(度/秒)
    public float spread = 12f;       // 2本撃ちする場合の開き
    public bool doubleShot = true;

    private float _angle;
    private float _acc;

    public override void OnEnter(DanmakuEmitter emitter)
    {
        _angle = 0f;
        _acc = 0f;
    }

    public override void Tick(DanmakuEmitter emitter, Transform player, float dt, ref float phaseTime)
    {
        phaseTime += dt;

        _angle += turnSpeed * dt;
        _acc += fireRate * dt;

        while (_acc >= 1f)
        {
            _acc -= 1f;

            emitter.Shoot(emitter.MuzzlePos, speed, _angle);

            if (doubleShot)
            {
                emitter.Shoot(emitter.MuzzlePos, speed, _angle + spread);
            }
        }
    }
}
