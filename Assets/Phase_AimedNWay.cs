using UnityEngine;

[CreateAssetMenu(menuName = "Danmaku/Phase/Aimed N-Way")]
public class Phase_AimedNWay : DanmakuPhase
{
    public int ways = 5;
    public float speed = 4f;
    public float interval = 2f;
    public float totalSpread = 40f;  // 全体の開き(度)
    public float aimOffset = 4f;     // 狙いを少し外す(度) 例: 6で優しくなる

    private float _cool;

    public override void OnEnter(DanmakuEmitter emitter) => _cool = 0f;

    public override void Tick(DanmakuEmitter emitter, Transform player, float dt, ref float phaseTime)
    {
        phaseTime += dt;
        _cool -= dt;
        if (_cool > 0f) return;
        _cool = interval;

        if (player == null) return;

        float baseAngle = DanmakuEmitter.AngleTo(emitter.MuzzlePos, player.position) + aimOffset;

        if (ways <= 1)
        {
            emitter.Shoot(emitter.MuzzlePos, speed, baseAngle);
            return;
        }

        float step = totalSpread / (ways - 1);
        float start = baseAngle - totalSpread * 0.5f;
        for (int i = 0; i < ways; i++)
            emitter.Shoot(emitter.MuzzlePos, speed, start + step * i);
    }
}
