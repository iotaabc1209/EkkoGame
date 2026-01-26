using UnityEngine;

public abstract class DanmakuPhase : ScriptableObject
{
    [Tooltip("このフェーズの持続時間(秒)。0以下なら無限。")]
    public float duration = 6f;

    public virtual void OnEnter(DanmakuEmitter emitter) { }
    public abstract void Tick(DanmakuEmitter emitter, Transform player, float dt, ref float phaseTime);
    public virtual void OnExit(DanmakuEmitter emitter) { }
}
