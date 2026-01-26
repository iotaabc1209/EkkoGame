using UnityEngine;

public class AfterImageSpawner : MonoBehaviour
{
    public GameObject afterImagePrefab;
    public float spawnInterval = 0.05f;

    float timer;
    SpriteRenderer playerSR;

    void Start()
    {
        playerSR = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnAfterImage();
            timer = 0f;
        }
    }

    void SpawnAfterImage()
    {
        GameObject img = Instantiate(afterImagePrefab, transform.position, Quaternion.identity);

        SpriteRenderer imgSR = img.GetComponent<SpriteRenderer>();
        imgSR.sprite = playerSR.sprite;
        imgSR.flipX = playerSR.flipX;
    }
}
