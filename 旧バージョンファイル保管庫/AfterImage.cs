using UnityEngine;

public class AfterImage : MonoBehaviour
{
    SpriteRenderer sr;

    public float lifeTime = 0.3f;
    public float fadeSpeed = 5f;

    Color color;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        color = sr.color;
    }

    void Update()
    {
        color.a -= fadeSpeed * Time.deltaTime;
        sr.color = color;

        if (color.a <= 0)
        {
            Destroy(gameObject);
        }
    }
}
