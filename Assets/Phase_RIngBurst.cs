using UnityEngine;

[CreateAssetMenu(menuName = "Danmaku/Phase/Ring Burst")]
public class Phase_RingBurst : DanmakuPhase
{
    public int bullets = 18;
    public float speed = 4f;
    public float interval = 2f;

    [Header("Angle")]
    public float baseStartAngle = 340f;     // ★Inspector用（固定）
    public float angleStepPerBurst = 6f;  // ★1回撃つごとに回す量（0で固定）

    private float _cool;
    private float _angle; // ★実行中の角度

    public override void OnEnter(DanmakuEmitter emitter)
    {
        _cool = 0f;
        _angle = baseStartAngle; // ★フェーズ開始時に必ず初期化
    }

    public override void Tick(DanmakuEmitter emitter, Transform player, float dt, ref float phaseTime)
    {
        phaseTime += dt;
        _cool -= dt;
        if (_cool > 0f) return;
        _cool = interval;

        float step = 360f / Mathf.Max(1, bullets);
        for (int i = 0; i < bullets; i++)
        {
            float a = _angle + step * i;
            emitter.Shoot(emitter.MuzzlePos, speed, a);
        }

        _angle += angleStepPerBurst;
    }
}
