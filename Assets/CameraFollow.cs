using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("追従対象")]
    public Transform target;   // Player を入れる

    [Header("追従設定")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    void LateUpdate()
    {
        if (target == null) return;

        // X だけ追従、Y は固定
        Vector3 newPos = transform.position;
        newPos.x = target.position.x + offset.x;
        newPos.y = offset.y;
        newPos.z = offset.z;

        transform.position = newPos;
    }
}
